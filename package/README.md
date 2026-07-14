# StationPacks

**Wearable crafting stations.** Craft a Workbench Pack, wear it, and build anywhere.

Building in Valheim is gated on standing near a crafting station. Every time a build creeps past a
workbench's radius you stop, gather materials, plant another bench, and tear it down later. The
stonecutter is worse. The friction lands exactly when you're most engaged — mid-build.

StationPacks is **not** a config toggle that deletes the restriction. It's a thing you craft, wear,
and pay for.

## What a pack does

While worn, a pack lets you **place, deconstruct and repair** pieces that need its station — with no
station in sight.

That's all it does. **You cannot craft or upgrade items in the field.** Deliberately: this is a
building mod, not a portable everything.

## What it costs you

- **Your cape slot.** Packs are shoulder items. You wear a pack or you wear a cape.
- **Weight and speed.** 8–20 weight, and −3% to −8% movement.
- **Charge.** Every piece you place spends a point. When the pack runs dry it stays equipped but
  stops working — until you repair it at **the very station it replaces.** Go home to recharge.
- **Progression.** Each pack is gated behind the tier of the station it stands in for.

**Building near a real station is free.** A pack is only charged when it's the thing that actually
made the build possible — so you never bleed charge inside your own base.

## The packs

| Pack | Grants | Craft at | Recharge at |
|---|---|---|---|
| Workbench Pack | Workbench | Workbench | Workbench |
| Stonecutter Pack | Stonecutter | Forge | Stonecutter |
| Forge Pack | Forge | Forge (lvl 2) | Forge |
| Artisan Pack | Artisan Table | Artisan Table | Artisan Table |
| Black Forge Pack | Black Forge | Black Forge | Black Forge |
| Galdr Pack | Galdr Table | Galdr Table | Galdr Table |

Packs upgrade to quality 3; each level adds charge.

## Install

**Every client and the server must have this mod, at the same version.** Six custom items cross the
network, so a server that doesn't know them can't store them. The mod will refuse the connection
rather than corrupt anything.

## Compatibility

- **AdventureBackpacks** — both use the vanilla shoulder slot, so you wear one or the other. Not a
  crash, just exclusivity. Install a slot-extender (**ExtraSlots**) and leave `Slot mode` on `Auto`
  and you can wear both.
- **AzuCraftyBoxes** — composes nicely. It supplies the *materials* from nearby chests; StationPacks
  supplies the *station*.
- Does not interfere with real crafting stations. Your base forge keeps its extensions and its
  level — a pack is never registered as a real station, by design.

## Config

`BepInEx/config/donrh.stationpacks.cfg` — slot mode, charge costs, max charge, build range, weight.

## Credits

Built with [Jotunn](https://github.com/Valheim-Modding/Jotunn).
