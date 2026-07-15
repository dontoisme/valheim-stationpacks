using BepInEx.Configuration;
using UnityEngine;

namespace StationPacks.Config
{
    /// <summary>Which slot a station pack must sit in to work.</summary>
    public enum SlotMode
    {
        /// <summary>Vanilla utility slot (shared with the Megingjord belt / Wishbone). One at a time; frees the cape.</summary>
        Utility,

        /// <summary>Vanilla shoulder/cape slot. Costs you your cape instead of your belt.</summary>
        Shoulder,

        /// <summary>Anywhere in the inventory. Easy mode: no opportunity cost, and no 'one at a time'.</summary>
        AnyInventory,
    }

    public static class SPConfig
    {
        // --- local ---
        public static ConfigEntry<SlotMode> Slot;
        public static ConfigEntry<bool> Verbose;
        public static ConfigEntry<bool> ShowChargeMessages;
        public static ConfigEntry<bool> ShowBackMesh;
        public static ConfigEntry<bool> HideCape;

        // --- dev tooling (gated off for players) ---
        public static ConfigEntry<bool> DevTools;
        public static ConfigEntry<KeyboardShortcut> TuneKey;

        // --- balance (server-authoritative in spirit; see docs/PLAN.md section 7) ---
        public static ConfigEntry<float> MaxDurability;
        public static ConfigEntry<float> DurabilityPerLevel;
        public static ConfigEntry<float> PlaceCost;
        public static ConfigEntry<float> RemoveCost;
        public static ConfigEntry<float> RepairCost;
        public static ConfigEntry<float> BuildRange;

        public static void Bind(ConfigFile cfg)
        {
            Slot = cfg.Bind("1 - General", "Slot mode", SlotMode.Utility,
                "Where a station pack must sit to work.\n" +
                "Utility: the vanilla utility slot (shared with the Megingjord belt and Wishbone). One pack " +
                "at a time, and your cape slot stays free for survival capes.\n" +
                "Shoulder: the vanilla cape slot - you give up your cape instead of your belt.\n" +
                "AnyInventory: just carry it anywhere. No opportunity cost; effectively easy mode.");

            Verbose = cfg.Bind("1 - General", "Verbose logging", false,
                "Log phantom-station construction and every grant. Noisy; for debugging only.");

            ShowChargeMessages = cfg.Bind("1 - General", "Show charge messages", true,
                "Show a top-left message with the pack's remaining charge as it drains.");

            ShowBackMesh = cfg.Bind("3 - Appearance", "Show station on back", true,
                "Mount a shrunk copy of the emulated station on your back, so each pack looks distinct.\n" +
                "Turn off to keep the plain cape look.");

            HideCape = cfg.Bind("3 - Appearance", "Hide the cape", true,
                "Hide the cape's own cloth so only the backpack shows. Off keeps the cape draping behind\n" +
                "the pack. Only matters when 'Show station on back' is on.");

            MaxDurability = cfg.Bind("2 - Balance", "Max charge", 100f,
                new ConfigDescription("Charge of a quality-1 pack. One point is spent per piece placed.",
                    new AcceptableValueRange<float>(1f, 10000f)));

            DurabilityPerLevel = cfg.Bind("2 - Balance", "Charge per upgrade level", 50f,
                new ConfigDescription("Extra charge for each quality level above 1.",
                    new AcceptableValueRange<float>(0f, 10000f)));

            PlaceCost = cfg.Bind("2 - Balance", "Charge per placement", 1f,
                new ConfigDescription("Charge spent when the pack is what allowed a piece to be placed.",
                    new AcceptableValueRange<float>(0f, 100f)));

            RemoveCost = cfg.Bind("2 - Balance", "Charge per deconstruct", 0.5f,
                new ConfigDescription("Charge spent when the pack is what allowed a piece to be removed.",
                    new AcceptableValueRange<float>(0f, 100f)));

            RepairCost = cfg.Bind("2 - Balance", "Charge per repair", 0.5f,
                new ConfigDescription("Charge spent when the pack is what allowed a piece to be repaired.",
                    new AcceptableValueRange<float>(0f, 100f)));

            BuildRange = cfg.Bind("2 - Balance", "Build range", 20f,
                new ConfigDescription(
                    "Radius the pack projects around you. Vanilla workbench is 20.",
                    new AcceptableValueRange<float>(4f, 64f)));

            DevTools = cfg.Bind("9 - Dev", "Enable dev tools", false,
                "Enables the in-game dev tooling: the 'stationpacks' console commands (give, meshes, back, " +
                "phantoms, invariant, ...) and the back-mesh tuning panel. Off for normal play.");

            TuneKey = cfg.Bind("9 - Dev", "Tuning panel key", new KeyboardShortcut(KeyCode.F6),
                "Opens the live slider panel for positioning the pack on your back. Only works when " +
                "'Enable dev tools' is on.");
        }
    }
}
