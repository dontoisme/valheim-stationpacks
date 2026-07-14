using System.Collections.Generic;
using System.Linq;
using Jotunn.Entities;
using Jotunn.Managers;
using StationPacks.Config;
using UnityEngine;

namespace StationPacks.Core
{
    /// <summary>
    /// In-game console commands used to *observe* the acceptance criteria rather than assert them on
    /// faith. Open the console with F5 and type `stationpacks`.
    /// </summary>
    internal sealed class DebugCommands : ConsoleCommand
    {
        public override string Name => "stationpacks";
        public override string Help => "stationpacks <phantoms|invariant|stations|give> - StationPacks diagnostics";

        public static void Register() => CommandManager.Instance.AddConsoleCommand(new DebugCommands());

        public override void Run(string[] args)
        {
            var sub = args.Length > 0 ? args[0].ToLowerInvariant() : "phantoms";

            switch (sub)
            {
                case "phantoms":
                    Phantoms();
                    break;

                case "invariant":
                    Invariant();
                    break;

                case "stations":
                    NearbyStations();
                    break;

                case "give":
                    Give();
                    break;

                default:
                    Console.instance.Print(Help);
                    break;
            }
        }

        /// <summary>sp-br6.2: one phantom per vanilla station, and the registry knows every pack's station.</summary>
        private static void Phantoms()
        {
            Console.instance.Print("<color=yellow>StationPacks phantoms</color>");
            foreach (var def in PackDefinition.All)
            {
                var token = PhantomStationRegistry.StationNameFor(def.StationPrefab);
                var phantom = token == null ? null : PhantomStationRegistry.Get(token);
                var state = phantom == null ? "<color=red>MISSING</color>" : "<color=lime>ok</color>";
                Console.instance.Print($"  {def.DisplayName,-20} {def.StationPrefab,-22} {token ?? "?",-24} {state}");
            }
        }

        /// <summary>
        /// sp-br6.5: the invariant the whole design rests on. If a phantom ever entered
        /// m_allStations, StationExtension could re-target a real forge's coolers onto the pack.
        /// This prints the real stations the game knows about; no phantom may appear.
        /// </summary>
        private static void Invariant()
        {
            var all = CraftingStation.m_allStations;
            int leaked = all.Count(PhantomStationRegistry.IsPhantom);

            Console.instance.Print($"m_allStations.Count = {all.Count}");
            Console.instance.Print(leaked == 0
                ? "<color=lime>OK: no phantom is registered. Station extensions cannot re-target onto a pack.</color>"
                : $"<color=red>LEAK: {leaked} phantom(s) are in m_allStations. Real station levels are at risk.</color>");
        }

        /// <summary>sp-br6.5: prove a nearby real forge keeps its level and its extensions while a pack is worn.</summary>
        private static void NearbyStations()
        {
            var player = Player.m_localPlayer;
            if (player == null) return;

            var near = CraftingStation.m_allStations
                .Where(s => s != null && Vector3.Distance(s.transform.position, player.transform.position) < 30f)
                .ToList();

            if (near.Count == 0)
            {
                Console.instance.Print("No real crafting stations within 30m.");
                return;
            }

            Console.instance.Print("<color=yellow>Real stations within 30m</color>");
            foreach (var s in near)
            {
                Console.instance.Print(
                    $"  {s.m_name,-24} level={s.GetLevel(true)}  extensions={s.GetExtensions().Count}  " +
                    $"range={s.GetStationBuildRange():0.#}");
            }
        }

        /// <summary>
        /// Hands over every pack, skipping every recipe and progression gate. That is a cheat, so it
        /// is gated behind a config flag that defaults to off. The read-only diagnostics above are
        /// harmless and stay available unconditionally.
        /// </summary>
        private static void Give()
        {
            if (!SPConfig.AllowGiveCommand.Value)
            {
                Console.instance.Print(
                    "<color=orange>'give' is disabled - it hands you all six packs for free, skipping " +
                    "every recipe.</color>\nEnable 'Allow the give command' in the StationPacks config " +
                    "if you want it for testing.");
                return;
            }

            var player = Player.m_localPlayer;
            if (player == null) return;

            var given = new List<string>();
            foreach (var def in PackDefinition.All)
            {
                var prefab = ObjectDB.instance.GetItemPrefab(def.PrefabName);
                if (prefab == null)
                {
                    Console.instance.Print($"<color=red>{def.PrefabName} is not in ObjectDB.</color>");
                    continue;
                }
                player.GetInventory().AddItem(def.PrefabName, 1, 1, 0, 0L, "");
                given.Add(def.DisplayName);
            }
            Console.instance.Print($"<color=lime>Gave: {string.Join(", ", given)}</color>");
        }
    }
}
