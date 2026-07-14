# Changelog

## 0.1.1

**Fixes a balance bug that made packs strictly better than they should be. Please update.**

- **Packs no longer inherit the perks of the cape they're built from.** The Forge Pack was granting
  frost resistance (it's a reskinned wolf cape under the hood) and the Black Forge Pack was granting
  fall protection. That defeated the whole point: a pack is supposed to *cost* you your cape slot,
  not hand you a cape's best perk for free. A pack now carries exactly three stats — weight, a
  movement penalty, and charge.
- **`stationpacks give` is disabled by default.** It hands you all six packs for free, skipping every
  recipe, and it should never have shipped enabled. Turn it on in the config if you want it for
  testing. The read-only diagnostics (`phantoms`, `invariant`, `stations`) still work.
- **Added an uninstall warning to the README.** Removing the mod permanently deletes your packs — see
  the top of the readme.

## 0.1.0

Initial release. Six wearable crafting stations: Workbench, Stonecutter, Forge, Artisan, Black Forge
and Galdr. Build and deconstruct anywhere; recharge at the station you left behind.
