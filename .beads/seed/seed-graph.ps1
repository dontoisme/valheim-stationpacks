# Seeds the StationPacks work graph into Beads.
# Idempotency: this creates issues unconditionally. Run once on a fresh `bd init`.
# Re-running will duplicate. To start over: delete .beads/ and re-run `bd init --quiet --prefix sp`.

$ErrorActionPreference = 'Stop'
$env:PATH = "$env:PATH;C:\Users\donrh\AppData\Local\Programs\bd"
Set-Location $PSScriptRoot\..\..

function New-Epic($Title, $Priority, $Desc) {
    $id = bd create $Title -t epic -p $Priority --description $Desc --silent
    if (-not $id) { throw "failed to create epic: $Title" }
    Write-Host ("EPIC  {0,-10} {1}" -f $id, $Title)
    return $id.Trim()
}

function New-Task($Title, $Parent, $Priority, $Desc, $Accept, $Labels) {
    $id = bd create $Title -p $Priority --parent $Parent `
        --description $Desc --acceptance $Accept --labels $Labels --silent
    if (-not $id) { throw "failed to create task: $Title" }
    Write-Host ("  task {0,-12} {1}" -f $id, $Title)
    return $id.Trim()
}

# ---------------------------------------------------------------- E1
$E1 = New-Epic "Environment and scaffold" 0 @"
Get a plugin loading in-game and pin down the last unknowns in the assembly.
Blocks everything else. See docs/PLAN.md.
"@

$T11 = New-Task "Restore BepInEx pack" $E1 0 @"
A Steam file-verify wiped the BepInEx/ folder. winhttp.dll and doorstop_config.ini
are still present and already armed (target_assembly=BepInEx\core\BepInEx.Preloader.dll),
so restoring the folder alone re-arms Doorstop. Reinstall denikson-BepInExPack_Valheim
(r2modman is the sane route on a dev box). Enable the console in BepInEx.cfg.
"@ @"
Game launches; the BepInEx console window appears; BepInEx/LogOutput.log is written
and names the BepInEx version.
"@ "env"

$T12 = New-Task "Install Jotunn" $E1 0 @"
Install ValheimModding-Jotunn (2.29.x) into the dev profile.
"@ @"
Jotunn logs its version line on startup in the BepInEx console.
"@ "env"

$T13 = New-Task "Scaffold the csproj" $E1 0 @"
net48 + Microsoft.NETFramework.ReferenceAssemblies (no Framework dev pack on this box).
BepInEx.AssemblyPublicizer.MSBuild with Publicize=true on assembly_valheim.
BepInEx.Core 5.4.x (HarmonyX) + JotunnLib. Local refs (Private=false) to the game's
Managed/ DLLs -- prefer these over NuGet reference assemblies, since this Unity
6000.0.61 build may be ahead of them. Debug post-build copy into the plugins folder.
"@ @"
`dotnet build` succeeds from a clean clone and drops StationPacks.dll into
<Valheim>/BepInEx/plugins/StationPacks/.
"@ "env"

$T14 = New-Task "Hello-world plugin loads" $E1 0 @"
BaseUnityPlugin with GUID/name/version and a single log line in Awake.
"@ @"
The plugin's GUID and version appear in the BepInEx console on startup, with no errors.
"@ "env"

$T15 = New-Task "Assembly recon into docs/RECON.md" $E1 0 @"
Two open questions the design depends on:
(1) Does CraftingStation carry [RequireComponent(typeof(ZNetView))]? If so, AddComponent
    silently adds a ZNetView to the phantom and we must DestroyImmediate it.
(2) The six real station m_name tokens -- QUOTE them from the assembly, do not guess.
Use ilspycmd (dotnet tool install -g ilspycmd).
"@ @"
docs/RECON.md exists and answers both questions with values quoted from
assembly_valheim.dll, not from memory or web docs.
"@ "env,recon"

# ---------------------------------------------------------------- E2
$E2 = New-Epic "Phantom station core" 0 @"
The entire technical risk of the mod. A single Harmony postfix on the static
CraftingStation.HaveBuildStationInRange(string, Vector3) covers build placement,
deconstruct, hammer-repair and the build-menu UI -- IL call-graph analysis confirms
those are its only three callers. The phantom stations must NEVER enter m_allStations.
"@

$T21 = New-Task "The piece un-greys (proof of concept)" $E2 0 @"
One postfix, hardcoded, no item and no durability:
  if (__result == null && name == "`$piece_workbench") __result = phantom;
Phantom built per docs/PLAN.md section 1: inactive DontDestroyOnLoad holder,
AddComponent on the INACTIVE GameObject so Start()/OnEnable() never run.
This is the whole concept in one patch. If it works, the mod is real.
"@ @"
Standing in the wilderness far from any bench: a wood wall goes from greyed-out to
buildable in the hammer menu, places successfully, and deconstructs WITHOUT printing
`$msg_missingstation. Zero exceptions in the BepInEx console.
"@ "core,risk"

$T22 = New-Task "Phantom registry for all stations" $E2 0 @"
Scan ZNetScene.instance.m_prefabs for CraftingStation components and build one inactive
phantom per station, keyed by m_name. Scanning (rather than hardcoding names) means
modded stations get pack support for free. Copy fields per docs/PLAN.md: force
m_craftRequireRoof/m_craftRequireFire false, m_discoverRange 0, non-null empty
m_attachedExtensions and EffectLists, null m_areaMarker/m_areaMarkerCircle/m_nview.
"@ @"
A debug command logs exactly one phantom per vanilla crafting station, AND
CraftingStation.m_allStations.Count is identical before and after phantom construction
(assert this in code -- it is the invariant the whole design rests on).
"@ "core"

$T23 = New-Task "Phantom guard prefixes" $E2 0 @"
Callers dereference the returned station (Hud.SetupPieceInfo calls ShowAreaMarker() and
reads m_icon; HaveRequirements may call GetLevel). Rather than hope the copied fields
suffice, make the phantom unable to execute vanilla code. Prefixes keyed on an O(1)
HashSet identity check: GetLevel -> 1, CheckUsable -> true, GetStationBuildRange ->
config, ShowAreaMarker/HideMarker -> no-op, InUseDistance -> true.
"@ @"
With all six phantoms live, opening every build tab with the hammer, the hoe and the
cultivator produces zero exceptions in the console.
"@ "core"

$T24 = New-Task "Safety guards on the postfix" $E2 0 @"
The static takes no Player argument, so other mods (PlanBuild, InfinityHammer, base
scanners) can call it for arbitrary points and would otherwise get a free station.
Guards, in order: bail if __result != null (never override a real station, and stay
compatible with V+/InfinityHammer); bail if Player.m_localPlayer == null or
ZNet.instance.IsDedicated(); bail if the point is >64m from the local player.
Must stay O(1) -- this runs per-piece, per-frame with the build menu open.
"@ @"
An in-game test calling HaveBuildStationInRange for a point >64m from the player
returns null even with a pack equipped. A real station in range still wins.
"@ "core"

$T25 = New-Task "Regression: station extensions unaffected" $E2 0 @"
THE bug this design exists to prevent. The naive approach (parent a real CraftingStation
to the player) enters m_allStations, so StationExtension.FindClosestStationInRange can
re-target your BASE forge's coolers onto the portable one -- silently dropping the real
forge's GetLevel() (level = 1 + extension count) and gating recipes while you stand next
to it. Testable with the hardcoded grant, before any item exists.
"@ @"
Standing next to a real forge with a forge grant active: the base forge's GetLevel() is
unchanged and its coolers remain attached to it. m_allStations.Count never changes.
"@ "core,regression"

$T26 = New-Task "Regression: no portable item-crafting" $E2 0 @"
Scope guard. The crafting GUI reads Player.m_currentStation, which is set only by
CraftingStation.Interact. We never touch it, so portable crafting is excluded by
construction -- but prove it, and keep proving it.
"@ @"
With a pack grant active, the crafting/upgrade panel remains UNAVAILABLE. You cannot
craft or upgrade items in the field.
"@ "core,regression"

# ---------------------------------------------------------------- E3
$E3 = New-Epic "Pack items" 1 @"
Six wearable packs, built by cloning six different vanilla capes -- no Unity Editor on
this box, and VisEquipment.SetShoulderEquipped needs a real attach hierarchy that only a
cloned prefab has.
"@

$T31 = New-Task "Workbench Pack as a real item" $E3 1 @"
Jotunn CustomItem cloned from CapeDeerHide via PrefabManager.CreateClonedPrefab.
ItemType.Shoulder. CustomRecipe at the workbench. The grant now routes through
EquippedPackResolver.TryGetPack(player) instead of being hardcoded.
"@ @"
Unequip the pack -> the wood wall is greyed out. Equip it -> buildable. The pack renders
visibly on the character's back. It survives a world rejoin (ObjectDB.CopyOtherDB).
"@ "items"

$T32 = New-Task "Remaining five packs and progression gating" $E3 1 @"
Stonecutter, Forge, Artisan, Black Forge, Galdr. One distinct tier-appropriate cape each
(troll hide, wolf, lox, feather, a late-game cape) so they read as progression with zero
art. Per-tier m_weight (~8-20) and m_movementModifier (-0.03..-0.08). Recipes gated by
Recipe.m_craftingStation + m_minStationLevel and tiered materials.
"@ @"
Each pack enables exactly its own station and no other. Each is craftable only at its
gating station and level. Wearing a Stonecutter Pack does NOT let you build workbench
pieces.
"@ "items,balance"

$T33 = New-Task "Icons and English localization" $E3 2 @"
128x128 PNGs as EmbeddedResource, Texture2D.LoadImage + Sprite.Create. Fallback: reuse
the source cape's icon so art never blocks code.
"@ @"
No raw `$sp_ localization tokens are visible anywhere in the UI. Every pack has a
distinct icon in the inventory.
"@ "items,polish"

# ---------------------------------------------------------------- E4
$E4 = New-Epic "Durability and charges" 1 @"
The cost that keeps this from being a cheat mod. Uses vanilla ItemData.m_durability, so
the tooltip bar, the broken state, and save/chest persistence all come free.
"@

$T41 = New-Task "Charge drain and station repair" $E4 1 @"
Set m_useDurability=true, m_maxDurability (config), m_canBeReparied=true, and
m_durabilityDrain=0 so vanilla never touches it -- WE own the decrement.
Drain only when the pack actually satisfied the gate: the postfix records
LastGrant=(stationName, Time.frameCount); postfixes on Player.PlacePiece /
Player.RemovePiece / Player.Repair drain only on a same-frame matching grant.
Placement costs more than removal. Recharge: set each pack's CustomRecipe.m_repairStation
to its matching REAL station, so vanilla InventoryGui repair works with zero code.
At 0 durability Valheim does not destroy the item -- it just stops granting. Free.
"@ @"
Place ~100 walls in the field -> the pack breaks -> building is blocked -> repair it at a
real workbench -> building works again.
"@ "balance"

$T42 = New-Task "Durability persistence" $E4 1 @"
Vanilla m_durability is a first-class serialized ItemData field, so this should be free --
verify rather than assume.
"@ @"
A partially-drained pack retains its EXACT durability value across a logout/login and
across a round-trip into and out of a chest.
"@ "balance,regression"

$T43 = New-Task "Regression: no false drain near a real station" $E4 1 @"
If the drain is not gated on the grant flag, the pack bleeds durability while you stand
in your own base -- the single most likely balance bug.
"@ @"
Building inside a real workbench's radius with a pack equipped drains ZERO durability.
"@ "balance,regression"

# ---------------------------------------------------------------- E5
$E5 = New-Epic "Compatibility and config" 2 @"
The AdventureBackpacks slot conflict is the #1 expected user complaint (722K downloads,
same shoulder slot). Not a crash -- exclusivity. Solve it with a seam, not a hack.
"@

$T51 = New-Task "EquippedPackResolver slot seam and synced config" $E5 2 @"
Every "is a pack equipped?" question goes through EquippedPackResolver.TryGetPack(Player).
SlotMode = Auto | Shoulder | ExtraSlot | AnyInventory. Auto uses an ExtraSlots dedicated
slot when that mod is present (freeing the shoulder for AdventureBackpacks), else the
shoulder. ServerSync the BALANCE values (durability, costs, weights) so admins own the
numbers -- note this is NOT a security boundary; see docs/PLAN.md section 7.
"@ @"
Flipping SlotMode in the config takes effect with no code change. A server-side balance
value overrides the client's local setting.
"@ "compat,config"

$T52 = New-Task "AdventureBackpacks and ExtraSlots interop" $E5 2 @"
Detect AB via Chainloader.PluginInfos -> log at INFO + one-time in-game notice. No
hard-fail, no warn-spam. Call ExtraSlots / AzuExtendedPlayerInventory purely by
REFLECTION (soft-dep only) so our DLL carries no assembly reference and cannot fail to
load when they are absent.
"@ @"
With AdventureBackpacks installed: exactly one informational notice appears and neither
mod throws. With ExtraSlots installed and SlotMode=Auto: a station pack and an
AdventureBackpack are worn SIMULTANEOUSLY.
"@ "compat"

# ---------------------------------------------------------------- E6
$E6 = New-Epic "Multiplayer validation" 2 @"
Build placement is client-authoritative -- the dedicated server has no Player for remote
clients and never validates builds. So client-side patching is correct and sufficient.
NOTE: no dedicated server is installed on this box; needs a second install or machine.
"@

$T61 = New-Task "Two-client plus server test" $E6 2 @"
One modded client, one VANILLA client, one dedicated server.
"@ @"
Placements made with a phantom station persist after a server round-trip. The vanilla
client does not crash (it merely renders no cape). A dropped pack does not spam
ZNetScene instantiate errors on the modded client.
"@ "mp"

$T62 = New-Task "Choose the NetworkCompatibility mode" $E6 2 @"
Jotunn NetworkCompatibilityMode.EveryoneMustHaveMod is the honest default (custom
prefabs must exist on all peers), with a config toggle for mixed lobbies that accept
invisible capes.
"@ @"
The decision and its rationale are recorded in docs/PLAN.md.
"@ "mp,docs"

# ---------------------------------------------------------------- E7
$E7 = New-Epic "Release" 3 @"
Thunderstore packaging and publish.
"@

$T71 = New-Task "Thunderstore package" $E7 3 @"
manifest.json (deps: denikson-BepInExPack_Valheim, ValheimModding-Jotunn), 256x256 icon,
README, CHANGELOG, MIT license. Package with tcli.
"@ @"
`tcli build` produces a valid package archive.
"@ "release"

$T72 = New-Task "Publish and verify from a clean profile" $E7 3 @"
The only test that matters: does it work for a stranger?
"@ @"
The package installs cleanly from Thunderstore into a FRESH r2modman profile, and the
wilderness test from the proof-of-concept task passes on that profile.
"@ "release"

# ---------------------------------------------------------------- dependency wiring
Write-Host "`nWiring blockers..."
$deps = @(
    @($T14, $T11), @($T14, $T13),           # hello-world needs BepInEx + a build
    @($T21, $T14), @($T21, $T15),           # PoC needs a loading plugin + recon
    @($T22, $T21),
    @($T23, $T22), @($T24, $T22), @($T25, $T22), @($T26, $T22),
    @($T31, $T23), @($T31, $T12),           # CustomItem needs Jotunn
    @($T32, $T31), @($T33, $T32),
    @($T41, $T31), @($T42, $T41), @($T43, $T41),
    @($T51, $T31), @($T52, $T51),
    @($T61, $T41), @($T61, $T52), @($T62, $T61),
    @($T71, $T33), @($T71, $T43),
    @($T72, $T71), @($T72, $T62)
)
foreach ($d in $deps) {
    bd dep add $d[0] $d[1] --no-cycle-check | Out-Null
    Write-Host ("  {0} blocked-by {1}" -f $d[0], $d[1])
}
bd dep cycles
Write-Host "`nSeed complete."
