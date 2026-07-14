using System.Collections.Generic;
using Jotunn.Configs;

namespace StationPacks.Core
{
    /// <summary>
    /// One wearable station pack. Everything here is declarative; <see cref="PackRegistry"/> turns it
    /// into a Jotunn CustomItem, and <see cref="PhantomStationRegistry"/> resolves
    /// <see cref="StationPrefab"/> to the live station's <c>m_name</c> token at runtime.
    /// </summary>
    public sealed class PackDefinition
    {
        /// <summary>Prefab name of the item we create, e.g. "SP_PackWorkbench".</summary>
        public string PrefabName;

        /// <summary>Vanilla cape prefab we clone. No Unity Editor, so the mesh + attach hierarchy comes from here.</summary>
        public string ClonedCape;

        /// <summary>
        /// Prefab name of the station this pack emulates, e.g. "piece_workbench".
        /// We deliberately do NOT hardcode the station's m_name token - see docs/RECON.md Q6.
        /// </summary>
        public string StationPrefab;

        /// <summary>Station where the pack is crafted (progression gate).</summary>
        public string CraftedAt;
        public int MinStationLevel;

        /// <summary>Station where the pack is recharged. Its own station, which closes the loop: go home to refill.</summary>
        public string RepairedAt;

        public string DisplayName;
        public string Description;

        public float Weight;
        public float MovementModifier;

        public RequirementConfig[] Requirements;

        /// <summary>Localization token for the item name, e.g. "$sp_pack_workbench".</summary>
        public string NameToken => "$sp_" + PrefabName.ToLowerInvariant();
        public string DescToken => NameToken + "_desc";

        /// <summary>
        /// The six vanilla build stations, roughly in progression order. The cape choice per pack is
        /// cosmetic-only but tier-appropriate, so the packs read as a progression with zero art.
        /// </summary>
        public static readonly List<PackDefinition> All = new List<PackDefinition>
        {
            new PackDefinition
            {
                PrefabName = "SP_PackWorkbench",
                ClonedCape = "CapeDeerHide",
                StationPrefab = CraftingStations.Workbench,
                CraftedAt = CraftingStations.Workbench,
                MinStationLevel = 1,
                RepairedAt = CraftingStations.Workbench,
                DisplayName = "Workbench Pack",
                Description = "A carpenter's bench lashed to a hide frame. Heavy, awkward, and it means you " +
                              "never have to plant another bench mid-build.",
                Weight = 8f,
                MovementModifier = -0.03f,
                Requirements = new[]
                {
                    new RequirementConfig("Wood", 20, 10),
                    new RequirementConfig("LeatherScraps", 8, 4),
                    new RequirementConfig("Flint", 6, 3),
                },
            },
            new PackDefinition
            {
                PrefabName = "SP_PackStonecutter",
                ClonedCape = "CapeTrollHide",
                StationPrefab = CraftingStations.Stonecutter,
                CraftedAt = CraftingStations.Forge,
                MinStationLevel = 1,
                RepairedAt = CraftingStations.Stonecutter,
                DisplayName = "Stonecutter Pack",
                Description = "Chisels, saws and a dust-caked apron. The stone will not cut itself, but at " +
                              "least now it will cut where you are standing.",
                Weight = 14f,
                MovementModifier = -0.06f,
                Requirements = new[]
                {
                    new RequirementConfig("Wood", 10, 5),
                    new RequirementConfig("Iron", 8, 4),
                    new RequirementConfig("Stone", 20, 10),
                },
            },
            new PackDefinition
            {
                PrefabName = "SP_PackForge",
                ClonedCape = "CapeWolf",
                StationPrefab = CraftingStations.Forge,
                CraftedAt = CraftingStations.Forge,
                MinStationLevel = 2,
                RepairedAt = CraftingStations.Forge,
                DisplayName = "Forge Pack",
                Description = "A field anvil and a bellows you will regret carrying uphill.",
                Weight = 16f,
                MovementModifier = -0.07f,
                Requirements = new[]
                {
                    new RequirementConfig("Bronze", 8, 4),
                    new RequirementConfig("DeerHide", 6, 3),
                    new RequirementConfig("Wood", 10, 5),
                },
            },
            new PackDefinition
            {
                PrefabName = "SP_PackArtisan",
                ClonedCape = "CapeLinen",
                StationPrefab = CraftingStations.ArtisanTable,
                CraftedAt = CraftingStations.ArtisanTable,
                MinStationLevel = 1,
                RepairedAt = CraftingStations.ArtisanTable,
                DisplayName = "Artisan Pack",
                Description = "Fine tools, rolled in oilcloth. For work that does not forgive a shaking hand.",
                Weight = 12f,
                MovementModifier = -0.05f,
                Requirements = new[]
                {
                    new RequirementConfig("Iron", 8, 4),
                    new RequirementConfig("Silver", 4, 2),
                    new RequirementConfig("WolfPelt", 6, 3),
                },
            },
            new PackDefinition
            {
                PrefabName = "SP_PackBlackForge",
                ClonedCape = "CapeFeather",
                StationPrefab = CraftingStations.BlackForge,
                CraftedAt = CraftingStations.BlackForge,
                MinStationLevel = 1,
                RepairedAt = CraftingStations.BlackForge,
                DisplayName = "Black Forge Pack",
                Description = "Black metal, still warm. It hums against your spine as you walk.",
                Weight = 20f,
                MovementModifier = -0.08f,
                Requirements = new[]
                {
                    new RequirementConfig("BlackMetal", 12, 6),
                    new RequirementConfig("YggdrasilWood", 8, 4),
                },
            },
            new PackDefinition
            {
                PrefabName = "SP_PackGaldr",
                ClonedCape = "CapeLox",
                StationPrefab = CraftingStations.GaldrTable,
                CraftedAt = CraftingStations.GaldrTable,
                MinStationLevel = 1,
                RepairedAt = CraftingStations.GaldrTable,
                DisplayName = "Galdr Pack",
                Description = "Runes stitched into the lining. Carrying it feels like being watched.",
                Weight = 10f,
                MovementModifier = -0.04f,
                Requirements = new[]
                {
                    new RequirementConfig("Eitr", 20, 10),
                    new RequirementConfig("YggdrasilWood", 8, 4),
                    new RequirementConfig("LinenThread", 10, 5),
                },
            },
        };
    }
}
