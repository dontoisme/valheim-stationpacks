# StationPacks 0.1.0 — test build (not published)

Wearable crafting stations. Wear a Workbench Pack and you can **build and deconstruct** in the field
with no bench nearby — at the cost of your cape slot, some carry weight, some movement speed, and a
charge that only refills at the real station it replaces.

It does **not** let you craft or upgrade items in the field. That's deliberate.

> This is an unreleased test build. Don't redistribute it.

## Install it in THREE places

All three must have it, and all three must run the **same version** (the mod enforces this and will
refuse the connection otherwise — that's by design, not a bug).

| Where | What to install |
|---|---|
| Your client | Jotunn + StationPacks |
| Every other player's client | Jotunn + StationPacks |
| **The dedicated server** | Jotunn + StationPacks |

**The server is the one people forget.** Two modded clients still cannot play together if the server
doesn't have it — Valheim's server has to know the six custom item prefabs in order to store and
replicate them.

## Clients (r2modman)

1. In r2modman, Online tab → search **Jotunn** (`ValheimModding-Jotunn`) → Install.
2. Create the folder `BepInEx\plugins\StationPacks\` inside your r2modman profile and drop
   `StationPacks.dll` into it.
   - Profile path is roughly:
     `%APPDATA%\r2modmanPlus-local\Valheim\profiles\<ProfileName>\BepInEx\plugins\`
3. Launch from r2modman.

You should see this in the BepInEx console:

```
[Info :StationPacks] StationPacks 0.1.0 loaded.
[Info :StationPacks] Registered Workbench Pack (clone of CapeDeerHide).
...
[Info :StationPacks] Built 9 phantom stations; m_allStations unchanged at 0.
```

## Dedicated server

Drop `Jotunn.dll` and `StationPacks.dll` into the server's `BepInEx\plugins\`. Restart it.

## Try it

Press **F5** for the console:

```
stationpacks give     -- hands you all six packs (test build only)
stationpacks phantoms -- every pack should say "ok"
stationpacks invariant-- must say "OK: no phantom is registered"
```

Equip a **Workbench Pack** in the cape slot, walk somewhere with no bench in range, and open the
hammer. The wood wall should be **buildable instead of greyed out**.

> A missing station **greys the piece out in the build menu** — it does *not* turn the placement
> ghost red. Watch the menu, not the ghost.

## The six packs

| Pack | Grants | Craft at | Recharge at |
|---|---|---|---|
| Workbench Pack | Workbench | Workbench | Workbench |
| Stonecutter Pack | Stonecutter | Forge | Stonecutter |
| Forge Pack | Forge | Forge (lvl 2) | Forge |
| Artisan Pack | Artisan Table | Artisan Table | Artisan Table |
| Black Forge Pack | Black Forge | Black Forge | Black Forge |
| Galdr Pack | Galdr Table | Galdr Table | Galdr Table |

Each pack charges 1 point per piece placed (100 to start). At zero it stays equipped but stops
working until you repair it — at the very station it lets you leave behind.

**Building near a real station costs you nothing.** The pack is only charged when it's the thing that
made the build possible.

## Known / untested

- Multiplayer is **untested**. That's what this build is for.
- What an *unmodded* player sees when you walk past wearing a pack: unknown (probably no cape).
- Balance numbers are first-draft guesses. All tunable in the config.

Config: `BepInEx\config\donrh.stationpacks.cfg` (created on first run).
