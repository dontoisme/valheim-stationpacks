# StationPacks — Progress

A build log of how this mod came together, for anyone (future me included) picking it up.

## The problem

Building in Valheim is gated on standing near a crafting station. Every time a build creeps past a
workbench's radius you stop, gather mats, plant another bench, and tear it down later. The friction
lands exactly when you're most engaged — mid-build. Existing mods either delete the restriction with
a global config toggle (no cost, feels like cheating) or strap stations to a cart you drag around
(CraftyCartsRemake). Nobody shipped station function as a **craftable, equippable, cost-bearing
item**. That empty niche is StationPacks.

## What it is

Six wearable "station packs" — Workbench, Stonecutter, Forge, Artisan, Black Forge, Galdr Table.
Wear one and you can **build, deconstruct, and repair** anywhere that station is needed, at the cost
of your utility slot, carry weight, and a charge you refill at the very bench you were avoiding. It
does **not** grant portable item-crafting — that's deliberate; it's a building mod.

## How it works (the load-bearing ideas)

- **One hook does the whole feature.** An IL call-graph scan of the shipped `assembly_valheim.dll`
  found that `CraftingStation.HaveBuildStationInRange(string, Vector3)` has exactly three callers —
  build placement, deconstruct/repair, and the build-menu UI. A single Harmony postfix on that
  static method covers the entire requested scope and nothing else. Portable item-crafting is
  excluded *by construction* because the crafting GUI reads a different field
  (`Player.m_currentStation`), which we never touch.
- **Phantom stations that vanilla can never see.** The postfix returns a hidden `CraftingStation`
  built as a component on an **inactive** `DontDestroyOnLoad` GameObject, so `Start()` never runs and
  it never enters `m_allStations`. This matters: a registered phantom would let `StationExtension`
  re-target your *base* forge's coolers onto the pack on your back, silently dropping the real
  forge's level. Verified in-game that `m_allStations.Count` never changes.
- **Charge rides on vanilla durability.** No custom save data — the tooltip bar, "broken" state, and
  save/chest/network persistence come free. Charge is only spent when the pack is what actually
  enabled the action (frame-matched grant check), so building next to a real bench costs nothing.
- **Packs are cloned vanilla capes** (no Unity Editor on the project), which gives a working
  shoulder attach hierarchy for free. Inherited cape perks (frost resist, etc.) are stripped so a
  pack is a trade-off, not a free upgrade.
- **Each pack wears its station on your back.** The back mesh borrows the real station's mesh +
  materials into the cape's `attach_skin` node, then reparents onto the `Spine2` bone so it leans
  with the torso. Selection walks the *active* hierarchy first (skips destruction-fragment subtrees),
  falling back to disabled renderers for stations like the black forge whose renderers sleep at load.

## The release arc

| Version | What landed |
|---|---|
| **0.1.0** | Core: phantom stations, the one hook, six cloned-cape items, durability/charge, recharge-at-station. |
| **0.1.1** | Stripped inherited cape perks (a Forge Pack was secretly granting frost resistance); gated the `give` cheat command; uninstall data-loss warning. |
| **0.2.0** | Appearance: procedural icons + each pack wears its station model on the back (spine-bound). Plugin GUID renamed `donrh.*` → `LosGoobers.*`. Six-pack lineup **settled by data** — a diagnostic that scans every buildable piece's `m_craftingStation` proved the black forge (18 pieces) and galdr table (3) *do* gate building, contradicting the wiki. |
| **0.3.0** | Packs moved from the cape slot to the **vanilla utility slot** (`ItemType.Utility`). Frees the cape slot (wear a wolf cape *and* build on the mountain); vanilla's single utility slot gives one-bench-at-a-time for free; AdventureBackpacks now coexists out of the box. No dependencies. |

## Design decisions worth remembering

- **Utility slot, not a slot-extender.** We considered depending on ExtraSlots for a dedicated slot,
  then chose the vanilla utility slot instead — it gives "one at a time" for free, frees the cape,
  and creates a genuine pack-vs-Megingjord-belt trade-off, all with zero dependencies. The deciding
  factor: the cape slot carries *survival* status effects (frost resistance is required for the
  Mountains), so a pack must not live there.
- **`AttachArmor` is shared.** `SetUtilityEquipped` and `SetShoulderEquipped` both call it, so moving
  packs to the utility slot needed almost no visual rework — hooking the shared `AttachArmor` made
  the back-mesh bind slot-agnostic.
- **Multiplayer is unenforceable by design, and that's fine.** Build placement is client-authoritative;
  the server never validates it. We install everywhere for prefab availability, not security.
- **Deliberate non-goal:** we do *not* enforce "single active pack" for players who install a
  slot-extender that multiplies utility slots. Vanilla's one slot is the only guarantee; stacking is
  their choice.

## Tooling notes

- Build: `dotnet build src/StationPacks/StationPacks.csproj` (net48; game/BepInEx paths in
  `Directory.Build.props`). Debug builds deploy into an r2modman profile.
- Dev tooling (F6 slider panel for back-mesh positioning, `stationpacks` console diagnostics) ships
  in the DLL but is gated behind an `Enable dev tools` config, default off — player-safe without
  stripping.
- Assembly findings that the whole design rests on are in [`docs/RECON.md`](docs/RECON.md); the
  original design in [`docs/PLAN.md`](docs/PLAN.md). Work is tracked with Beads in [`.beads/`](.beads).

## Where it stands

**Shipped:** 0.3.0 on Thunderstore (Los_Goobers team), public MIT repo.

**In progress (uncommitted, still versioned 0.3.0 — bead `sp-iz1.1`):** rendered model icons.
- `StationIconRenderer` renders each pack's icon from the real station mesh via Jotunn `RenderManager`,
  vanilla isometric style. Falls back to the shipped PNG, then the cloned cape's icon.
- Per-pack framing fields on `PackDefinition` (`IconFieldOfView` / `IconDistanceMultiplier` /
  `IconRotationEuler` / `IconRenderSize`). Defaults are the Workbench tuning pass; the Galdr Pack
  carries its own override. Tuned live with the **F7** dev panel (`IconTuningPanel`), whose "Copy bake
  line" now emits paste-ready C# initializers.
- `HammerOverlay` stamps the vanilla Hammer icon as a small bottom-right corner badge (with a drop
  shadow) so a pack reads as a build tool. Reuses the game's own Hammer sprite — no new art. Toggle:
  `3 - Appearance → "Hammer on icons"`. Size/position knobs are the constants at the top of the file.
- Not yet: commit, version bump, Thunderstore repackage. See the rollback note below before playing on
  a friend's 0.3.0 server with a version-bumped build.

**Open:**
- **Multiplayer validation.** Never run over a real network — needs a server + a second client.
- **Feedback collection** on the live builds against common mods.
