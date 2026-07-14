using System.Collections.Generic;
using StationPacks.Config;
using UnityEngine;

namespace StationPacks.Core
{
    /// <summary>
    /// Builds one hidden <see cref="CraftingStation"/> per vanilla station, used purely as the return
    /// value of our <c>HaveBuildStationInRange</c> postfix.
    ///
    /// The invariant that the whole mod rests on: these must NEVER enter
    /// <c>CraftingStation.m_allStations</c>. That list is what <c>StationExtension</c> scans, so a
    /// registered phantom forge would let your base forge's coolers re-target onto the pack on your
    /// back - silently dropping the real forge's level (level = 1 + extension count) and gating
    /// recipes while you stand next to it.
    ///
    /// We get that for free: <c>CraftingStation.Start()</c> is the only method that calls
    /// <c>m_allStations.Add(this)</c> (verified in docs/RECON.md), and Unity never runs Start() on a
    /// component added to an INACTIVE GameObject. So we create the holder, deactivate it, and only
    /// then AddComponent. Order matters - AddComponent-then-deactivate would register.
    /// </summary>
    public static class PhantomStationRegistry
    {
        /// <summary>Keyed by the station's <c>m_name</c> token (e.g. "$piece_workbench").</summary>
        private static readonly Dictionary<string, CraftingStation> ByStationName =
            new Dictionary<string, CraftingStation>();

        /// <summary>Keyed by the station's prefab name (e.g. "piece_workbench"). Pack definitions use this.</summary>
        private static readonly Dictionary<string, string> PrefabToStationName =
            new Dictionary<string, string>();

        /// <summary>Reference identity set, so the guard prefixes can answer "is this mine?" in O(1).</summary>
        private static readonly HashSet<CraftingStation> Phantoms = new HashSet<CraftingStation>();

        private static GameObject _holder;

        public static bool IsPhantom(CraftingStation station) =>
            station != null && Phantoms.Contains(station);

        /// <summary>Resolves a station prefab name ("piece_workbench") to its m_name token ("$piece_workbench").</summary>
        public static string StationNameFor(string stationPrefab) =>
            PrefabToStationName.TryGetValue(stationPrefab, out var n) ? n : null;

        public static CraftingStation Get(string stationName) =>
            ByStationName.TryGetValue(stationName, out var s) && s != null ? s : null;

        /// <summary>
        /// Scans ZNetScene for every prefab carrying a CraftingStation and mirrors it into a phantom.
        /// Scanning rather than hardcoding names means modded stations get pack support for free, and
        /// it is why we never have to guess an m_name token.
        /// </summary>
        public static void Build()
        {
            var scene = ZNetScene.instance;
            if (scene == null)
            {
                Plugin.Log.LogWarning("ZNetScene not ready; phantom stations not built.");
                return;
            }

            // The invariant, measured rather than assumed.
            int before = CraftingStation.m_allStations.Count;

            Clear();

            _holder = new GameObject("StationPacks_Phantoms");
            // Deactivate BEFORE adding any component. This is the entire trick.
            _holder.SetActive(false);
            Object.DontDestroyOnLoad(_holder);

            foreach (var prefab in scene.m_prefabs)
            {
                if (prefab == null) continue;
                var src = prefab.GetComponent<CraftingStation>();
                if (src == null || string.IsNullOrEmpty(src.m_name)) continue;
                if (ByStationName.ContainsKey(src.m_name)) continue;

                var go = new GameObject("SP_Phantom_" + prefab.name);
                go.transform.SetParent(_holder.transform, false); // inherits the inactive state
                var cs = go.AddComponent<CraftingStation>();      // Start()/OnEnable() never run

                CopyFrom(src, cs);

                ByStationName[cs.m_name] = cs;
                PrefabToStationName[prefab.name] = cs.m_name;
                Phantoms.Add(cs);

                if (SPConfig.Verbose.Value)
                    Plugin.Log.LogInfo($"  phantom: {prefab.name} -> m_name '{cs.m_name}'");
            }

            int after = CraftingStation.m_allStations.Count;
            if (after != before)
            {
                // If this ever fires, a phantom registered itself and base-forge extensions are at risk.
                Plugin.Log.LogError(
                    $"INVARIANT VIOLATED: m_allStations changed {before} -> {after} while building phantoms. " +
                    "A phantom registered itself; station extensions may re-target onto it. Disabling grants.");
                Clear();
                return;
            }

            Plugin.Log.LogInfo(
                $"Built {ByStationName.Count} phantom stations; m_allStations unchanged at {after}.");

            ValidatePackDefinitions();
        }

        /// <summary>
        /// A pack whose station prefab we never found would simply never grant anything - a silent
        /// dead item. Say so loudly instead; this is the failure mode a game update would cause.
        /// </summary>
        private static void ValidatePackDefinitions()
        {
            foreach (var def in PackDefinition.All)
            {
                if (!PrefabToStationName.ContainsKey(def.StationPrefab))
                {
                    Plugin.Log.LogError(
                        $"{def.DisplayName} points at station prefab '{def.StationPrefab}', which no loaded " +
                        "prefab provides. This pack will never grant anything. Did the game rename it?");
                }
            }
        }

        /// <summary>
        /// Mirrors the fields our callers actually read. Anything that could make the phantom *act*
        /// like a real station (markers, networking, fire/roof gating) is deliberately neutered - and
        /// the guard prefixes in CraftingStationPatches stop it executing vanilla code at all.
        /// </summary>
        private static void CopyFrom(CraftingStation src, CraftingStation dst)
        {
            dst.m_name = src.m_name;               // the match key HaveBuildStationInRange compares
            dst.m_icon = src.m_icon;               // Hud.SetupPieceInfo renders this
            dst.m_craftingSkill = src.m_craftingSkill;
            dst.m_useDistance = src.m_useDistance;
            dst.m_useAnimation = src.m_useAnimation;

            dst.m_rangeBuild = SPConfig.BuildRange.Value;
            dst.m_buildRange = SPConfig.BuildRange.Value;
            dst.m_extraRangePerLevel = 0f;

            // Portable means no shelter and no fire. Also keeps CheckUsable from touching
            // m_roofCheckPoint, which we do not have.
            dst.m_craftRequireRoof = false;
            dst.m_craftRequireFire = false;

            dst.m_discoverRange = 0f;              // no "new station discovered" popups

            // GetExtentionCount() dereferences this; it must be non-null even though it stays empty.
            dst.m_attachedExtensions = new List<StationExtension>();

            dst.m_craftItemEffects = new EffectList();
            dst.m_craftItemDoneEffects = new EffectList();
            dst.m_repairItemDoneEffects = new EffectList();

            dst.m_areaMarker = null;
            dst.m_areaMarkerCircle = null;
            dst.m_inUseObject = null;
            dst.m_haveFireObject = null;
            dst.m_roofCheckPoint = null;
            dst.m_connectionPoint = null;
            dst.m_effectAreaCollider = null;
            dst.m_nview = null;
        }

        private static void Clear()
        {
            ByStationName.Clear();
            PrefabToStationName.Clear();
            Phantoms.Clear();
            if (_holder != null) Object.Destroy(_holder);
            _holder = null;
        }
    }
}
