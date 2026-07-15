# StationPacks

### Wear your workbench. Build anywhere.

Sling a workbench across your back and raise walls in the middle of nowhere — no bench to plant, no
run home for stone, no breaking flow mid-build. Six packs, one for every build station, each worn as
the real thing.

![Workbench Pack](https://raw.githubusercontent.com/dontoisme/valheim-stationpacks/main/docs/media/pack-workbench.png)

![Forge Pack](https://raw.githubusercontent.com/dontoisme/valheim-stationpacks/main/docs/media/pack-forge.png)

![Galdr Pack](https://raw.githubusercontent.com/dontoisme/valheim-stationpacks/main/docs/media/pack-galdr.png)

StationPacks doesn't *delete* the crafting-station rule — it lets you **carry** the station. That's
the whole point: mobility you earn and pay for, not a cheat toggle you flip.

## What you get

- **Build, deconstruct and repair anywhere** a station is needed — out in the field, mid-raid, up a
  mountain.
- **Six packs**, each worn as the station it replaces: Workbench, Stonecutter, Forge, Artisan,
  Black Forge, Galdr Table.
- **A real cost.** Packs are heavy, they slow you down, they cost you a slot, and they run down with
  every piece you place. Recharge at the very bench you were trying to avoid.
- **Not portable-everything.** You can *build* in the field, but you can't craft or upgrade gear.
  On purpose — this is a building mod, not a bag of holding.

## The packs

| Pack | Grants | Craft at | Recharge at |
|---|---|---|---|
| Workbench Pack | Workbench | Workbench | Workbench |
| Stonecutter Pack | Stonecutter | Forge | Stonecutter |
| Forge Pack | Forge | Forge (lvl 2) | Forge |
| Artisan Pack | Artisan Table | Artisan Table | Artisan Table |
| Black Forge Pack | Black Forge | Black Forge | Black Forge |
| Galdr Pack | Galdr Table | Galdr Table | Galdr Table |

Each pack is earned at roughly the tier of the station it stands in for, upgrades to quality 3, and
gains charge as it levels. Building next to a *real* station costs a pack nothing — you're only
charged when the pack is what made the build possible.

## Install

**Every client and the dedicated server must run this mod, on the same version.** The packs are
custom items that travel across the network, so a server that doesn't know them can't store them —
the mod refuses the connection rather than risk your world.

## Compatibility

- **AdventureBackpacks** — both want the vanilla cape slot, so out of the box you wear one or the
  other. Run a slot-extender (**ExtraSlots**) and move your backpack to its own slot, and the two
  live happily together.
- **AzuCraftyBoxes** — a great pairing: it pulls build materials from nearby chests, StationPacks
  supplies the station. Build a base out of your storage without a bench in sight.
- **Plays nice with real stations.** Your base forge keeps its extensions and its level — a pack is
  never registered as a real station, by design.

## Heads up

> ⚠ **Uninstalling deletes your packs.** Remove or disable the mod and any packs you're carrying
> vanish — inventory, equipment slot, chests, all of it, silently. That's how Valheim treats items
> whose mod is gone; every custom-item mod does this. **Dismantle your packs before uninstalling.**

## Config

Edit the StationPacks config in `BepInEx/config/` (created on first launch), or use an in-game config
manager — slot mode, charge costs, build range, weight, and pack appearance.

## Source & credits

Open source ([MIT](https://github.com/dontoisme/valheim-stationpacks)), built with
[Jotunn](https://github.com/Valheim-Modding/Jotunn).
