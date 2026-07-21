using Jotunn.Managers;
using StationPacks.Config;
using UnityEngine;

namespace StationPacks.Core
{
    /// <summary>
    /// Builds a pack's inventory icon by rendering the actual station model, so it reads as part of the
    /// game rather than an off-theme placeholder. Jotunn's <see cref="RenderManager"/> frames a GameObject
    /// with a dedicated camera/light and snapshots it to a sprite; we hand it the same donor station mesh
    /// <see cref="PackVisual"/> mounts on the player's back, shot from the vanilla isometric angle.
    ///
    /// Like the back mesh, this is additive and defensive: any failure returns null and
    /// <see cref="PackRegistry"/> falls back to the shipped PNG (and then to the cloned cape's icon), so a
    /// pack is never left with no icon.
    /// </summary>
    public static class StationIconRenderer
    {
        /// <summary>
        /// Renders <paramref name="def"/>'s station model to an inventory sprite, or returns null if the
        /// donor prefab is missing, has no mesh, or the render came back empty. Must run on the main thread
        /// after vanilla prefabs are available (RenderManager needs a live render loop) - which is exactly
        /// when PackRegistry.Register runs.
        /// </summary>
        public static Sprite Render(PackDefinition def)
        {
            var donor = PrefabManager.Instance.GetPrefab(def.EffectiveBackMeshDonor);
            if (donor == null)
            {
                Plugin.Log.LogWarning($"{def.DisplayName}: icon donor '{def.EffectiveBackMeshDonor}' " +
                                      "not found; using the placeholder PNG.");
                return null;
            }

            // A parentless, inactive root keeps the temp mesh out of the scene and off the character while
            // RenderManager does its thing; it renders the object regardless of active state, then we drop it.
            var root = new GameObject("SP_IconRender_" + def.Tag);
            root.SetActive(false);
            try
            {
                int copied = PackVisual.PopulateMeshPieces(root, donor, def);
                if (copied == 0)
                {
                    Plugin.Log.LogWarning($"{def.DisplayName}: icon donor '{def.EffectiveBackMeshDonor}' " +
                                          "had no mesh to render; using the placeholder PNG.");
                    return null;
                }

                var request = new RenderManager.RenderRequest(root)
                {
                    // IsometricRotation is the vanilla-icon angle; the per-pack euler nudges stations that
                    // read better from a slightly different turn (task sp-iz1.1).
                    Rotation = RenderManager.IsometricRotation * Quaternion.Euler(def.IconRotationEuler),
                    Width = def.IconRenderSize,
                    Height = def.IconRenderSize,
                    FieldOfView = def.IconFieldOfView,
                    DistanceMultiplier = def.IconDistanceMultiplier,
                    // No cache: the temp root is unique per registration, and we want the tuning knobs to
                    // take effect on every rebuild rather than serving a stale render.
                    UseCache = false,
                };

                var sprite = RenderManager.Instance.Render(request);
                if (sprite == null)
                {
                    Plugin.Log.LogWarning($"{def.DisplayName}: RenderManager returned no sprite; " +
                                          "using the placeholder PNG.");
                    return null;
                }

                Plugin.Log.LogInfo($"{def.DisplayName}: rendered icon from '{def.EffectiveBackMeshDonor}' " +
                                   $"({copied} mesh part(s)).");
                return sprite;
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"{def.DisplayName}: icon render threw {e.GetType().Name}: {e.Message}; " +
                                      "using the placeholder PNG.");
                return null;
            }
            finally
            {
                Object.Destroy(root);
            }
        }
    }
}
