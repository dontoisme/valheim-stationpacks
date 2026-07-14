# StationPacks — a Valheim mod for mobile build stations

## Context

Building in Valheim is gated on proximity to a crafting station. Every time a build extends past
a workbench's radius you stop building, gather mats, plant another bench, and later tear it down.
Stone is worse: the stonecutter has the same problem and you rarely want a permanent one where
you're working. The friction lands exactly when you're most engaged — mid-build.

Existing mods solve this in one of two unsatisfying ways: **global config toggles** that simply
delete the station requirement (Valheim+, BuildRestrictionTweaks, No Crafting Station
Restrictions, InfinityHammer) — zero cost, zero progression, feels like cheating; or
**CraftyCartsRemake**, which straps stations to a cart you then drag over terrain.

**StationPacks** fills the empty cell: a *wearable* station. Craft a Workbench Pack, wear it in
your cape slot, and build and deconstruct anywhere — but it's heavy, it slows you down, it costs
you your cape, it burns durability with every piece you place, and you go home to a real bench to
recharge it. Mobility you *earn*, not a switch you flip.

Verified across Thunderstore/Nexus/GitHub: **no existing mod delivers build-station function as a
craftable, equippable, cost-bearing item.** The niche is open.

Work is tracked in **Beads** (`bd`), a dependency-aware issue tracker built for coding agents.
Every task below carries explicit acceptance criteria so validation isn't left to vibes.

## Verified ground truth

Confirmed by reflecting over and IL-disassembling the *installed* `assembly_valheim.dll` — not
from memory or docs.

- **Game:** Valheim at `D:\SteamLibrary_500SSD\steamapps\common\Valheim`, Unity `6000.0.61f1`.
  Reference `assembly_valheim.dll` + `assembly_utils.dll` (`Assembly-CSharp.dll` is a 23 KB stub).
- **Box:** .NET SDK 9, git, VSCode. **No Unity Editor** — the mod must be built entirely from
  cloned vanilla prefabs, no AssetBundle. **No Go, Node, or npm.**
- **BepInEx:** `winhttp.dll` + `doorstop_config.ini` are present and already armed
  (`target_assembly=BepInEx\core\BepInEx.Preloader.dll`), but the `BepInEx/` folder is **missing**
  — a Steam file-verify wiped it. Restoring the folder alone re-arms it.

### The one hook that does everything

```csharp
static CraftingStation CraftingStation.HaveBuildStationInRange(string name, Vector3 point)
```

An IL call-graph scan found **exactly three** vanilla callers, whose transitive callers are
*precisely* the requested scope and nothing else:

| Caller | Reached from | Gate |
|---|---|---|
| `Player.HaveRequirements(Piece, RequirementMode)` | `Player.UpdatePlacement`, `PieceTable.UpdateAvailable`, `Hud.UpdatePieceBuildStatus` | **build placement** |
| `Player.CheckCanRemovePiece(Piece)` | `Player.RemovePiece()` **and** `Player.Repair(ItemData, Piece)` | **deconstruct + hammer-repair** |
| `Hud.SetupPieceInfo(Piece)` | build HUD | **requirement icon / greying** |

Its body (IL-verified) matches **by `m_name` string** and reads `transform.position` **live** on
every call:

```csharp
foreach (CraftingStation s in m_allStations) {
    if (s.m_name != name) continue;
    Vector3 p = point; p.y = s.transform.position.y;            // y-flattened
    if (Vector3.Distance(s.transform.position, p) < s.GetStationBuildRange()) return s;
}
return null;
```

**Portable item-crafting is excluded for free.** The crafting/repair GUI reads
`Player.m_currentStation`, set *only* by `CraftingStation.Interact`. We never touch it, so the
crafting panel stays unavailable — the chosen scope is enforced by construction, not discipline.

Also verified: `m_allStations.Add(this)` happens in **`Start()`** (not `Awake`); `OnEnable` only
adds to the `Instances` updater list; `CustomUpdate` early-returns when `m_nview == null`;
`GetExtensions()` null-checks `m_areaMarker` and `m_effectAreaCollider`. `ItemDrop.ItemData` has
first-class `m_durability` / `GetMaxDurability(int)`, and `Recipe.m_repairStation` exists.

## Design decisions (confirmed)

- **Slot:** vanilla shoulder/cape slot (`Humanoid.m_shoulderItem`), as AdventureBackpacks does.
- **Scope:** build placement + deconstruct/repair only. **No** portable item-crafting.
- **Balance:** progression-gated recipes **+** durability/charges **+** weight/movement penalty.
- **Stations:** all six build stations. **No** station extensions or station levels.

## Architecture

### 1. Phantom stations — never in `m_allStations`

The tempting approach (parent a real `CraftingStation` to the player and let vanilla find it) is a
**trap**: it enters `m_allStations`, so `StationExtension.FindClosestStationInRange` can pick the
portable forge as the closest station for your *base* forge's coolers. The extensions re-target,
and the real forge's `GetLevel()` — which is `1 + extension count` — silently drops, gating
recipes while you stand next to it. Reject it.

Instead, build phantoms vanilla can never see:

```csharp
holder = new GameObject("StationPacks_Phantoms");
holder.SetActive(false);              // inactive FIRST
Object.DontDestroyOnLoad(holder);

// for each prefab in ZNetScene.instance.m_prefabs carrying a CraftingStation:
go = new GameObject("SP_" + prefab.name);
go.transform.SetParent(holder.transform, false);
cs = go.AddComponent<CraftingStation>();   // Start()/OnEnable() NEVER run
CopyFields(prefab.GetComponent<CraftingStation>(), cs);
map[cs.m_name] = cs;                       // keyed by "$piece_workbench", etc.
```

`AddComponent` on an **inactive** GameObject never runs `Start`/`OnEnable`, so the phantom never
registers, never gets a `ZNetView`, never spawns markers. Use a `DontDestroyOnLoad` holder — **not**
the Player GameObject, which is destroyed on death/respawn, leaving a Unity-null phantom that
silently stops working. Discover stations by **scanning `ZNetScene`**, not hardcoded names: modded
stations then work for free.

Field copy: `m_name` (the match key) and `m_icon` verbatim; ranges copied;
`m_craftRequireRoof`/`m_craftRequireFire` forced **false** (portable ⇒ no shelter, no fire);
`m_discoverRange = 0` (no "new station discovered" popups); `m_attachedExtensions` and any
`EffectList` set to empty non-null; `m_areaMarker`, `m_areaMarkerCircle`, `m_nview` null.

The phantom **must** be a live component on a live GameObject: Unity's overloaded `operator==`
makes any object with a null native pointer compare `== null`, so `new CraftingStation()` would
read as null to every caller. There is no detached-object path.

### 2. The postfix

```csharp
[HarmonyPostfix, HarmonyPatch(typeof(CraftingStation), nameof(HaveBuildStationInRange))]
static void Postfix(string name, Vector3 point, ref CraftingStation __result)
```

Guards, in order — bail unless all pass:
- `__result != null` → **return** (never override a real station; stays compatible with
  V+/InfinityHammer-style mods that already returned one).
- `Player.m_localPlayer == null`, or `ZNet.instance.IsDedicated()` → return.
- `Vector3.Distance(point, Player.m_localPlayer.transform.position) > 64f` → return. The static
  takes **no `Player` argument**, so other mods (PlanBuild, InfinityHammer, base scanners) can call
  it for arbitrary points; without this they'd get a free station.
- `EquippedPackResolver.TryGetPack(player)` yields a pack matching `name` with `m_durability > 0`.

Then `phantom.transform.position = point;` (transforms work on inactive GameObjects — this makes
every downstream distance check trivially pass) and `__result = phantom;`, recording
`LastGrant = (name, Time.frameCount)` for the charge system.

Runs **per-piece, per-frame** with the build menu open, so it must stay O(1): a dictionary lookup
plus a direct `m_shoulderItem` read.

### 3. Phantom guard prefixes

Callers dereference the returned station (`Hud.SetupPieceInfo` calls `ShowAreaMarker()`, reads
`m_icon`; `HaveRequirements` may call `GetLevel`). Rather than hope the copied fields suffice, make
the phantom **unable to execute vanilla code**, keyed on an O(1) `HashSet` identity check:

| Patch | Behavior |
|---|---|
| `CraftingStation.GetLevel(bool)` | `__result = 1` (configurable), skip original |
| `CraftingStation.CheckUsable(Player, bool)` | `__result = true`, skip |
| `CraftingStation.GetStationBuildRange()` | `__result = <config>`, skip |
| `CraftingStation.ShowAreaMarker()` / `HideMarker()` | no-op |
| `CraftingStation.InUseDistance(Humanoid)` | `__result = true`, skip |

~30 lines that turn a class of possible NREs into a non-issue.

### 4. Items — six cloned capes, no Unity

`VisEquipment.SetShoulderEquipped` instantiates the item prefab's attach hierarchy onto the
shoulder bone, so a hand-built GameObject won't render. Cloning a cape gives that for free:

```csharp
PrefabManager.Instance.CreateClonedPrefab("PackWorkbench", "CapeDeerHide");
```

Use a **different, tier-appropriate cape per pack** (deer hide → workbench, troll hide →
stonecutter, wolf → forge, lox → artisan, feather → black forge, late-game cape → galdr): six
distinct silhouettes that read as progression, zero art, no Editor. Icons: 128×128 PNGs as
`EmbeddedResource` (`Texture2D.LoadImage` + `Sprite.Create`); v0 can reuse the source cape's icon
so art never blocks code. A modeled backpack mesh is a v2 AssetBundle task, deliberately deferred.

> If we ever recolor rather than swap capes: `new Material(src)` and assign to
> `renderer.materials`. Mutating `sharedMaterial` recolors **every** deer cape in the world,
> including other players'.

### 5. Balance

**Weight/movement:** `m_shared.m_weight` (~8–20 by tier) and `m_movementModifier` (-0.03 … -0.08).
Shoulder weight counts toward carry weight automatically.

**Durability** uses the vanilla field, so the tooltip bar, the "broken" state, and persistence
across logout/chests come free. Set `m_useDurability = true`, `m_maxDurability` (config),
`m_durabilityPerLevel`, `m_canBeReparied = true`, and `m_durabilityDrain = 0` so vanilla never
touches it — **we** own the decrement.

Charge only when the pack actually satisfied the gate (else it drains while you stand in your own
base). Postfix `Player.PlacePiece` / `Player.RemovePiece` / `Player.Repair`: if it succeeded, the
piece required a station, and `LastGrant` matches that station name **on the same frame**, drain
the pack. Placement costs more than removal.

**At zero:** Valheim does not destroy items at 0 durability. The pack stays equipped, shows
"broken", and stops granting — the desired behavior, for free.

**Recharge:** set each pack's `CustomRecipe.m_repairStation` to its *matching real station*, so a
Workbench Pack repairs at a workbench and a Black Forge Pack at a black forge. Vanilla
`InventoryGui` repair then works with **zero code**, closing the thematic loop.

**Progression gating** via `Recipe.m_craftingStation` + `m_minStationLevel` and tiered materials.

### 6. The AdventureBackpacks conflict

Both want the shoulder slot. This is **not a crash — it's exclusivity**: vanilla allows one
shoulder item, so you wear a station pack *or* an AdventureBackpack.

1. **Default:** coexist by exclusion. Detect AB via `Chainloader.PluginInfos`, log at INFO, show a
   one-time in-game notice. No hard-fail, no warn-spam.
2. **Put the slot behind one seam.** Every "is a pack equipped?" question goes through
   `EquippedPackResolver.TryGetPack(Player)`, with
   `SlotMode = Auto | Shoulder | ExtraSlot | AnyInventory`. `Auto` uses an **ExtraSlots** dedicated
   slot when present (freeing the shoulder for AB), else the shoulder. This turns the biggest
   compatibility risk into a config line.
3. **Soft-dep only.** `DependencyFlags.SoftDependency` for load order; call ExtraSlots /
   AzuExtendedPlayerInventory purely by **reflection**, so our DLL carries no assembly reference and
   cannot fail to load if they're absent.

### 7. Multiplayer

Build placement is **client-authoritative**: the client runs `HaveRequirements`, then instantiates
the piece and creates a ZDO the server merely stores and replicates. The dedicated server has no
`Player` for remote clients and **never validates builds**.

- Client-side-only patching is correct and sufficient.
- **The mod is unenforceable by design** — a modded client could zero the costs. Not a new exploit
  class (a cheat client could already place anything). ServerSync is for *balance consistency*, not
  security. Don't oversell it.
- **Install:** required on every client that wants to use packs *or see other players' packs* (an
  unknown shoulder-item hash renders no cape). Install server-side too, for prefab availability and
  config sync.
- Jotunn `NetworkCompatibilityMode.EveryoneMustHaveMod`, with a config toggle for mixed lobbies.

### 8. Toolchain — Jotunn

Jotunn wins **because there's no Unity Editor**: the whole item strategy is clone-a-vanilla-prefab,
and Jotunn has `PrefabManager.CreateClonedPrefab`, embedded-resource sprite loading, localization,
and — critically — `CustomItem`/`CustomRecipe` survive the `ObjectDB.CopyOtherDB` rebuild on world
join, the classic "my item vanished on join" footgun. ItemManager is optimized for
AssetBundle-authored items and has no clone story. Cost is a hard Jotunn dependency, which is
ubiquitous and acceptable.

- `net48` + `Microsoft.NETFramework.ReferenceAssemblies` (**required** — no Framework dev pack here).
- `BepInEx.AssemblyPublicizer.MSBuild` with `<Publicize>true</Publicize>` on `assembly_valheim`
  → direct access to `m_allStations`, `m_shoulderItem`, private methods; no reflection.
- `BepInEx.Core` 5.4.x (brings HarmonyX), `JotunnLib` 2.29.x. No ILRepack on this lane.
- Local refs (`Private=false`) to the game's `Managed/` DLLs — prefer these over NuGet reference
  assemblies, since this Unity 6000.0.61 build may be ahead of them.
- Debug post-build copy → `…\Valheim\BepInEx\plugins\StationPacks\`.

## Phase 0 — Project + Beads bootstrap

Create `C:\Users\donrh\valheim-stationpacks\`, `git init`, and copy **this plan** to
`docs/PLAN.md` in the repo (it becomes the design doc the issues reference).

**Install Beads.** No Go/Node/npm on this box, so use the documented Windows path — a prebuilt,
checksum-verified release:

```powershell
irm https://raw.githubusercontent.com/gastownhall/beads/main/install.ps1 | iex
```

> If the prebuilt release is unavailable, fall back to installing Go and
> `$env:CGO_ENABLED="1"; $env:GOFLAGS="-tags=gms_pure_go"; go install github.com/steveyegge/beads/cmd/bd@latest`
> (the `CGO_ENABLED=0` variant is server-mode-only — no embedded Dolt — and would force
> `bd init --server`). Beads is also a known Windows-AV false positive; verify with
> `Get-FileHash bd.exe -Algorithm SHA256` against the release `checksums.txt` if Defender objects.

Then, in the repo:

```powershell
bd init --quiet          # .beads/ + embedded Dolt + AGENTS.md + git hooks
bd setup claude          # SessionStart hook (bd prime) + CLAUDE.md pointer
```

**Load the work graph.** Beads has a first-class `--acceptance` field and an `epic` issue type, so
acceptance criteria are structured data, not prose buried in a description. Rather than ~40
individual `bd create` calls, author one JSONL file and import the whole graph — `bd import`
accepts `title`, `description`, `acceptance_criteria`, `design`, `issue_type`, `priority`,
`labels`, and inline `dependencies` per line:

```powershell
bd import --dry-run .beads\seed\stationpacks.jsonl   # verify first
bd import .beads\seed\stationpacks.jsonl
bd ready                                              # should surface SP-1.x only
```

`--parent` is **not** a documented JSONL import field, so verify parenting under `--dry-run`; if it
doesn't survive, attach children with `bd update <id> --parent <epic-id>` (documented) and wire
blockers via `bd dep add --file deps.jsonl`. Working loop thereafter:
`bd ready` → `bd update <id> --claim` → work → `bd close <id> --reason "…"`.

## Work breakdown (Beads epics)

Seven epics. Each task gets a real acceptance criterion — the thing that must be *observed*, not
merely coded. `E2-1` is the whole technical risk and should be reachable on day one.

**E1 — Environment & scaffold** *(blocks everything)*
- Restore BepInEx: reinstall `denikson-BepInExPack_Valheim` so `BepInEx/` sits beside
  `valheim.exe`; enable the console in `BepInEx.cfg`. → *Accept: game launches, BepInEx console
  appears, `LogOutput.log` written.*
- Install Jotunn into the profile. → *Accept: Jotunn logs its version on startup.*
- Scaffold the csproj (net48, reference assemblies, publicizer, HarmonyX, JotunnLib, post-build
  copy). → *Accept: `dotnet build` succeeds and drops the DLL into `BepInEx/plugins/StationPacks/`.*
- Hello-world plugin. → *Accept: the plugin's GUID + version appear in the BepInEx console.*
- Recon with `ilspycmd`: does `CraftingStation` carry `[RequireComponent(typeof(ZNetView))]`? Dump
  the six real station `m_name` tokens. → *Accept: written into `docs/RECON.md`; names are quoted
  from the assembly, not guessed.*

**E2 — Phantom station core** *(the entire technical risk)*
- **E2-1 — "The piece un-greys."** One postfix, hardcoded:
  `if (__result == null && name == "$piece_workbench") __result = phantom;`. No item, no durability.
  → *Accept: standing in the wilderness far from any bench, a wood wall goes from greyed-out to
  buildable in the hammer menu, places successfully, and deconstructs without printing
  `$msg_missingstation`. Zero exceptions in the console.* **If this passes, the mod is real.**
- Phantom registry: scan `ZNetScene`, build inactive-GO phantoms for every station.
  → *Accept: a debug command logs one phantom per vanilla station, and `m_allStations.Count` is
  **byte-identical** before and after construction.*
- Guard prefixes (`GetLevel`, `CheckUsable`, `GetStationBuildRange`, `ShowAreaMarker`, `HideMarker`,
  `InUseDistance`). → *Accept: opening every build tab with hammer, hoe, and cultivator, with all
  six phantoms live, produces zero exceptions.*
- Safety guards (local-player, dedicated-server, 64 m distance, `__result != null` bailout).
  → *Accept: a unit-ish in-game test calling the static for a far-away point returns null.*
- **Extension-safety regression test** — the bug this design exists to prevent. → *Accept: standing
  next to a real forge with a Forge Pack equipped, the base forge's `GetLevel()` is unchanged and
  its coolers remain attached to it.*
- **Scope regression test.** → *Accept: with a pack equipped, the crafting panel remains
  unavailable — proving we did not accidentally ship portable item-crafting.*

**E3 — Pack items**
- Workbench Pack as a Jotunn `CustomItem` cloned from `CapeDeerHide`, `ItemType.Shoulder`, recipe at
  the workbench. Grant now routes through `EquippedPackResolver`. → *Accept: unequip → piece greyed;
  equip → buildable. The pack renders on the character's back.*
- Remaining five packs + progression-gated recipes + per-tier weight/movement modifier.
  → *Accept: each pack enables exactly its own station and no other; each is craftable only at its
  gating station/level.*
- Icons + English localization. → *Accept: no `$sp_…` raw tokens visible anywhere in the UI.*

**E4 — Durability & charges**
- Vanilla durability fields + frame-matched drain on place/remove/repair + depletion message +
  `m_repairStation`. → *Accept: place ~100 walls → the pack breaks → building is blocked → repair at
  a real workbench → building works again.*
- Persistence. → *Accept: a partially-drained pack retains its exact durability across a logout and
  across a round-trip through a chest.*
- No false drain. → *Accept: building inside a real workbench's radius with a pack equipped drains
  **zero** durability.*

**E5 — Compatibility & config**
- `EquippedPackResolver` slot modes + ServerSync'd balance config.
  → *Accept: flipping `SlotMode` takes effect without a code change; a server-side balance value
  overrides the client's.*
- AdventureBackpacks detection notice; ExtraSlots/AzuEPI reflection path.
  → *Accept: with AB installed, exactly one informational notice appears and neither mod throws.
  With ExtraSlots installed and `SlotMode=Auto`, a pack and an AdventureBackpack are worn
  simultaneously.*

**E6 — Multiplayer** *(needs a second install or a second machine — no dedicated server on this box)*
- Two clients (one vanilla) + a server. → *Accept: placements made with a phantom station persist
  after a server round-trip; the vanilla client does not crash (it merely renders no cape); a
  dropped pack does not spam `ZNetScene` instantiate errors on the modded client.*
- Pick the final `NetworkCompatibility` mode. → *Accept: the decision and its rationale are recorded
  in `docs/PLAN.md`.*

**E7 — Release**
- `manifest.json` (deps: `denikson-BepInExPack_Valheim`, `ValheimModding-Jotunn`), 256×256 icon,
  README, CHANGELOG, MIT license. → *Accept: `tcli build` produces a valid package.*
- Publish. → *Accept: the package installs cleanly from Thunderstore into a fresh r2modman profile
  and E2-1's wilderness test passes on that profile.*

## Verification

The acceptance criteria above *are* the test plan. The four that actually matter:

1. **E2-1** — the concept, provable in minutes: far from any bench, a wood wall goes from greyed-out
   to placeable and deconstructs cleanly.
2. **Extension safety** — a Forge Pack must not alter a real forge's level or steal its coolers.
   Assert in code that `m_allStations.Count` never changes.
3. **Scope** — the crafting panel must stay unavailable with a pack equipped.
4. **Durability round-trip** — drain to zero, building blocked, repair at the real station, working
   again, value survives logout.

Plus: a clean BepInEx console through a full build/deconstruct/repair session with every build tool.

## Open questions / risks

- `[RequireComponent(typeof(ZNetView))]` on `CraftingStation` — unknown; resolved in E1 recon. If
  present, `AddComponent` silently adds a ZNetView and we `DestroyImmediate` it.
- `bd import` may not honor `--parent`; fall back to `bd update --parent` (verified in Phase 0 with
  `--dry-run` before importing for real).
- PlanBuild / InfinityHammer call `HaveRequirements` in bulk; the `__result != null` bailout and the
  distance guard should cover them, but worth an explicit test if those mods matter to you.
- The AdventureBackpacks slot exclusivity is a **product** decision as much as a technical one; the
  `EquippedPackResolver` seam keeps it cheap to reverse.
