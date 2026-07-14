using HarmonyLib;
using StationPacks.Config;
using StationPacks.Core;
using UnityEngine;

namespace StationPacks.Patches
{
    /// <summary>
    /// The entire feature.
    ///
    /// An IL call-graph scan of assembly_valheim found exactly three callers of the static
    /// CraftingStation.HaveBuildStationInRange(string, Vector3):
    ///
    ///   Player.HaveRequirements(Piece, RequirementMode)  -> build placement
    ///   Player.CheckCanRemovePiece(Piece)                -> deconstruct AND hammer-repair
    ///   Hud.SetupPieceInfo(Piece)                        -> the build menu's requirement icon
    ///
    /// Those are precisely the three gates we want to open and nothing else. In particular the
    /// crafting/upgrade GUI reads Player.m_currentStation, which is set only by
    /// CraftingStation.Interact - we never touch it, so portable item-crafting is excluded by
    /// construction rather than by discipline.
    /// </summary>
    [HarmonyPatch]
    internal static class CraftingStationPatches
    {
        /// <summary>
        /// The static takes no Player argument, so other mods (PlanBuild, InfinityHammer, base
        /// scanners) can legitimately call it for arbitrary world points. Without a distance guard
        /// they would silently get a free station anywhere on the map.
        /// </summary>
        private const float MaxGrantDistance = 64f;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.HaveBuildStationInRange))]
        private static void GrantFromPack(string name, Vector3 point, ref CraftingStation __result)
        {
            // A real station already covers this. Never override it - that also keeps us compatible
            // with mods (Valheim+, InfinityHammer) that may have returned one themselves.
            if (__result != null) return;

            var player = Player.m_localPlayer;
            if (player == null) return;
            if (ZNet.instance != null && ZNet.instance.IsDedicated()) return;

            if (Vector3.Distance(point, player.transform.position) > MaxGrantDistance) return;

            var pack = EquippedPackResolver.TryGetPack(player, name);
            if (pack == null) return;

            var phantom = PhantomStationRegistry.Get(name);
            if (phantom == null) return;

            // Transforms work on inactive GameObjects, and every downstream distance check measures
            // against the station's position - so putting it exactly on the query point makes them
            // all trivially pass without patching any of them.
            phantom.transform.position = point;

            PackCharge.NoteGrant(name);
            __result = phantom;

            if (SPConfig.Verbose.Value)
                Plugin.Log.LogInfo($"granted '{name}' from {pack.m_shared.m_name} ({pack.m_durability:0.#} left)");
        }

        // ---------------------------------------------------------------------------------------
        // Guard prefixes.
        //
        // Callers dereference the station we hand back (Hud.SetupPieceInfo calls ShowAreaMarker()
        // and reads m_icon; HaveRequirements may call GetLevel). Rather than hope the copied fields
        // are enough - and re-verify that every game patch - we make the phantom simply unable to
        // execute vanilla code. IsPhantom is an O(1) reference-identity check.
        // ---------------------------------------------------------------------------------------

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.GetLevel))]
        private static bool GetLevel(CraftingStation __instance, ref int __result)
        {
            if (!PhantomStationRegistry.IsPhantom(__instance)) return true;
            __result = 1;   // no extensions, no levels: packs satisfy base-tier building only
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.GetStationBuildRange))]
        private static bool GetStationBuildRange(CraftingStation __instance, ref float __result)
        {
            if (!PhantomStationRegistry.IsPhantom(__instance)) return true;
            __result = SPConfig.BuildRange.Value;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.CheckUsable))]
        private static bool CheckUsable(CraftingStation __instance, ref bool __result)
        {
            if (!PhantomStationRegistry.IsPhantom(__instance)) return true;
            __result = true;   // no roof, no fire: that is the point of carrying it
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.InUseDistance))]
        private static bool InUseDistance(CraftingStation __instance, ref bool __result)
        {
            if (!PhantomStationRegistry.IsPhantom(__instance)) return true;
            __result = true;   // it is on your back; you are always in range of it
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.ShowAreaMarker))]
        private static bool ShowAreaMarker(CraftingStation __instance) =>
            !PhantomStationRegistry.IsPhantom(__instance);

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.HideMarker))]
        private static bool HideMarker(CraftingStation __instance) =>
            !PhantomStationRegistry.IsPhantom(__instance);

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.GetExtensions))]
        private static bool GetExtensions(CraftingStation __instance, ref System.Collections.Generic.List<StationExtension> __result)
        {
            if (!PhantomStationRegistry.IsPhantom(__instance)) return true;
            __result = __instance.m_attachedExtensions;   // always empty; never scan for real extensions
            return false;
        }
    }
}
