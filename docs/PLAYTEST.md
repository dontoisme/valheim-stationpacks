# Playtest 1 — checklist

Tick these off as you go. Each maps to a Beads issue; the `bd close` command to run afterwards is
listed at the bottom.

**Launch from r2modman** (the Default profile now has StationPacks + Jotunn + PlantEasily +
AzuCraftyBoxes). Launching from Steam will *not* load StationPacks — that's expected now.

Open the in-game console with **F5**.

---

## Startup — before you touch anything

- [ ] A BepInEx console window opens alongside the game.
- [ ] Console shows `StationPacks 0.1.0 loaded.`
- [ ] Console shows six `Registered ... Pack` lines.
- [ ] After loading a world, console shows
      **`Built N phantom stations; m_allStations unchanged at ...`**
- [ ] It does **NOT** say `INVARIANT VIOLATED`. *(If it does — stop, tell Claude. A phantom
      registered itself and real stations are at risk.)*

---

## 1. `sp-br6.1` — the concept ✅ ALREADY PASSED

Confirmed: wood walls placed in the field with the Workbench Pack, far from any bench. Re-check only
if something else changes.

---

## 2. `sp-br6.2` — phantom registry

```
stationpacks give        (hands you all six packs)
stationpacks phantoms
```

- [ ] All six packs listed, every one shows `ok` — none show `MISSING`.

---

## 3. `sp-br6.5` — station extensions unaffected ⚠️ **the important one**

This is the bug the whole design exists to prevent: a naive implementation lets your **base forge's**
coolers/anvils re-target onto the pack on your back, silently dropping the real forge's level and
gating recipes while you stand right at it.

```
stationpacks invariant
```

- [ ] Prints **`OK: no phantom is registered`**.

Now stand next to a **real forge that has extensions attached** (anvils, bellows, cooler), wearing
the **Forge Pack**:

```
stationpacks stations
```

- [ ] The real forge still reports its full `level=` (not dropped to 1).
- [ ] It still reports its full `extensions=` count.
- [ ] Recipes that need a high-level forge are still craftable at it.

---

## 4. `sp-br6.6` — no portable item-crafting

With a pack equipped, out in the field, open the crafting panel.

- [ ] Only the four vanilla station-free recipes appear (Stone Axe, Club, Hammer, Torch).
- [ ] **No workbench-gated recipes** are craftable. *(If you can craft armour/weapons in the field,
      we accidentally shipped portable crafting — that would make this a cheat mod.)*

---

## 5. `sp-81k.3` — no false drain (likeliest balance bug)

Stand **inside a real workbench's radius** wearing the Workbench Pack. Place ~10 pieces.

- [ ] The pack's durability does **not move at all**. A real bench covered those placements, so the
      pack must not be charged.

---

## 6. `sp-81k.1` — the charge loop

*(Tip: drop "Max charge" in the config to ~10 to make this fast.)*

- [ ] Placing in the field drains charge. *(Already seen: 100 → 96 over four walls.)*
- [ ] Deconstructing in the field drains charge (less than placing).
- [ ] Repairing a piece in the field drains charge.
- [ ] At **zero** the pack stays equipped, shows as broken, and pieces go back to greyed-out.
- [ ] Repairing the pack **at a real workbench** restores it, and it works again.

---

## 7. `sp-81k.2` — persistence

- [ ] A part-drained pack keeps its exact durability across a **logout / login**.
- [ ] It keeps it across a round-trip **into and out of a chest**.

---

## 8. `sp-br6.3` / `sp-br6.4` — no exceptions

With all six packs on you, open every build tab with the **hammer**, the **hoe**, and the
**cultivator**.

- [ ] Zero exceptions (red text) in the BepInEx console.

---

## 9. `sp-c2v.2` — packs are not interchangeable

- [ ] Wearing the **Stonecutter Pack** does **not** let you build workbench pieces.
- [ ] Each pack enables **only** its own station.

---

## 10. `sp-c2v.3` — presentation

- [ ] No raw `$sp_...` text anywhere in the UI.
- [ ] Each pack shows a sensible name, description and icon.

---

## Interop (new — these now load together)

- [ ] **AzuCraftyBoxes** pulls build materials from nearby chests *and* the pack supplies the
      station: a piece that is buildable actually places.
- [ ] **PlantEasily** still behaves normally.

---

## Closing issues

Run from `C:\Users\donrh\valheim-stationpacks`:

```powershell
bd close sp-br6.2 --reason "..."
bd close sp-br6.3 --reason "..."
bd close sp-br6.4 --reason "..."
bd close sp-br6.5 --reason "..."
bd close sp-br6.6 --reason "..."
bd close sp-81k.1 --reason "..."
bd close sp-81k.2 --reason "..."
bd close sp-81k.3 --reason "..."
bd ready                      # shows what unblocked
```

Or just tell Claude what happened and it'll record it.

## If something fails

Capture: the console line(s), the output of `stationpacks phantoms` / `invariant` / `stations`, and
what you were doing. The full log is at:

```
%APPDATA%\r2modmanPlus-local\Valheim\profiles\Default\BepInEx\LogOutput.log
```
