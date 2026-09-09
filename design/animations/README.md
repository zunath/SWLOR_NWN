# Game animations

This branch contains perk playback, the in-game animation tester, authored clips, and the
current animation production plan, plus a standing fishing sequence. The command-line generator uses the shared headless
animation library and works without the separate Avalonia animation editor.

The active ability library covers 281 animation entries and 506 ability-rank bindings: 213 custom
perk requirements, 67 active Mimicry techniques, and Call Beast as its own action.
Stealth retains its native toggle and has no custom project, installed clip, or tester entry.
The perk plan keeps its explicit `Native` exclusion without an internal animation name.
Beast abilities are excluded because their models have separate animation sets; player
Beast Mastery actions remain included. The Animations tab retains all 119 image references
and documents every entry's internal name, base motion, source project, and review status.
The installed registry and tester also include Fishing6, Fishing7, and Fishing8, for 284
clips total. These activity clips are separate from the 281-entry ability inventory and Bible.
The seven original Vibroblade clips are preserved. The other custom ability motions have
273 individual choreography recipes; Provoke uses stock NWN Taunt, while Throw Lightsaber
uses the master branch's custom throw through its original animation slot in game and in the
tester. The Throw Lightsaber recipe remains an authoring reference. Recipes
combine observed native phases with directed poses and require in-game visual review.

`active-abilities.json` is the explicit authoring and feat-binding contract;
`animation-names.json` reserves the stable internal names. Names are at most 12 characters,
leaving room for `_in` and `_out` within NWN's 16-character limit. Animators should use the
Bible's **Internal Name** exactly. Display names and C# identifiers are separate from it.
`active-manifest.json` records actual motion sources, durations, and source hashes.
Related abilities intentionally share base movement families while retaining independent
names and editable projects. Skeletal animation does not create the effects shown in the
reference images or change ability mechanics.

Generate and install through the headless CLI:

```powershell
dotnet build tools/SWLOR.AnimationDrafts/SWLOR.AnimationDrafts.csproj -p:RunPostBuildEvent=Never
dotnet tools/SWLOR.AnimationDrafts/bin/Debug/net10.0/SWLOR.AnimationDrafts.dll generate-active SWLOR_Haks/sw_cr_creature/a_ba.mdl design/animations/active-abilities.json design/animations
# Installation accepts a subset; generation above must always use the complete inventory.
New-Item -ItemType Directory -Force artifacts/animations | Out-Null
$animationEntries = Get-Content design/animations/active-abilities.json -Raw | ConvertFrom-Json
ConvertTo-Json -InputObject @($animationEntries | Where-Object Id -eq 'IronWallStance') -Depth 30 | Set-Content artifacts/animations/install-batch.json
dotnet tools/SWLOR.AnimationDrafts/bin/Debug/net10.0/SWLOR.AnimationDrafts.dll install-active . artifacts/animations/install-batch.json a_ba a_fa
python -B SWLOR_Haks/tools/CompileModels.py --since HEAD --apply
python tools/UpdateAnimationBible.py design/animations/active-abilities.json
powershell -ExecutionPolicy Bypass -File tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible
```

New or explicitly replaced animations require an individual recipe in the category's
`choreographies.json`, or an explicit `SourceAnimation` in the input inventory. Generation
rejects missing motion direction instead of choosing a generic animation from ability-name
keywords. The separate legacy Vibroblade workflow remains available.

Generation preserves existing projects unless explicitly passed `--overwrite`; preserve
manual edits before using that switch. `--replace <Id>` regenerates only the selected ability
projects and preserves existing shared bases. Generation stages projects, provenance, and the
runtime catalog together and restores earlier outputs if publication fails. Duplicate feat
bindings are rejected before generation or installation. Authoring transactions have a 512 MiB
snapshot budget covering the existing library and the new output bytes. For large revisions,
run several `generate-active ... --replace Id1 Id2 ...` commands with small groups (for example,
16 animations), always using the complete inventory. Each command publishes its own complete
manifest and catalog atomically; the commands together are not one transaction. A budget
failure leaves that command's outputs unchanged. Reduce the replacement group and retry;
if the existing library itself reaches the budget, stop and use a separately designed bounded
publication workflow rather than increasing the guard or passing a partial inventory.

Bulk installation validates all projects first
and restores earlier changes if a later installation fails, preserving concurrent edits and
reporting any retained recovery backups. Install large revisions in small subsets, compiling
between batches so the next transaction reads compact native banks. A subset only updates its
clips; it preserves the rest of the registry. Rebalance banks if their editable sources still
exceed the per-transaction budget. Compile the changed HAK models and regenerate robe
bridges using the workflow below before deployment. The generator's foot-floor and release
checks do not replace testing with actual equipment in NWN.

Installation retains SHA-256 and length fingerprints for untouched model dependencies,
instead of accumulating the complete inherited bank binaries in the 128 MiB input budget.
The complete parsed model corpus has a separate 128 MiB input limit, checked before each
allocation. An edited bank is promoted to a verified full before snapshot, and its editable
companion remains a full transaction input. Apply verifies
the fingerprints and holds read leases through publication; a batch rechecks earlier model
dependencies between steps and at completion. Rollback restores only files published by the
batch, never a concurrently changed read-only source.

Generated gameplay playback is limited to player creatures. Scripted ranged attacks use
their authored impact gesture independently of damage resolution; ranged stances use a
single entry gesture. Queued ranged attacks, channels, space, stealth, and explicitly
preserved native choreography retain their native contracts. Where an authored preview
exists, it remains available in the tester; native-mode Stealth has no tester entry.
NPCs retain their native playback.

## Device and companion choreography

All 27 Devices clips and all seven Beast Mastery actions have individual
timed recipes in `devices/choreographies.json` and `beast-mastery/choreographies.json`.
Reward presents an offering, Soothe Pet gives a slow calming signal, Tame gives a cautious
invitation, and Revive Beast kneels to assist before rising. These are humanoid gestures;
they do not assume a particular beast model's size or animate the beast itself.
Grenades use throwing preparation, release, and follow-through. Projectors, beacons,
support devices, and companion commands have separate gestures. Emergency Bunker deploys
from a standing pose. Flamethrower follows the master branch's `CastOutAnimation`
playback: that enum's numeric value, 86, selects NWScript `CUSTOM64`. Its source clips
are `custom64start` and `custom64lp` in `a_ba_casts`, rather than the similarly named
`castout` motion. The 2.1-second sampled draft still requires comparison with native playback
in the live game.

Grenade abilities play the same named motion as the tester. Their C# ability logic owns
blast and visual effects; the animation does not provide a native projectile carrier.
Existing gameplay timing remains separate from the authored throw's visual beats.

Each recipe contains an ability `Id`, a motion `Description`, `Duration` in seconds,
and labeled `Beats`. Beat `Time` is in seconds and `SourceTime` is a normalized position
within `SourceAnimation`. An optional per-beat `SourceModel` selects another model's motion.
The recipe-level `InPlace` option removes horizontal root travel for a stationary preview,
while preserving vertical motion and joint rotations; native gameplay still owns movement.
Optional `LeftHand` and `RightHand` targets use absolute model-space
metres; `TorsoDegrees` supplies the torso adjustment. Forward spans using the same native
animation continue sampling its intermediate motion even when hand or torso controls are
present. This retains the underlying leg, hip, and recoil movement. When a directed span
resets to an earlier source phase, it blends the endpoint poses for recovery. Spans without
hand or torso controls retain native reverse playback for deliberate rising cuts and loop wraps.
Hand direction retains the native local wrist grip for equipped weapons. When a hand target
starts or ends, the arm rotations blend between the native pose and the solved target pose
so the constraint does not snap the arm into position.

Optional per-beat `RootOffset` supplies a body displacement in model-space metres
(X lateral, Y forward, Z up). Use it to load a stance, drive a release, absorb recoil, or
settle a guard. The legs bend to retain the source pose's foot positions and sole orientations;
native steps still follow their source trajectories. Unreachable displacements are reduced
to preserve foot contact. Limits are 15 cm per horizontal axis and 10 cm vertically; current
recipes generally use smaller shifts. Weight changes should follow the ability's effort,
not repeat an unrelated bounce. Entry and recovery use unmodified native idle and cannot
have offsets. Absolute Defense remains the unchanged visual benchmark for this pass.
The generator automatically reads each category's recipe file. Regenerate selected entries
with `generate-active ... --replace <Id>` and reinstall before compiling body and robe models.
The provenance manifest records recipe paths, per-ability recipe hashes, additional model
dependency hashes, and editable project hashes.

These revisions have offline generation checks, but no live NWN visual approval. Review them
in `/animations` with the intended weapon and shield after deploying both body and robe HAKs.

## First Aid and Ghost Protocol

The twelve First Aid abilities have individual recipes in `first-aid/choreographies.json`.
Stim motions distinguish forearm injection, inhalation, shoulder compression, defensive
bracing, and multiple doses. Kits use dressing, infusion, cleansing, or urgent stabilization
gestures. Kolto Mist has an underarm canister motion; Resuscitation lowers beside the patient,
performs two compressions, and rises. Ghost Protocol's recipe in
`espionage/choreographies.json` operates a wrist cloaking control and settles into a low
escape posture. These recipes use the same generation and provenance workflow described above.
The native Stealth toggle has no custom animation and remains separate from Ghost Protocol.
Review the new motions with equipped items in the live game; the skeletal clips do not
create medical props or alter healing, resource costs, or stealth mechanics. Current equipment
remains visible. Focus Stim and Resuscitation place the weapon hand forward and outward
to clear the head in sampled equipped poses while preserving the native wrist grip. This
is offline asset verification, not a claim of live NWN visual validation.

## Force choreography

All 25 Force entries have individual recipes in `force/choreographies.json`. Directed pushes,
precise lances, inward drains, mind gestures, target wards, and area releases use different
hand paths and timing. Force Lightning reuses the original `a_ba_casts` CUSTOM64 start and
loop at native speed; gameplay restores its three-second native discharge. The tester's
leap sequences use the original CUSTOM65 phases at their existing playback speed.
Native gameplay retains movement for Force Leap and Force Intercept. Throw Lightsaber
uses the master's `Animation.SaberThrow` alias for `ANIMATION_LOOPING_CUSTOM46` (68)
at 2x speed in both gameplay and the tester. This requires the master model's
`custom46start`, `custom46lp`, and `custom46end` in `a_ba_non_combat`; it is not a stock
NWN throw. Its `sw_thro_sabe` name and editable project remain
available as an authoring reference; the generated clip is not selected for playback.

Ordinary Force gestures play at their declared activation or impact stage without extending
cast timers. Creeping Terror's field creation uses activation playback; it does not replay
the gesture on each damage pulse. Skeletal poses do not create lightning, stones, shields,
or other visual effects. Test with actual equipment after deploying both body and robe assets.

## Animation tester

Provoke uses NWN's native `FireForgetTaunt` (108) for both ranks and for tester
playback. Native timing follows the character model. Its historical `sw_provoke`
name remains a searchable alias; the old generated sword clip is not selected.
The Bible synchronizer reads explicit native preview declarations from ability
definitions so later refreshes preserve this distinction.


Use `/animations` to open the searchable **Animation Tester**. It is available to everyone
on the Test environment, and to DM/Admin accounts on other environments. Search by readable
name (`covering strike`), identifier (`CoveringStrike`), or installed name (`sw_coverings`).
Choose a skill category on the left to narrow the list, or **All animations** to search across
categories. Counts show installed clips; search works within the selected category. Changing
category resets pagination while keeping the search text. Categories start with the authored
catalog and include the skills declared on abilities, falling back to their associated perk category
for buffs and casts without a declared skill. Shared clips appear in each applicable category.
Clips without a categorized ability binding remain available under **Other**.
The list reads the generated `AuthoredAnimation` catalog automatically and shows 20 matches
per page, so additional installed clips require no separate debug list.
While a DM possesses an NPC, the window opens on the DM's screen and previews on that NPC.

Equip the weapon and shield you want to inspect, stand outside combat, and click **Play**.
The selected clip runs once on your character without damage, costs, or perk effects. Click
**Play** again to repeat. **Stop** and closing the window release only the preview started by
that window, preserving any newer ability animation. `/animtest <name>` remains available
with the same staff/Test permissions. Updated model assets require a client and server restart.

The current animation backlog is in [the animation production plan](ANIMATION-PLAN.md),
with a searchable [CSV](animation-plan.csv). It groups current active perks,
preserves matching Bible image references, and identifies installed clips and shared-motion candidates.

## Authored clips and generation

### Fishing activity

`fishing/` contains the same cast, wait, reel, and idle sequence at six, seven, and eight
seconds. Only the holding interval varies, preserving the activity's random wait time.
Fishing selects the matching `AuthoredAnimation.Fishing6`, `Fishing7`, or `Fishing8` clip.
Movement, combat, death, rod removal, and point exhaustion release only the animation
owned by that attempt. Completion callbacks capture the attempt ID so an old timer cannot
finish a restarted cast. Fish rewards, bait consumption, and catch odds are unchanged.

The editable sources are installed on both humanoid supermodel chains. `/animations`
lists them under **Other**; equip a fishing rod to check the actual attachment in game.
The recipe uses `activity: "Fishing"` and an empty `abilityDefinition`. Activity recipes
must name a real non-invalid `ActivityStatusType`; perk recipes still require a current
ability definition. Generate and install with the same commands below, substituting the
`fishing` folder and each `Fishing6`, `Fishing7`, and `Fishing8` project.

The HTML preview draws a 1.72m rod proxy from the native right-hand attachment, using the
bamboo rod's measured forward extent. It shows the saved skeletal poses, not live client
playback. After installing, compile the banks and regenerate the clothing bridges as below.

### Library layout

```text
design/animations/
  README.md                    Workflow and library conventions
  registry.json                Installed names, targets, and canonical ProjectPath values
  ANIMATION-PLAN.md             Readable production backlog
  animation-plan.csv            Searchable production backlog
  active-abilities.json         Active actions and exact feat bindings
  animation-names.json          Stable internal names reserved for animators
  active-manifest.json          Generated motion provenance and source hashes
  <skill-category>/            Editable sources for each skill group
  vibroblade/
    *.swlanim                  One editable source per animation
    recipe.json                Repeatable pose-authoring controls
    manifest.json              Bible references and generated-source hashes
```

Add future skill groups beside `vibroblade`; use lowercase folder names with hyphens and
keep each animation in one owning group even when several perks use it. Shared motions can
live in `shared/`. A categorized project named to match its animation is reused on
first installation. Projects saved elsewhere are copied into `uncategorized/`.
Subsequent installations reuse the registry's `ProjectPath`. To reorganize an installed source,
move its file and update that path together. Legacy registries retain existing flat sources
until explicitly reorganized. Keep generated previews, GIFs, render data, and scratch drafts
in the ignored `artifacts/animations/` directory.


The seven current Vibroblade perk references on the Bible's **Animations** tab have editable sources in
`design/animations/vibroblade/`. Keep these `.swlanim` files long term: they preserve
the rig and editable poses used to export the installed MDLs. The game reads MDLs from the HAKs.
The recipe records procedural pose controls; the manifest records Bible references and hashes.
Neither replaces hand-edited projects. There is one canonical project per animation.

The seven `.swlanim` files are editable authoring projects. These drafts use the native
male humanoid `a_ba` rig and remain editable through the CLI workflow. One-shot moves start and finish in
the native standing pose. Shield Wall keeps a guard loop for its channel, with a separate exit
that returns to the target model's neutral pose.
All seven are installed into the `a_ba` and `a_fa` humanoid supermodel chains in `sw_cr_creature`.
The registry points to these same editable sources; installation does not keep a second copy. The images establish the main
pose; wind-up, recovery, and durations are authored interpretations for review. The shield
barriers, hit effects, blood, targets, and gameplay outcomes pictured in the references are
not part of the skeletal clips. Covering Strike uses a planted guarded thrust, Riot Blade a
compact descending cut, Rending Strike a rising diagonal, and Savage Cleave a broad waist-height
sweep with a low recovery. These motions return to their starting position; actual character
travel requires separate gameplay support.

After rebuilding C# and deploying the updated creature and clothing HAKs to the server and client, use the existing
Shield Bash, Shield Wall, Covering Strike, Invincible, Riot Blade, Rending Strike, and Savage Cleave
perks. Every rank references its generated `AuthoredAnimation` clip. Shield Bash and Riot Blade
replace native melee swings while readied and restore them after consumption/cancellation;
they do not enqueue another animation at impact. Other clips play once at activation, or loop
for a longer cast/channel. Damage, costs, cooldowns, and movement rules are unchanged.

`/animtest ShieldBash` previews a clip on your character; `/animtest` lists all names. This debug
command is available to DM/Admin accounts and everyone on a test server. Hacking Blade and Carve
are outdated spreadsheet entries with no current matching abilities, so they are excluded
from the recipe, installed models, registry, and preview list. Perk recipe entries must identify an
existing `IAbilityListDefinition`; generation rejects stale entries instead of inventing perks.

To update an installed draft from the command line, run this against an
isolated checkout with the complete HAK source chain available:

```powershell
dotnet build tools/SWLOR.AnimationDrafts/SWLOR.AnimationDrafts.csproj -p:RunPostBuildEvent=Never
dotnet tools/SWLOR.AnimationDrafts/bin/Debug/net10.0/SWLOR.AnimationDrafts.dll install . design/animations/vibroblade/ShieldBash.swlanim a_ba a_fa
```

The command uses the shared animation installation transaction, then the HAK and C# builds
must be deployed again. Review both the parent repository and HAK submodule changes.
If backup cleanup fails after installation, the command lists the retained `.bak` files.
Review those files before removing them manually; failed installs also report retained backups
with their recovery details.

`manifest.json` records each Bible row, image link, interpretation, key poses, and validation
hash. `design/animations/vibroblade/recipe.json` preserves the authored pose controls so Codex
can make repeatable changes such as a stronger lunge or a faster cut. It does not call an
external AI service. Back up manual edits before regenerating the originals.

Regenerate with a local copy of the HAK source model (substitute your actual path):

```powershell
dotnet build tools/SWLOR.AnimationDrafts/SWLOR.AnimationDrafts.csproj -p:RunPostBuildEvent=Never
dotnet tools/SWLOR.AnimationDrafts/bin/Debug/net10.0/SWLOR.AnimationDrafts.dll generate C:/Projects/SWLOR_NWN/SWLOR_Haks/sw_cr_creature/a_ba.mdl design/animations/vibroblade/recipe.json design/animations/vibroblade --overwrite
```

The generator checks joint reach, foot clearance, a matching start/end pose, project loading,
and native MDL exchange before writing outputs. It does not write a browser preview into the
source library. Generate a preview from the saved projects without rebaking or changing them:

```powershell
dotnet tools/SWLOR.AnimationDrafts/bin/Debug/net10.0/SWLOR.AnimationDrafts.dll preview design/animations/vibroblade artifacts/animations/vibroblade/preview.html --overwrite
```

Open that HTML file in a browser to select a motion, slow playback, orbit the mannequin,
and jump to its main poses. Its accessory proxies use NWN's attachment points; verify final
equipment and motion in NWN. `AnimationDraftAssetTests` also checks planted
feet during interpolation and correspondence with the Bible references. Recipe vectors use
native metres (`+Y` forward, `+Z` up); `chest`, `hips`, and `shield` use degrees in the order
forward lean, yaw, side bend. Each beat overrides the corresponding ready pose, and the
generator solves limb positions at 20 frames per second with smooth timing between beats.
Mark contact and passing beats with `"through": true` to carry velocity through them. The
generator uses shape-preserving cubic interpolation, with zero velocity at unmarked beats
and direction changes; targets never overshoot. The four sword attacks use this to continue
through contact while separating wind-up, strike, and recovery.
Native shield meshes face `-X` with their top along `+Y`. NWN attaches shields to the
`lforearm` dummy beneath `lforearm_g`; weapons attach to `lhand` / `rhand`, which have
offsets from the `lhand_g` / `rhand_g` body pivots. See the
[native attachment node reference](https://nwn.wiki/spaces/NWN1/pages/38176272/Model+Special+Nodes).
Rotating a hand to aim a shield makes the preview misleading and twists the wrist in game.
The generator instead braces the forearm across the guard and rolls it about the elbow-to-wrist
axis, retaining the native socket offsets and a neutral left wrist. This applies to all seven
moves, since a shield may remain equipped during a sword attack. To inspect
actual equipment rather than the HTML mannequin's stand-ins, export posed triangles with:

```powershell
dotnet tools/SWLOR.AnimationDrafts/bin/Debug/net10.0/SWLOR.AnimationDrafts.dll render-data SWLOR_Haks/sw_cr_creature/a_ba.mdl design/animations/vibroblade artifacts/animations/animation-poses.json --frames --shield SWLOR_Haks/sw_weapon/ashlw_113.mdl --sword SWLOR_Haks/sw_weapon/wswls_t_122.mdl
```

Add `--overlay SWLOR_Haks/sw_cr_creature/an_a_ba.mdl --registry design/animations/registry.json`
to sample installed clips against the target model's skeleton and animation scale. The renderer
follows the overlay's supermodel chain, so a manifest may span several rebalanced banks; each
clip's `PoseSource` identifies the bank that supplied it. Overlays in configured HAK source
directories resolve parents through those layers. Standalone exports resolve sibling MDLs,
including exports inside the repository's `artifacts/` directory. The aggregate bank input
limit is checked before each parent is allocated or read.
For female validation use `a_fa.mdl` and `an_a_fa.mdl`. These are offline renders of game assets;
they do not verify the live client's playback, model cache, equipment choice, or transitions.

## Clothing animation bridges

All MDLs published in the HAK folders must be compiled. After installing or updating banks,
run the native compiler from `SWLOR_Haks` before regenerating clothing:

```powershell
python -B tools/CompileModels.py --since origin/feature/combat-upgrade --apply
```

Use the actual changed bank resrefs when the library grows. Compilation audits every output
and retains editable bank text in `model_sources/<hak-folder>/<bank>.mdl.ascii`, outside the
packaged HAK folders. Commit these companions with the compiled MDLs. They retain all existing
animation blocks and hashes of both source text and compiled bytes so subsequent editor/CLI
installs can update compiled banks without discarding other clips. Missing or mismatched pairs
are rejected. The `.swlanim` files remain the canonical pose projects; these bank sources are
the reproducible inputs for the compiled model resources. Recompile after each installation.

### Rebalance growing banks before installing updated clips

Banks are limited to 128 clips and 24 MiB of editable MDL text. Older, larger banks can be
split without resampling any animation. Run one rig at a time from the game repository;
each command verifies source hashes and commits its chain atomically under the existing
128 MiB transaction budgets. Registrations and internal animation names stay unchanged.

```powershell
dotnet run --no-build --project tools/SWLOR.AnimationDrafts -- rebalance-banks . a_ba
```

Compile the changed and newly created male banks from `SWLOR_Haks` using `CompileModels.py`
as above, then run the female transaction and compile its outputs:

```powershell
dotnet run --no-build --project tools/SWLOR.AnimationDrafts -- rebalance-banks . a_fa
```

Compile between rigs because the female chain inherits male banks. Only after compilation,
install the updated clips and compile again. Rebalancing
preserves complete main/start/end animation groups and the original native supermodel chain.
If one animation group alone exceeds the bank limit, the command rejects it without writing.

If a whole-rig rebalance exceeds its input budget, select one existing owned bank with
`--bank an_a_fa` (substitute the exact bank resref) and compile before selecting the next.
The transaction still validates and protects the complete compiled chain and registration;
only the selected bank's editable companion is loaded. This lets large libraries split
banks incrementally without raising memory limits or dropping concurrent-edit checks.

For an update that would outgrow an already packed bank, add `--clips-per-bank 64` to make
more room before reinstalling. The default remains 128. A limit of `1` is a diagnostic
option for small libraries; it still obeys the 32-model chain limit and will usually be
rejected for large libraries. Neither option changes any motion keys or raises byte limits.

Keep bank animation blocks sorted by name; the installer now sorts the complete bank before
compilation. Offline pose sampling alone does not validate native animation lookup or equipment
overlays. Fishing rods use the standard wield setting in `baseitems.2da`: the polearm setting
adds a shoulder-rest overlay that masks the right arm during the fishing clip. Validate fishing
with the rod equipped after replacing the HAKs, restarting the server, and reopening the client.

RGB robe phenotypes also have generated animation bridges for their separate garment joints.
Publish generated robe updates in a separate HAK PR from the humanoid animation banks.
Stack the robe PR on the body-animation branch, and keep the parent game's submodule pointer
on the complete body-and-robe result. Merge both HAK changes before deploying either set.
After changing animations inherited from a body rig, regenerate those bridges from the HAK
repository before packaging; otherwise a robe can continue using its older movement tracks:

```powershell
python -B tools/GenerateRobeRgbModels.py --game-data "<NWN installation>/data" --apply
python -B tools/PruneRobeAnimationBridges.py --apply
git add -u -- sw_pt_root
python -B tools/PruneRobeAnimationBridges.py
python -B tools/GenerateRobeRgbModels.py --check
```

Run these commands from `SWLOR_Haks`. Package and deploy `sw_pt_root.hak`, `sw_pt_robe.hak`,
and `sw_2da.hak` together with the HAK containing the changed animation overlays.
The generator audits body poses, weapon attachments, garment bindings, and source/output hashes.
The cleanup keeps every bridge referenced by a current or retired body model, including transitive
parents across all configured HAK layers. It removes only verified, unreachable generated files;
historical resref assignments remain reserved. Run it from a checkout containing all tracked models.
