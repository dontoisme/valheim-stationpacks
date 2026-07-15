using System.Linq;
using HarmonyLib;
using StationPacks.Config;
using StationPacks.Core;
using UnityEngine;

namespace StationPacks.Patches
{
    /// <summary>
    /// Makes the back mesh follow the torso.
    ///
    /// By default the game attaches an armor item under the body model's root, so a static mesh
    /// parented there stays rigid relative to the whole character - it floats in place when the
    /// character bends (sitting, sleeping). Reparenting our container onto the actual spine bone lets
    /// it lean and twist with the upper body like a real pack.
    ///
    /// We hook VisEquipment.AttachArmor, the shared method that instantiates the attach_skin visual for
    /// EVERY slot (shoulder, utility, chest, ...). That way the bind fires whether a pack is worn on the
    /// utility slot (the default) or the shoulder slot, without caring which. It scans for our own
    /// PackBackTag, so equips of ordinary armor are a cheap no-op.
    /// </summary>
    [HarmonyPatch(typeof(VisEquipment), "AttachArmor")]
    internal static class BackBonePatch
    {
        [HarmonyPostfix]
        private static void Bind(VisEquipment __instance)
        {
            if (!SPConfig.ShowBackMesh.Value) return;

            var bodyModel = __instance.m_bodyModel;
            if (bodyModel == null) return;

            var tags = __instance.GetComponentsInChildren<PackBackTag>(true);
            if (tags.Length == 0) return;

            var spine = FindSpineBone(bodyModel);
            if (spine == null) return;

            foreach (var tag in tags)
            {
                if (tag.Bound) continue;

                // Remember the attach_skin we're leaving, so the tag can self-destruct when the game
                // destroys it on unequip (otherwise the mesh orphans onto the skeleton).
                tag.Source = tag.transform.parent;

                // Reparent onto the spine bone and set the pack's placement directly in bone-local
                // space. worldPositionStays:false so we control the transform outright - the tag's
                // target values (and therefore the tune command and the baked defaults) are all
                // expressed relative to Spine2.
                tag.transform.SetParent(spine, worldPositionStays: false);
                tag.transform.localScale = Vector3.one * tag.TargetScale;
                tag.transform.localPosition = tag.TargetOffset;
                tag.transform.localRotation = Quaternion.Euler(tag.TargetEuler);
                tag.Bound = true;
            }
        }

        /// <summary>
        /// Picks the upper-spine bone from the body model's skeleton. Bone names live in the character
        /// armature (asset data), not the assembly, so we discover rather than hardcode: among bones
        /// whose name mentions the spine/chest, take the deepest (highest-numbered) one, which is the
        /// upper back. Falls back to the root bone.
        /// </summary>
        private static Transform FindSpineBone(SkinnedMeshRenderer bodyModel)
        {
            var bones = bodyModel.bones;
            if (bones == null || bones.Length == 0) return bodyModel.rootBone;

            Transform best = null;
            foreach (var b in bones)
            {
                if (b == null) continue;
                var n = b.name.ToLowerInvariant();
                if (n.Contains("spine") || n.Contains("chest") || n.Contains("back"))
                {
                    // Prefer later/deeper bones (Spine2 over Spine, i.e. the upper torso).
                    if (best == null || CompareDepth(b, best) > 0) best = b;
                }
            }

            if (best != null && !_loggedBone)
            {
                _loggedBone = true;
                Plugin.Log.LogInfo($"Back mesh bound to spine bone '{best.name}'.");
            }
            return best ?? bodyModel.rootBone;
        }

        private static bool _loggedBone;

        /// <summary>Rough "deeper in the hierarchy / higher trailing number" comparison.</summary>
        private static int CompareDepth(Transform a, Transform b)
        {
            int na = TrailingNumber(a.name), nb = TrailingNumber(b.name);
            if (na != nb) return na - nb;
            return Depth(a) - Depth(b);
        }

        private static int TrailingNumber(string name)
        {
            int i = name.Length;
            while (i > 0 && char.IsDigit(name[i - 1])) i--;
            return i < name.Length && int.TryParse(name.Substring(i), out var v) ? v : 0;
        }

        private static int Depth(Transform t)
        {
            int d = 0;
            while (t.parent != null) { d++; t = t.parent; }
            return d;
        }
    }
}
