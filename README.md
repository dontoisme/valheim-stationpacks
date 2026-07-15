# StationPacks

A Valheim mod: **wearable crafting stations**. Craft a Workbench Pack, wear it, and build
anywhere — at the cost of a slot, some carry weight, and a charge you refill at the bench you left
behind. Six packs, one per build station (Workbench, Stonecutter, Forge, Artisan, Black Forge,
Galdr), each of which shows the station it emulates on your back.

Built with [BepInEx](https://github.com/BepInEx/BepInEx) and
[Jotunn](https://github.com/Valheim-Modding/Jotunn).

## For players

Install from Thunderstore (search **StationPacks**) with a mod manager like r2modman or Thunderstore
Mod Manager. Full player-facing details — pack list, costs, compatibility, config — are in
[package/README.md](package/README.md).

> **Note:** removing the mod permanently deletes any packs you're carrying, as with every custom-item
> mod. Dismantle them first.

## For developers

- **Source:** [`src/StationPacks/`](src/StationPacks) — a BepInEx plugin targeting `net48`.
- **Build:** `dotnet build src/StationPacks/StationPacks.csproj`. Paths to the game and BepInEx are
  in [`Directory.Build.props`](Directory.Build.props); override `ValheimDir` if your install differs.
- **How it works:** the whole build-gating feature is a single Harmony postfix on
  `CraftingStation.HaveBuildStationInRange`. See [`docs/PLAN.md`](docs/PLAN.md) for the design and
  [`docs/RECON.md`](docs/RECON.md) for the assembly findings it's built on.
- **Packaging:** [`package/`](package) holds the Thunderstore `manifest.json`, icon, and readme.

Issues are tracked with [Beads](https://github.com/steveyegge/beads) in [`.beads/`](.beads).

## License

[MIT](LICENSE).
