using System.Collections.Generic;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using StationPacks.Config;
using UnityEngine;

namespace StationPacks.Core
{
    /// <summary>
    /// Creates the six pack items.
    ///
    /// There is no Unity Editor on this project, and a hand-built GameObject will not render on the
    /// character: VisEquipment.SetShoulderEquipped instantiates the item prefab's attach hierarchy
    /// onto the shoulder bone. Cloning a vanilla cape gives us that hierarchy - and the mesh, and the
    /// materials - for free. A different cape per pack means six distinct silhouettes that read as
    /// progression, with zero art assets.
    /// </summary>
    public static class PackRegistry
    {
        public static void Register()
        {
            foreach (var def in PackDefinition.All)
            {
                var prefab = PrefabManager.Instance.CreateClonedPrefab(def.PrefabName, def.ClonedCape);
                if (prefab == null)
                {
                    Plugin.Log.LogError($"Could not clone '{def.ClonedCape}' for {def.PrefabName}; skipping.");
                    continue;
                }

                var itemDrop = prefab.GetComponent<ItemDrop>();
                var shared = itemDrop.m_itemData.m_shared;

                shared.m_name = def.NameToken;
                shared.m_description = def.DescToken;
                // Utility slot (shared with the Megingjord belt / Wishbone): vanilla allows exactly one,
                // so packs are one-at-a-time, and the cape slot stays free for survival capes.
                shared.m_itemType = ItemDrop.ItemData.ItemType.Utility;

                shared.m_weight = def.Weight;
                shared.m_movementModifier = def.MovementModifier;

                // Charge rides on vanilla durability, so the tooltip bar, the "broken" state, and
                // persistence through saves, chests and the network all come for free. m_durabilityDrain
                // stays 0 - vanilla must never touch this; PackCharge owns every decrement.
                shared.m_useDurability = true;
                shared.m_canBeReparied = true;
                shared.m_maxDurability = SPConfig.MaxDurability.Value;
                shared.m_durabilityPerLevel = SPConfig.DurabilityPerLevel.Value;
                shared.m_durabilityDrain = 0f;
                shared.m_useDurabilityDrain = 0f;

                StripInheritedPerks(shared, def);
                shared.m_maxQuality = 3;

                // Prefer a live render of the station model (vanilla isometric style); fall back to the
                // shipped placeholder PNG, and finally to the cloned cape's own icon if both fail.
                var icon = SPConfig.RenderedIcons.Value ? StationIconRenderer.Render(def) : null;
                if (icon == null) icon = LoadIcon(def);
                // Stamp the hammer across the icon so it reads as a building tool. Applied here rather
                // than inside the render so the F7 framing panel still shows the bare station, and so
                // the PNG fallback gets the same affordance. Falls back to the un-stamped icon on failure.
                if (icon != null && SPConfig.HammerOverlay.Value)
                    icon = HammerOverlay.Apply(icon) ?? icon;
                if (icon != null) shared.m_icons = new[] { icon };

                PackVisual.Apply(prefab, def);

                var config = new ItemConfig
                {
                    Name = def.NameToken,
                    Description = def.DescToken,
                    CraftingStation = def.CraftedAt,
                    MinStationLevel = def.MinStationLevel,
                    // The loop that makes this a survival mod and not a cheat: a pack is recharged at
                    // the very station it lets you leave behind.
                    RepairStation = def.RepairedAt,
                    Requirements = def.Requirements,
                };

                ItemManager.Instance.AddItem(new CustomItem(prefab, fixReference: true, config));
                EquippedPackResolver.Register(def);

                Plugin.Log.LogInfo($"Registered {def.DisplayName} (clone of {def.ClonedCape}).");
            }

            AddLocalization();
        }

        /// <summary>
        /// A cloned cape arrives carrying every perk of the cape it came from - the Forge Pack was
        /// shipping CapeWolf's frost resistance, and the Black Forge Pack would have shipped
        /// CapeFeather's fall protection. That quietly destroys the premise: a pack is supposed to
        /// COST you the cape slot, not hand you a cape's best perk for free.
        ///
        /// So we strip everything and then re-apply only the three stats a pack is allowed to have:
        /// weight, a movement penalty, and charge. Anything the game adds to SharedData in a future
        /// update defaults to "not inherited" only if we keep this list honest - see the playtest
        /// check in docs/PLAYTEST.md.
        /// </summary>
        private static void StripInheritedPerks(ItemDrop.ItemData.SharedData shared, PackDefinition def)
        {
            shared.m_armor = 0f;
            shared.m_armorPerLevel = 0f;

            // The frost resistance / fall damage / etc. that live on capes.
            shared.m_damageModifiers = new List<HitData.DamageModPair>();
            shared.m_equipStatusEffect = null;
            shared.m_setStatusEffect = null;
            shared.m_setName = string.Empty;
            shared.m_setSize = 0;

            shared.m_eitrRegenModifier = 0f;
            shared.m_heatResistanceModifier = 0f;

            shared.m_jumpStaminaModifier = 0f;
            shared.m_attackStaminaModifier = 0f;
            shared.m_blockStaminaModifier = 0f;
            shared.m_dodgeStaminaModifier = 0f;
            shared.m_swimStaminaModifier = 0f;
            shared.m_sneakStaminaModifier = 0f;
            shared.m_runStaminaModifier = 0f;
            shared.m_homeItemsStaminaModifier = 0f;

            // The only stats a pack carries. Re-applied last so nothing above can clobber them.
            shared.m_weight = def.Weight;
            shared.m_movementModifier = def.MovementModifier;
        }

        /// <summary>
        /// Loads a pack's inventory icon from the PNG embedded at StationPacks.Assets.icon_&lt;tag&gt;.png.
        /// No Unity Editor and no AssetBundle: read the resource bytes, LoadImage into a Texture2D,
        /// Sprite.Create. Returns null on any failure, in which case the pack keeps the cloned cape's
        /// icon rather than showing nothing.
        /// </summary>
        private static Sprite LoadIcon(PackDefinition def)
        {
            try
            {
                var asm = typeof(PackRegistry).Assembly;
                using (var stream = asm.GetManifestResourceStream(def.IconResource))
                {
                    if (stream == null)
                    {
                        Plugin.Log.LogWarning($"{def.DisplayName}: icon resource '{def.IconResource}' not found.");
                        return null;
                    }
                    var bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);

                    // Jotunn wraps ImageConversion.LoadImage, so our assembly never references the
                    // netstandard-2.1 module directly (see the note in the csproj).
                    var tex = Jotunn.Utils.AssetUtils.LoadImage(bytes);
                    if (tex == null)
                    {
                        Plugin.Log.LogWarning($"{def.DisplayName}: failed to decode icon PNG.");
                        return null;
                    }
                    tex.filterMode = FilterMode.Bilinear;
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"{def.DisplayName}: icon load threw {e.GetType().Name}: {e.Message}");
                return null;
            }
        }

        private static void AddLocalization()
        {
            var translations = new Dictionary<string, string>
            {
                { "sp_pack_depleted", "Your pack is spent. Repair it at the station it replaces." },
            };

            foreach (var def in PackDefinition.All)
            {
                translations[def.NameToken.TrimStart('$')] = def.DisplayName;
                translations[def.DescToken.TrimStart('$')] = def.Description;
            }

            var loc = LocalizationManager.Instance.GetLocalization();
            loc.AddTranslation("English", translations);
        }
    }
}
