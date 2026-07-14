using HarmonyLib;
using StationPacks.Config;
using StationPacks.Core;
using UnityEngine;

namespace StationPacks.Patches
{
    /// <summary>
    /// Charge drain. Each of these fires immediately after vanilla has already consulted
    /// HaveBuildStationInRange, so <see cref="PackCharge"/>'s same-frame grant check reliably
    /// distinguishes "the pack made this possible" from "there was a real bench right there".
    /// </summary>
    [HarmonyPatch]
    internal static class BuildPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
        private static void OnPlacePiece(Player __instance, Piece piece) =>
            PackCharge.SpendFor(__instance, piece, SPConfig.PlaceCost.Value);

        /// <summary>
        /// RemovePiece() takes no arguments and resolves the target itself, so we capture the piece
        /// in a prefix (before it is destroyed) and only spend if the removal actually succeeded.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.RemovePiece))]
        private static void BeforeRemovePiece(Player __instance, out Piece __state)
        {
            __state = null;
            var hovered = __instance.m_hoveringPiece;
            if (hovered != null) __state = hovered;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.RemovePiece))]
        private static void AfterRemovePiece(Player __instance, bool __result, Piece __state)
        {
            if (!__result || __state == null) return;
            PackCharge.SpendFor(__instance, __state, SPConfig.RemoveCost.Value);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.Repair))]
        private static void OnRepair(Player __instance, Piece repairPiece) =>
            PackCharge.SpendFor(__instance, repairPiece, SPConfig.RepairCost.Value);
    }
}
