using System.Collections.Generic;
using StationPacks.Config;

namespace StationPacks.Core
{
    /// <summary>
    /// The single seam through which the whole mod asks "is the player wearing a station pack for this
    /// station?".
    ///
    /// Packs are vanilla utility items by default, so they live in the utility slot (shared with the
    /// Megingjord belt and Wishbone) - one at a time, and the cape slot stays free for survival capes.
    /// The slot the resolver reads is a config line, so a player can put packs on the shoulder slot or
    /// just carry them loose instead.
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
        /// The pack currently granting access to <paramref name="stationName"/>, or null. A depleted
        /// pack is deliberately not returned: at zero charge it stays equipped and simply stops working.
        /// </summary>
        public static ItemDrop.ItemData TryGetPack(Player player, string stationName)
        {
            if (player == null || string.IsNullOrEmpty(stationName)) return null;

            switch (SPConfig.Slot.Value)
            {
                case SlotMode.AnyInventory:
                    var inv = player.GetInventory();
                    if (inv == null) return null;
                    foreach (var item in inv.GetAllItems())
                        if (Matches(item, stationName))
                            return item;
                    return null;

                case SlotMode.Shoulder:
                    return Matches(player.m_shoulderItem, stationName) ? player.m_shoulderItem : null;

                default: // Utility - the vanilla single utility slot
                    return Matches(player.m_utilityItem, stationName) ? player.m_utilityItem : null;
            }
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
