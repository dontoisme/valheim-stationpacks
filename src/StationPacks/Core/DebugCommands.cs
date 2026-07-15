using System.Collections.Generic;
using System.Linq;
using Jotunn.Entities;
using Jotunn.Managers;
using StationPacks.Config;
using UnityEngine;

namespace StationPacks.Core
{
    /// <summary>
    /// In-game console diagnostics, all gated behind the 'Enable dev tools' config so normal players
    /// never see them. Open the console with F5 and type `stationpacks`.
    /// </summary>
    internal sealed class DebugCommands : ConsoleCommand
    {
        public override string Name => "stationpacks";
        public override string Help =>
            "stationpacks <give|phantoms|invariant|stations|buildstations|meshes> - StationPacks dev diagnostics";

        public override List<string> CommandOptionList() =>
            new List<string> { "give", "phantoms", "invariant", "stations", "buildstations", "meshes" };

        public static void Register() => CommandManager.Instance.AddConsoleCommand(new DebugCommands());

        public override void Run(string[] args)
        {
            if (!SPConfig.DevTools.Value)
            {
                Console.instance.Print("<color=orange>StationPacks dev tools are off. Enable 'Enable dev tools' " +
                                       "in the config to use these commands.</color>");
                return;
            }

            var sub = args.Length > 0 ? args[0].ToLowerInvariant() : "phantoms";
            switch (sub)
            {
                case "give": Give(); break;
                case "phantoms": Phantoms(); break;
                case "invariant": Invariant(); break;
                case "stations": NearbyStations(); break;
                case "buildstations": ListBuildStations(); break;
                case "meshes": ListMeshes(args); break;
                default: Console.instance.Print(Help); break;
            }
        }

        /// <summary>Hands over every pack, skipping progression - dev convenience.</summary>
        private static void Give()
        {
            var player = Player.m_localPlayer;
            if (player == null) return;
            var given = new List<string>();
            foreach (var def in PackDefinition.All)
            {
                if (ObjectDB.instance.GetItemPrefab(def.PrefabName) == null)
                {
                    Console.instance.Print($"<color=red>{def.PrefabName} not in ObjectDB.</color>");
                    continue;
                }
                player.GetInventory().AddItem(def.PrefabName, 1, 1, 0, 0L, "");
                given.Add(def.DisplayName);
            }
            Console.instance.Print($"<color=lime>Gave: {string.Join(", ", given)}</color>");
        }

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

        private static void Invariant()
        {
            var all = CraftingStation.m_allStations;
            int leaked = all.Count(PhantomStationRegistry.IsPhantom);
            Console.instance.Print($"m_allStations.Count = {all.Count}");
            Console.instance.Print(leaked == 0
                ? "<color=lime>OK: no phantom is registered. Station extensions cannot re-target onto a pack.</color>"
                : $"<color=red>LEAK: {leaked} phantom(s) in m_allStations. Real station levels are at risk.</color>");
        }

        private static void NearbyStations()
        {
            var player = Player.m_localPlayer;
            if (player == null) return;
            var near = CraftingStation.m_allStations
                .Where(s => s != null && Vector3.Distance(s.transform.position, player.transform.position) < 30f)
                .ToList();
            if (near.Count == 0) { Console.instance.Print("No real crafting stations within 30m."); return; }
            Console.instance.Print("<color=yellow>Real stations within 30m</color>");
            foreach (var s in near)
                Console.instance.Print($"  {s.m_name,-24} level={s.GetLevel(true)}  extensions={s.GetExtensions().Count}");
        }

        /// <summary>Which stations gate building (m_craftingStation of some Piece), with counts.</summary>
        private static void ListBuildStations()
        {
            var scene = ZNetScene.instance;
            if (scene == null) { Print("ZNetScene not ready."); return; }
            var counts = new Dictionary<string, int>();
            foreach (var prefab in scene.m_prefabs)
            {
                var piece = prefab != null ? prefab.GetComponent<Piece>() : null;
                var station = piece != null ? piece.m_craftingStation : null;
                if (station == null || string.IsNullOrEmpty(station.m_name)) continue;
                counts.TryGetValue(station.m_name, out var c);
                counts[station.m_name] = c + 1;
            }
            Print("=== stations that gate building (piece count) ===");
            foreach (var kv in counts.OrderByDescending(k => k.Value))
                Print($"  {kv.Key,-28} {kv.Value} piece(s)");
            Print(counts.Count == 0 ? "  (none found)" : $"  {counts.Count} station(s) gate building.");
        }

        /// <summary>Lists a prefab's mesh parts, marking the ones a pack would mount. Writes to the log too.</summary>
        private static void ListMeshes(string[] args)
        {
            if (args.Length < 2) { Print("Usage: stationpacks meshes <prefabName> [all]"); return; }
            var prefab = PrefabManager.Instance.GetPrefab(args[1]);
            if (prefab == null) { Console.instance.Print($"<color=orange>No prefab named '{args[1]}'.</color>"); return; }
            bool showAll = args.Length > 2 && args[2].ToLowerInvariant() == "all";

            var used = new HashSet<string>(PackVisual.PickedPartNames(prefab, null));
            Print($"=== mesh parts of {args[1]} ({(showAll ? "all" : "used")}) ===");
            int shown = 0, hidden = 0;
            foreach (var r in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
                var mesh = r is SkinnedMeshRenderer smr ? smr.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh == null) continue;
                bool isUsed = used.Contains(r.gameObject.name);
                if (!isUsed && !showAll) { hidden++; continue; }
                var s = mesh.bounds.size;
                var kind = r is SkinnedMeshRenderer ? "skin" : "mesh";
                var reason = isUsed ? "USED " : r.gameObject.name.ToLowerInvariant().Contains("destruction") ? "destr" : "off  ";
                Print($"  [{reason}][{kind}] {r.gameObject.name,-28} {s.x:0.##} x {s.y:0.##} x {s.z:0.##}");
                shown++;
            }
            Print($"  {shown} shown" + (hidden > 0 ? $", {hidden} hidden (add 'all')." : "."));
        }

        private static void Print(string line)
        {
            if (Console.instance != null) Console.instance.Print(line);
            Plugin.Log.LogInfo("[cmd] " + line);
        }
    }
}
