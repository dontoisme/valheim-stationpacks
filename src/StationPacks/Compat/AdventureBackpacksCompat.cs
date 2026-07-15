using BepInEx.Bootstrap;
using StationPacks.Config;

namespace StationPacks.Compat
{
    /// <summary>
    /// AdventureBackpacks lives in the shoulder/cape slot. Station packs default to the vanilla utility
    /// slot, so out of the box the two no longer contend for a slot - you can wear both. We just note
    /// it once for the log, and flag the one case that still conflicts: forcing packs onto the shoulder.
    /// </summary>
    public static class AdventureBackpacksCompat
    {
        private const string Guid = "vapok.mods.adventurebackpacks";

        public static bool Installed => Chainloader.PluginInfos.ContainsKey(Guid);

        public static void ReportOnce()
        {
            if (!Installed) return;

            if (SPConfig.Slot.Value == SlotMode.Shoulder)
            {
                Plugin.Log.LogInfo(
                    "AdventureBackpacks detected, and StationPacks 'Slot mode' is set to Shoulder - both " +
                    "want the cape slot, so you can wear one or the other. Switch Slot mode to Utility " +
                    "(the default) to wear a backpack and a station pack together.");
                return;
            }

            Plugin.Log.LogInfo(
                "AdventureBackpacks detected. Station packs use the utility slot, so the two coexist - " +
                "backpack on your back, station pack on your belt.");
        }
    }
}
