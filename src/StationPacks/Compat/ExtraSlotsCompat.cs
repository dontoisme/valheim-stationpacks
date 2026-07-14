using System;
using System.Reflection;
using BepInEx.Bootstrap;

namespace StationPacks.Compat
{
    /// <summary>
    /// Optional integration with a slot-extender so a station pack can be worn alongside a cape or an
    /// AdventureBackpack.
    ///
    /// Everything here is reflection-only and every call is wrapped: we take no assembly reference on
    /// these mods, so StationPacks loads and works normally when they are absent, and a breaking
    /// change on their side degrades us to the shoulder slot instead of failing to load.
    /// </summary>
    public static class ExtraSlotsCompat
    {
        private const string ExtraSlotsGuid = "shudnal.ExtraSlots";
        private const string AzuEpiGuid = "Azumatt.AzuExtendedPlayerInventory";

        private static bool _probed;
        private static MethodInfo _getItemInSlot;
        private static object _api;

        public static bool Available
        {
            get
            {
                Probe();
                return _getItemInSlot != null;
            }
        }

        /// <summary>The item sitting in our dedicated slot, or null if there is no such slot.</summary>
        public static ItemDrop.ItemData GetPackInSlot(Player player)
        {
            Probe();
            if (_getItemInSlot == null || player == null) return null;

            try
            {
                return _getItemInSlot.Invoke(_api, new object[] { "StationPack" }) as ItemDrop.ItemData;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Slot-extender call failed; falling back to the shoulder slot. {e.Message}");
                _getItemInSlot = null;   // stop trying
                return null;
            }
        }

        private static void Probe()
        {
            if (_probed) return;
            _probed = true;

            // Deliberately conservative: we only light this up when a slot-extender is present AND
            // exposes something we can call. Otherwise Auto falls back to the shoulder slot, which
            // always works.
            foreach (var guid in new[] { ExtraSlotsGuid, AzuEpiGuid })
            {
                if (!Chainloader.PluginInfos.TryGetValue(guid, out var info) || info?.Instance == null)
                    continue;

                var asm = info.Instance.GetType().Assembly;
                var apiType = asm.GetType("ExtraSlots.API") ?? asm.GetType("ExtraSlots.ExtraSlots+API");
                var method = apiType?.GetMethod("GetSlotItem", BindingFlags.Public | BindingFlags.Static);
                if (method == null) continue;

                _api = null;   // static
                _getItemInSlot = method;
                Plugin.Log.LogInfo($"Slot-extender detected ({guid}); packs can use a dedicated slot.");
                return;
            }
        }
    }
}
