using System.Collections.Generic;
using StationPacks.Compat;
using StationPacks.Config;

namespace StationPacks.Core
{
    /// <summary>
    /// The single seam through which the whole mod asks "is the player wearing a station pack?".
    ///
    /// This exists because AdventureBackpacks (720k+ downloads) also wants the shoulder slot, and
    /// vanilla allows exactly one shoulder item. Routing every check through here turns that
    /// conflict from a code problem into a config line: point the resolver at a slot-extender's
    /// dedicated slot and the two mods coexist.
    /// </summary>
    public static class EquippedPackResolver
    {
        /// <summary>Item shared-name token -> the pack it belongs to. Populated by PackRegistry.</summary>
        private static readonly Dictionary<string, PackDefinition> ByItemName =
            new Dictionary<string, PackDefinition>();

        public static void Register(PackDefinition def) => ByItemName[def.NameToken] = def;

        public static PackDefinition DefinitionOf(ItemDrop.ItemData item) =>
            item != null && ByItemName.TryGetValue(item.m_shared.m_name, out var d) ? d : null;

        /// <summary>
        /// The pack currently granting station access, or null. A depleted pack is deliberately NOT
        /// returned: at zero charge it stays equipped and simply stops working.
        /// </summary>
        public static ItemDrop.ItemData TryGetPack(Player player, string stationName)
        {
            if (player == null || string.IsNullOrEmpty(stationName)) return null;

            switch (EffectiveSlotMode())
            {
                case SlotMode.AnyInventory:
                    var inv = player.GetInventory();
                    if (inv == null) return null;
                    foreach (var item in inv.GetAllItems())
                        if (Matches(item, stationName))
                            return item;
                    return null;

                case SlotMode.ExtraSlot:
                    var slotted = ExtraSlotsCompat.GetPackInSlot(player);
                    return Matches(slotted, stationName) ? slotted : null;

                default: // Shoulder
                    var shoulder = player.m_shoulderItem;
                    return Matches(shoulder, stationName) ? shoulder : null;
            }
        }

        private static SlotMode EffectiveSlotMode()
        {
            var mode = SPConfig.Slot.Value;
            if (mode != SlotMode.Auto) return mode;
            // Auto: prefer a dedicated slot when a slot-extender is loaded, so the cape slot stays
            // free for AdventureBackpacks. Otherwise fall back to the shoulder.
            return ExtraSlotsCompat.Available ? SlotMode.ExtraSlot : SlotMode.Shoulder;
        }

        private static bool Matches(ItemDrop.ItemData item, string stationName)
        {
            var def = DefinitionOf(item);
            if (def == null) return false;
            if (item.m_durability <= 0f) return false;   // depleted: equipped but inert
            return PhantomStationRegistry.StationNameFor(def.StationPrefab) == stationName;
        }
    }
}
