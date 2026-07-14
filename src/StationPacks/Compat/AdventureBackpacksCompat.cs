using BepInEx.Bootstrap;
using StationPacks.Config;

namespace StationPacks.Compat
{
    /// <summary>
    /// AdventureBackpacks also lives in the shoulder slot, and vanilla allows exactly one shoulder
    /// item. That is not a crash - it is exclusivity: you wear one or the other.
    ///
    /// We say so once, plainly, and move on. No hard-fail, no repeated warnings. If the player also
    /// runs a slot-extender, Auto mode already resolves the conflict and we stay quiet.
    /// </summary>
    public static class AdventureBackpacksCompat
    {
        private const string Guid = "vapok.mods.adventurebackpacks";

        public static bool Installed => Chainloader.PluginInfos.ContainsKey(Guid);

        public static void ReportOnce()
        {
            if (!Installed) return;

            if (ExtraSlotsCompat.Available && SPConfig.Slot.Value != SlotMode.Shoulder)
            {
                Plugin.Log.LogInfo(
                    "AdventureBackpacks detected. A slot-extender is also present, so station packs use " +
                    "their own slot and the two can be worn together.");
                return;
            }

            Plugin.Log.LogInfo(
                "AdventureBackpacks detected. Both mods use the vanilla shoulder slot, so you can wear a " +
                "backpack or a station pack, but not both at once. To wear both, install a slot-extender " +
                "(ExtraSlots) and leave 'Slot mode' on Auto.");
        }
    }
}
