using BepInEx.Configuration;

namespace StationPacks.Config
{
    /// <summary>How we decide whether the player is wearing a station pack.</summary>
    public enum SlotMode
    {
        /// <summary>Use a slot-extender mod's dedicated slot if one is loaded, else the shoulder.</summary>
        Auto,

        /// <summary>Vanilla shoulder/cape slot. Mutually exclusive with capes and AdventureBackpacks.</summary>
        Shoulder,

        /// <summary>A dedicated slot from ExtraSlots / AzuExtendedPlayerInventory.</summary>
        ExtraSlot,

        /// <summary>Anywhere in the inventory. Easy mode: no opportunity cost.</summary>
        AnyInventory,
    }

    public static class SPConfig
    {
        // --- local ---
        public static ConfigEntry<SlotMode> Slot;
        public static ConfigEntry<bool> Verbose;
        public static ConfigEntry<bool> ShowChargeMessages;
        public static ConfigEntry<bool> AllowGiveCommand;

        // --- balance (server-authoritative in spirit; see docs/PLAN.md section 7) ---
        public static ConfigEntry<float> MaxDurability;
        public static ConfigEntry<float> DurabilityPerLevel;
        public static ConfigEntry<float> PlaceCost;
        public static ConfigEntry<float> RemoveCost;
        public static ConfigEntry<float> RepairCost;
        public static ConfigEntry<float> BuildRange;

        public static void Bind(ConfigFile cfg)
        {
            Slot = cfg.Bind("1 - General", "Slot mode", SlotMode.Auto,
                "Where a station pack must sit to work.\n" +
                "Auto: use a slot-extender mod's dedicated slot if present (leaves the cape slot free for " +
                "AdventureBackpacks), otherwise the shoulder slot.\n" +
                "Shoulder: vanilla cape slot. You give up your cape - that is the intended cost.\n" +
                "AnyInventory: just carry it. No opportunity cost; effectively easy mode.");

            Verbose = cfg.Bind("1 - General", "Verbose logging", false,
                "Log phantom-station construction and every grant. Noisy; for debugging only.");

            ShowChargeMessages = cfg.Bind("1 - General", "Show charge messages", true,
                "Show a top-left message with the pack's remaining charge as it drains.");

            AllowGiveCommand = cfg.Bind("1 - General", "Allow the give command", false,
                "Enables the 'stationpacks give' console command, which hands you all six packs for free.\n" +
                "That skips every recipe and progression gate, so it is off by default - it exists for testing.\n" +
                "The read-only diagnostics (phantoms, invariant, stations) always work.");

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
        }
    }
}
