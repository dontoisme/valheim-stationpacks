using System.Linq;
using Jotunn.Managers;
using StationPacks.Config;
using UnityEngine;

namespace StationPacks.Core
{
    /// <summary>
    /// Gives a pack a distinct look on the player's back by mounting a shrunk copy of the station it
    /// emulates - the Workbench Pack literally wears a mini workbench.
    ///
    /// How the shoulder slot renders (verified from VisEquipment.AttachArmor in assembly_valheim):
    /// the game walks the item prefab's children for names starting with "attach_", and for the
    /// "attach_skin" child it instantiates that node onto the body and copies the player's bones onto
    /// every SkinnedMeshRenderer inside it. Crucially, that bone-copy loop only touches
    /// SkinnedMeshRenderers - a plain MeshRenderer child rides along untouched. So we clone a cape
    /// (which brings a working attach_skin), then parent a static donor mesh inside it. No Unity Editor,
    /// no AssetBundle, no rigging, no shader work: the donor mesh keeps its vanilla material.
    ///
    /// This is purely cosmetic and additive. If anything here fails, the pack falls back to looking
    /// like the plain cloned cape, which already works.
    /// </summary>
    public static class PackVisual
    {
        private const string BackNode = "SP_BackMesh";

        public static void Apply(GameObject packPrefab, PackDefinition def)
        {
            if (!SPConfig.ShowBackMesh.Value) return;

            var attachSkin = FindAttachSkin(packPrefab);
            if (attachSkin == null)
            {
                Plugin.Log.LogWarning($"{def.DisplayName}: no attach_skin node on the cloned cape; " +
                                      "back mesh skipped (pack still works, just looks like the cape).");
                return;
            }

            var donor = PrefabManager.Instance.GetPrefab(def.EffectiveBackMeshDonor);
            if (donor == null)
            {
                Plugin.Log.LogWarning($"{def.DisplayName}: donor prefab '{def.EffectiveBackMeshDonor}' " +
                                      "not found; back mesh skipped.");
                return;
            }

            var picks = CollectPicks(donor, def);
            if (picks.Count == 0)
            {
                Plugin.Log.LogWarning($"{def.DisplayName}: donor '{def.EffectiveBackMeshDonor}' has no " +
                                      "mesh to borrow; back mesh skipped.");
                return;
            }

            // Container so scale/offset apply to the whole borrowed mesh as one unit. It starts under
            // attach_skin (so AttachArmor instantiates it onto the character), but its final placement
            // is set by BackBonePatch once it's reparented onto the Spine2 bone - which is why the
            // pack's transform is carried on the tag, in bone-local space, rather than applied here.
            var container = new GameObject(BackNode);
            var tag = container.AddComponent<PackBackTag>();
            tag.TargetScale = def.BackScale;
            tag.TargetOffset = def.BackOffset;
            tag.TargetEuler = def.BackEuler;
            container.transform.SetParent(attachSkin.transform, false);

            var donorRoot = donor.transform;
            foreach (var p in picks)
            {
                var piece = new GameObject(p.src.gameObject.name);
                piece.transform.SetParent(container.transform, false);
                // Preserve each sub-mesh's placement relative to the donor root, so a multi-part
                // station (bench + legs + vise) keeps its shape.
                piece.transform.localPosition = donorRoot.InverseTransformPoint(p.src.position);
                piece.transform.localRotation = Quaternion.Inverse(donorRoot.rotation) * p.src.rotation;
                piece.transform.localScale = p.src.lossyScale;

                piece.AddComponent<MeshFilter>().sharedMesh = p.mesh;   // skinned meshes render fine as static
                var dst = piece.AddComponent<MeshRenderer>();

                // Clone the materials. Sharing them would be fine here (we don't mutate), but cloning
                // is cheap insurance against a future tint accidentally recoloring every station.
                dst.sharedMaterials = p.mats.Select(m => m != null ? new Material(m) : null).ToArray();
                dst.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            int copied = picks.Count;

            if (SPConfig.HideCape.Value)
            {
                // A cape draping behind a backpack reads as clutter. Hide the cape's own mesh so only
                // the pack shows. We disable rather than destroy, in case a future toggle wants it back.
                foreach (var smr in attachSkin.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    smr.enabled = false;
            }

            Plugin.Log.LogInfo($"{def.DisplayName}: mounted {copied} mesh part(s) from " +
                               $"'{def.EffectiveBackMeshDonor}'.");
        }

        /// <summary>A borrowable mesh: the mesh, its materials, and the transform it sits at on the donor.</summary>
        private struct Pick
        {
            public Mesh mesh;
            public Material[] mats;
            public Transform src;
        }

        /// <summary>
        /// Gathers the meshes to mount, in order of preference. Some stations render through a plain
        /// MeshRenderer (workbench, forge), some through a SkinnedMeshRenderer (artisan table), and some
        /// - like the black forge - have their renderers DISABLED at prefab-registration time (their
        /// LODs aren't awake yet), so a strict "active only" pass finds nothing. So we try progressively
        /// looser passes and take the first that yields anything:
        ///   1. active renderers          (clean, the usual case)
        ///   2. any renderers, incl. disabled/inactive
        /// Both passes still drop destruction fragments and low-detail LODs, and honor the pack filter.
        /// </summary>
        private static System.Collections.Generic.List<Pick> CollectPicks(GameObject donor, PackDefinition def)
        {
            var strict = Renderers(donor, def, requireActive: true);
            return strict.Count > 0 ? strict : Renderers(donor, def, requireActive: false);
        }

        private static System.Collections.Generic.List<Pick> Renderers(GameObject donor, PackDefinition def, bool requireActive)
        {
            var lodDrops = LodDrops(donor);
            var picks = new System.Collections.Generic.List<Pick>();

            // Strict pass walks only the ACTIVE hierarchy (includeInactive:false). That is what keeps the
            // workbench's inactive "WorkbenchDestruction" subtree out - and it's what the un-spawned-
            // prefab's activeInHierarchy could NOT do reliably. The loose pass includes inactive for
            // stations whose renderers are simply disabled at load (black forge).
            foreach (var r in donor.GetComponentsInChildren<Renderer>(includeInactive: !requireActive))
            {
                if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
                if (requireActive && !r.enabled) continue;
                if (lodDrops.Contains(r)) continue;
                if (IsDestruction(r.transform, donor.transform)) continue;
                if (!KeepPart(r.gameObject.name, def)) continue;

                Mesh mesh = r is SkinnedMeshRenderer smr
                    ? smr.sharedMesh
                    : r.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh == null) continue;

                picks.Add(new Pick { mesh = mesh, mats = r.sharedMaterials, src = r.transform });
            }
            return picks;
        }

        /// <summary>
        /// True if this renderer is part of a station's shatter/debris model - checked up the whole
        /// parent chain, because the fragment GameObjects are often named plainly (e.g. "Plane.013")
        /// while only their parent carries "Destruction".
        /// </summary>
        private static bool IsDestruction(Transform t, Transform stopAt)
        {
            for (var cur = t; cur != null && cur != stopAt.parent; cur = cur.parent)
                if (cur.name.ToLowerInvariant().Contains("destruction")) return true;
            return false;
        }

        private static System.Collections.Generic.HashSet<Renderer> LodDrops(GameObject donor)
        {
            var drops = new System.Collections.Generic.HashSet<Renderer>();
            foreach (var lg in donor.GetComponentsInChildren<LODGroup>(true))
            {
                var lods = lg.GetLODs();
                for (int i = 1; i < lods.Length; i++)
                    foreach (var r in lods[i].renderers)
                        if (r != null) drops.Add(r);
            }
            return drops;
        }

        /// <summary>Applies a pack's include/exclude part filters (case-insensitive substring match).</summary>
        private static bool KeepPart(string partName, PackDefinition def)
        {
            if (def == null) return true;   // raw prefab query (the 'meshes' command) has no pack filter
            var name = partName.ToLowerInvariant();
            if (def.BackExcludeParts != null &&
                def.BackExcludeParts.Any(x => name.Contains(x.ToLowerInvariant())))
                return false;
            if (def.BackIncludeParts != null && def.BackIncludeParts.Length > 0)
                return def.BackIncludeParts.Any(x => name.Contains(x.ToLowerInvariant()));
            return true;
        }

        /// <summary>
        /// Finds the "attach_skin" child. AttachArmor matches children whose name starts with
        /// "attach_" and whose suffix equals "skin", so we match the same way rather than assuming an
        /// exact string (the name can carry a suffix on some prefabs).
        /// </summary>
        private static Transform FindAttachSkin(GameObject prefab)
        {
            foreach (Transform child in prefab.transform)
            {
                var name = child.gameObject.name;
                if (name.StartsWith("attach_") && name.Substring("attach_".Length).StartsWith("skin"))
                    return child;
            }
            return null;
        }
    }
}
