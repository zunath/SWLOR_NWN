# Game animations

This branch contains perk playback, the in-game animation tester, authored clips, and the
current animation production plan. The command-line generator uses the shared headless
animation library and works without the separate Avalonia animation editor.

The Design Bible Animations tab retains 119 references matching current active player perks.
Its 47 obsolete or passive entries have been removed. The production plan covers every
current active player perk: 7 installed clips, 206 remaining requirements, and the native
Stealth action. Beast abilities are excluded because their models have separate animation
sets; the six Beast Mastery abilities remain included. Shared motions can satisfy multiple
requirements. Existing image links are preserved; missing references are recorded in the plan.

## Animation tester


Use `/animations` to open the searchable **Animation Tester**. It is available to everyone
on the Test environment, and to DM/Admin accounts on other environments. Search by readable
name (`covering strike`), identifier (`CoveringStrike`), or installed name (`sw_coverings`).
Choose a skill category on the left to narrow the list, or **All animations** to search across
categories. Counts show installed clips; search works within the selected category. Changing
category resets pagination while keeping the search text. Categories come from the skills
declared on the abilities that play each clip, falling back to their associated perk category
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

```text
design/animations/
  README.md                    Workflow and library conventions
  registry.json                Installed names, targets, and canonical ProjectPath values
  ANIMATION-PLAN.md             Readable production backlog
  animation-plan.csv            Searchable production backlog
  projects/
    vibroblade/
      *.swlanim                One editable source per animation
      recipe.json              Repeatable pose-authoring controls
      manifest.json            Bible references and generated-source hashes
```

Add future skill groups beside `vibroblade`; use lowercase folder names with hyphens and
keep each animation in one owning group even when several perks use it. Shared motions can
live in `projects/shared/`. A categorized project named to match its animation is reused on
first installation. Projects saved elsewhere are copied into `projects/uncategorized/`.
Subsequent installations reuse the registry's `ProjectPath`. To reorganize an installed source,
move its file and update that path together. Legacy registries retain existing flat sources
until explicitly reorganized. Keep generated previews, GIFs, render data, and scratch drafts
in the ignored `artifacts/animations/` directory.


The seven current Vibroblade perk references on the Bible's **Animations** tab have editable sources in
`design/animations/projects/vibroblade/`. Keep these `.swlanim` files long term: they preserve
the rig and editable poses used to export the installed MDLs. The game reads MDLs from the HAKs.
The recipe records procedural pose controls; the manifest records Bible references and hashes.
Neither replaces hand-edited projects. There is one canonical project per animation.

The seven `.swlanim` files are editable authoring projects. When the separate animation
editor is installed, choose **Open project** to modify them visually. These drafts use the native male humanoid `a_ba` rig and remain editable. One-shot moves start and finish in
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
from the recipe, installed models, registry, and preview list. Recipe entries must identify an
existing `IAbilityListDefinition`; generation rejects stale entries instead of inventing perks.

To update an installed draft from the command line, close the toolset and run this against an
isolated checkout with the complete HAK source chain available:

```powershell
dotnet build tools/SWLOR.AnimationDrafts/SWLOR.AnimationDrafts.csproj -p:RunPostBuildEvent=Never
dotnet tools/SWLOR.AnimationDrafts/bin/Debug/net10.0/SWLOR.AnimationDrafts.dll install . design/animations/projects/vibroblade/ShieldBash.swlanim a_ba a_fa
```

The command uses the shared animation installation transaction, then the HAK and C# builds
must be deployed again. Review both the parent repository and HAK submodule changes.

`manifest.json` records each Bible row, image link, interpretation, key poses, and validation
hash. `design/animations/projects/vibroblade/recipe.json` preserves the authored pose controls so Codex
can make repeatable changes such as a stronger lunge or a faster cut. It does not call an
external AI service. Keep manual edits in **Save as** copies before regenerating the originals.

Regenerate with a local copy of the HAK source model (substitute your actual path):

```powershell
dotnet build tools/SWLOR.AnimationDrafts/SWLOR.AnimationDrafts.csproj -p:RunPostBuildEvent=Never
dotnet tools/SWLOR.AnimationDrafts/bin/Debug/net10.0/SWLOR.AnimationDrafts.dll generate C:/Projects/SWLOR_NWN/SWLOR_Haks/sw_cr_creature/a_ba.mdl design/animations/projects/vibroblade/recipe.json design/animations/projects/vibroblade --overwrite
```

The generator checks joint reach, foot clearance, a matching start/end pose, project loading,
and native MDL exchange before writing outputs. It does not write a browser preview into the
source library. Generate a preview from the saved projects without rebaking or changing them:

```powershell
dotnet tools/SWLOR.AnimationDrafts/bin/Debug/net10.0/SWLOR.AnimationDrafts.dll preview design/animations/projects/vibroblade artifacts/animations/vibroblade/preview.html --overwrite
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
dotnet tools/SWLOR.AnimationDrafts/bin/Debug/net10.0/SWLOR.AnimationDrafts.dll render-data SWLOR_Haks/sw_cr_creature/a_ba.mdl design/animations/projects/vibroblade artifacts/animation-poses.json --frames --shield SWLOR_Haks/sw_weapon/ashlw_113.mdl --sword SWLOR_Haks/sw_weapon/wswls_t_122.mdl
```

Add `--overlay SWLOR_Haks/sw_cr_creature/an_a_ba.mdl --registry design/animations/registry.json`
to sample the installed animation file against the target model's skeleton and animation scale.
For female validation use `a_fa.mdl` and `an_a_fa.mdl`. These are offline renders of game assets;
they do not verify the live client's playback, model cache, equipment choice, or transitions.

## Clothing animation bridges

RGB robe phenotypes also have generated animation bridges for their separate garment joints.
After changing animations inherited from a body rig, regenerate those bridges from the HAK
repository before packaging; otherwise a robe can continue using its older movement tracks:

```powershell
python -B tools/GenerateRobeRgbModels.py --game-data "<NWN installation>/data" --apply
python -B tools/PruneRobeAnimationBridges.py --apply
python -B tools/PruneRobeAnimationBridges.py
python -B tools/GenerateRobeRgbModels.py --check
```

Run these commands from `SWLOR_Haks`. Package and deploy `sw_pt_root.hak`, `sw_pt_robe.hak`,
and `sw_2da.hak` together with the HAK containing the changed animation overlays.
The generator audits body poses, weapon attachments, garment bindings, and source/output hashes.
The cleanup keeps every bridge referenced by a current or retired body model, including transitive
parents across all configured HAK layers. It removes only verified, unreachable generated files;
historical resref assignments remain reserved. Run it from a checkout containing all tracked models.
