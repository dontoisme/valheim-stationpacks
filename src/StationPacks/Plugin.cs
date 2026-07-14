using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using StationPacks.Compat;
using StationPacks.Config;
using StationPacks.Core;

namespace StationPacks
{
    /// <summary>
    /// Wearable crafting stations. Build and deconstruct in the field, at the cost of your cape slot,
    /// your carry weight, and a charge you can only refill at the bench you left behind.
    ///
    /// The whole feature is one Harmony postfix on CraftingStation.HaveBuildStationInRange - see
    /// Patches/CraftingStationPatches.cs and docs/PLAN.md.
    /// </summary>
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency("shudnal.ExtraSlots", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Azumatt.AzuExtendedPlayerInventory", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("vapok.mods.adventurebackpacks", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "donrh.stationpacks";
        public const string Name = "StationPacks";
        public const string Version = "0.1.0";

        internal static ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            SPConfig.Bind(Config);

            _harmony = new Harmony(Guid);
            _harmony.PatchAll();

            // Cloning vanilla capes requires the vanilla prefabs to exist, so we wait for Jotunn's
            // signal rather than racing ObjectDB in Awake.
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;

            // Phantom stations mirror ZNetScene's station prefabs, which are only registered once a
            // world is entered.
            ZNetSceneReady.Subscribe(PhantomStationRegistry.Build);

            DebugCommands.Register();

            Log.LogInfo($"{Name} {Version} loaded.");
        }

        private void OnVanillaPrefabsAvailable()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;

            PackRegistry.Register();
            AdventureBackpacksCompat.ReportOnce();
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();
    }

    /// <summary>
    /// Small shim: run an action once ZNetScene.Awake has completed. Jotunn has no event for this and
    /// PhantomStationRegistry needs the station prefabs, which only exist inside a loaded world.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    internal static class ZNetSceneReady
    {
        private static System.Action _callback;

        public static void Subscribe(System.Action callback) => _callback = callback;

        [HarmonyPostfix]
        private static void Postfix() => _callback?.Invoke();
    }
}
