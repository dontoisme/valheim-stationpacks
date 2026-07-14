# Assembly recon

All values below are quoted from the **installed** game assembly, obtained by reflecting over it
with `MetadataLoadContext` and disassembling method bodies with `Mono.Cecil`. Nothing here is from
memory, a wiki, or a decompiler blog post.

- Target: `D:\SteamLibrary_500SSD\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll`
- Engine: Unity `6000.0.61f1`
- Date: 2026-07-14

---

## Q1 — Does `CraftingStation` carry `[RequireComponent(typeof(ZNetView))]`?

**No.** The type has **no custom attributes at all.**

> This was the main open risk in the phantom design. It means `AddComponent<CraftingStation>()`
> will **not** silently attach a `ZNetView`, so there is nothing to `DestroyImmediate` afterwards.

## Q2 — Where does a station register itself into `m_allStations`?

**Only in `Start()`.** Full list of methods that touch the static list:

| Method | Operation |
|---|---|
| `Start()` | `m_allStations.Add(this)` ← **the only Add** |
| `OnDestroy()` | `m_allStations.Remove(this)` |
| `UpdateKnownStationsInRange()` | enumerate |
| `HaveBuildStationInRange()` | enumerate |
| `FindStationsInRange()` | enumerate |
| `FindClosestStationInRange()` | enumerate |
| `.cctor` | initialize |

`Start()` IL, in order: cache `m_nview` via `GetComponent<ZNetView>()`; **early-return if
`m_nview` is truthy but `GetZDO()` is null**; `m_allStations.Add(this)`; deactivate `m_areaMarker`
and cache its `CircleProjector`; `InvokeRepeating("CheckFire", 1, 1)` if `m_craftRequireFire`.

**Consequence — this is the load-bearing fact of the whole mod:** Unity never calls `Start()` (or
`Awake`/`OnEnable`) on a component added to an **inactive** GameObject. So a phantom built as
`AddComponent` on an inactive, `DontDestroyOnLoad` holder:

- never enters `m_allStations` → invisible to `FindClosestStationInRange` / `FindStationsInRange`,
  so **`StationExtension` can never re-target a real forge's coolers onto it**;
- never caches an `m_nview`, never spawns an area marker, never starts `CheckFire`.

Assert `m_allStations.Count` before/after phantom construction. It must not change.

## Q3 — Do the methods our postfix's return value flows into dereference `m_nview`?

**No.** Field access per method:

| Method | Fields it touches |
|---|---|
| `GetLevel(bool)` | *(none directly — delegates to `GetExtentionCount`)* |
| `GetExtentionCount(bool)` | `m_attachedExtensions` |
| `CheckUsable(Player, bool)` | `m_craftRequireRoof`, `m_roofCheckPoint`, `m_craftRequireFire`, `m_haveFire` |
| `GetStationBuildRange()` | `m_buildRange` |
| `GetExtensions()` | `m_updateExtensionTimer`, `m_attachedExtensions`, `m_rangeBuild`, `m_extraRangePerLevel`, `m_buildRange`, `m_areaMarker`, `m_areaMarkerCircle`, `m_effectAreaCollider` |
| `ShowAreaMarker()` / `HideMarker()` | `m_areaMarker` |
| `InUseDistance(Humanoid)` | `m_useDistance` |

None reads `m_nview`, so a phantom with a null `m_nview` is safe. `CheckUsable` short-circuits
cleanly when `m_craftRequireRoof` and `m_craftRequireFire` are both `false` — it never dereferences
`m_roofCheckPoint`. `GetExtentionCount` requires a **non-null** `m_attachedExtensions`.

We still install the guard prefixes: they make the phantom *unable* to execute vanilla code at all,
which is cheaper than re-verifying this table every game patch.

## Q4 — Fields available for the balance model

`ItemDrop.ItemData.SharedData`:

```
Int32   m_maxQuality          Single  m_weight
Single  m_movementModifier    Single  m_scaleWeightByQuality
Boolean m_useDurability       Single  m_maxDurability
Single  m_durabilityPerLevel  Single  m_durabilityDrain
Single  m_useDurabilityDrain  Boolean m_canBeReparied      // vanilla's typo, not ours
String  m_name                ItemType m_itemType
Sprite[] m_icons              String  m_setName
```

`ItemDrop.ItemData`: `Single m_durability`, `Int32 m_quality`,
`Dictionary<String,String> m_customData`.

Durability is a first-class serialized field, so the tooltip bar, the broken state, and
save/chest/network persistence all come for free. **We do not need `m_customData`** or any
base64/`CustomItemData` machinery for v1.

## Q5 — Exact signatures we patch

```csharp
static CraftingStation CraftingStation.HaveBuildStationInRange(string name, Vector3 point)

void Player.PlacePiece(Piece piece, Vector3 pos, Quaternion rot, bool doAttack)
bool Player.RemovePiece()
void Player.Repair(ItemDrop.ItemData toolItem, Piece repairPiece)
bool Player.HaveRequirements(Piece piece, Player.RequirementMode mode)
void Hud.SetupPieceInfo(Piece piece)
```

## Q6 — Station name tokens

Deliberately **not** hardcoded. `CraftingStation.m_name` is a serialized value on the *prefab*, not
a constant in the assembly, so quoting it from the DLL is impossible — and guessing it is exactly
what this document exists to avoid.

Instead, `PhantomStationRegistry` **scans `ZNetScene.instance.m_prefabs`** for `CraftingStation`
components and reads `m_name` off each one at runtime. Pack definitions therefore key off the
**station prefab name** (`piece_workbench`, …), and the `m_name` token is resolved from the live
prefab. This is strictly more robust than a hardcoded table, and it makes **modded** crafting
stations work with packs for free.

The registry logs every discovered `(prefabName, m_name)` pair at startup; that log is the
authoritative list, captured on first run.
