# Animation Editor

Open **Tools → Animation Editor**. This editor uses C#, Avalonia, `System.Numerics`, the native
SWLOR MDL reader, and the existing OpenGL model preview. Godot, GDScript, and a separate game
engine are not required.

## Start here

The editor opens in a guided layout with **Start**, **Pose**, and **Use in game** tabs.

1. Choose **Male humanoid**, **Female humanoid**, or **Other character file**.
2. Choose either **Use an existing movement** or **Build from poses**. Both are on the start screen.
   Starter movements include standing, walking, and an attack when the character supplies them.
   They are editable pose copies sampled from the same inherited movements used by the toolset preview;
   these short starters do not copy animation events. Advanced import remains available for other clips.
3. In **Pose**, choose a readable body part such as **Left upper arm** or **Head**. Bend, turn, and tilt
   buttons make five-degree adjustments. Each edit saves a pose at the current moment; Undo and Redo
   work throughout. Use **Start**, **Middle**, and **Finish**, or click the timeline to choose a moment.
4. Press **Play animation**. Add or remove poses as needed. Increasing the duration slows the movement.
5. Save the project. In **Use in game**, choose the current character and review the installation.
   The installation still needs rebuilt HAKs deployed to players and the server before in-game use.

Enable **Advanced** whenever you need raw joints, exact transforms, IK, retargeting, MDL exchange,
or other target models. Switching layouts preserves the animation and its undo history.

## Bible animation drafts

The seven current perks among the first nine image references on the Bible's **Animations** tab have editable drafts in
`design/animations/drafts/vibroblade/`. Open `preview.html` in your normal browser to choose a
motion, play it slowly, orbit the mannequin, and jump to its main poses. Its accessory proxies
use NWN's equipment attachment points; check the final equipment and motion in NWN.

In the toolset, choose **Open project** and open any of the seven `.swlanim` files. These drafts
use the native male humanoid `a_ba` rig and remain editable. One-shot moves start and finish in
the native standing pose. Shield Wall keeps a guard loop for its channel, with a separate exit
that returns to the target model's neutral pose.
All seven are installed into the `a_ba` and `a_fa` humanoid supermodel chains in `sw_cr_creature`.
Their installed editable copies are in `design/animations/`. The images establish the main
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
dotnet tools/SWLOR.AnimationDrafts/bin/Debug/net10.0/SWLOR.AnimationDrafts.dll install . design/animations/drafts/vibroblade/ShieldBash.swlanim a_ba a_fa
```

The command uses the editor's validated installation transaction, then the HAK and C# builds
must be deployed again. Review both the parent repository and HAK submodule changes.

`manifest.json` records each Bible row, image link, interpretation, key poses, and validation
hash. `design/animations/recipes/vibroblade.json` preserves the authored pose controls so Codex
can make repeatable changes such as a stronger lunge or a faster cut. It does not call an
external AI service. Keep manual edits in **Save as** copies before regenerating the originals.

Regenerate with a local copy of the HAK source model (substitute your actual path):

```powershell
dotnet build tools/SWLOR.AnimationDrafts/SWLOR.AnimationDrafts.csproj -p:RunPostBuildEvent=Never
dotnet tools/SWLOR.AnimationDrafts/bin/Debug/net10.0/SWLOR.AnimationDrafts.dll generate C:/Projects/SWLOR_NWN/SWLOR_Haks/sw_cr_creature/a_ba.mdl design/animations/recipes/vibroblade.json design/animations/drafts/vibroblade --overwrite
```

The generator checks joint reach, foot clearance, a matching start/end pose, project loading,
and native MDL exchange before writing outputs. `AnimationDraftAssetTests` also checks planted
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
dotnet tools/SWLOR.AnimationDrafts/bin/Debug/net10.0/SWLOR.AnimationDrafts.dll render-data SWLOR_Haks/sw_cr_creature/a_ba.mdl design/animations/drafts/vibroblade artifacts/animation-poses.json --frames --shield SWLOR_Haks/sw_weapon/ashlw_113.mdl --sword SWLOR_Haks/sw_weapon/wswls_t_122.mdl
```

Add `--overlay SWLOR_Haks/sw_cr_creature/an_a_ba.mdl --registry design/animations/registry.json`
to sample the installed animation file against the target model's skeleton and animation scale.
For female validation use `a_fa.mdl` and `an_a_fa.mdl`. These are offline renders of game assets;
they do not verify the live client's playback, model cache, equipment choice, or transitions.

## Advanced authoring

### In-game animation tester

Use `/animations` to open the searchable **Animation Tester**. It is available to everyone
on the Test environment, and to DM/Admin accounts on other environments. Search by readable
name (`covering strike`), identifier (`CoveringStrike`), or installed name (`sw_coverings`).
The list reads the generated `AuthoredAnimation` catalog automatically and shows 20 matches
per page, so additional installed clips require no separate debug list.
While a DM possesses an NPC, the window opens on the DM's screen and previews on that NPC.

Equip the weapon and shield you want to inspect, stand outside combat, and click **Play**.
The selected clip runs once on your character without damage, costs, or perk effects. Click
**Play** again to repeat. **Stop** and closing the window release only the preview started by
that window, preserving any newer ability animation. `/animtest <name>` remains available
with the same staff/Test permissions. Updated model assets require a client and server restart.

### Detailed pose tools

1. Enter a mounted NWN model resref (for example `a_ba`) and choose **Load rig**, or choose
   **Load rig file** for an ASCII or compiled `.mdl`. The model supplies the real joint names,
   hierarchy, and rest transforms. Loading a new rig prompts before replacing unsaved work.
   Rigs loaded from packed HAKs resolve back to their loose repository source for installation;
   if no source exists, choose a target source explicitly.
2. Select a joint in the list or viewport. Right drag orbits, the wheel zooms, and **Frame rig**
   fits the skeleton. Left drag rotates the selected axis, moves a joint, or solves a two-bone
   IK chain according to the selected mode. The colored rings select a local rotation axis.
   The Pose panel also exposes position, Euler rotation in degrees, scale, and the IK pole.
   Moving the body can keep hands and feet anchored, subject to the limbs' physical reach.
3. Drag the timeline to another time and pose again. Edits automatically create/update a key
   at the playhead. **Set key**, **Remove key**, **Copy pose**, **Paste pose**, and **Reset pose**
   work at that time. Gold markers snap to exact key times. Changing duration rescales keys
   and events. Playback loops with linear position/scale interpolation and quaternion slerp.
4. The **Model preview** tab renders the current pose using SWLOR's shared model renderer.
   Reopening a project resolves its model resref through the mounted resources; **Attach
   matching preview model** can restore a local preview without replacing the animation.
   Compatible local previews retain their source folder for inherited starter movements.
   Skeleton-only models use the rig view in the guided layout. Playback uploads a bounded set
   of sampled frames once; scrubbing while paused evaluates the exact authored pose.
5. Save an editable `.swlanim` project or export a `newanim`/`doneanim` MDL text block. Import
   accepts one ASCII animation block against the loaded rig, including constant transforms,
   counted or `endlist`-terminated transform keys, and animation events. Multiple animation blocks
   and unsupported controllers fail explicitly rather than
   disappearing during a round trip. Compiled MDLs can supply rigs; animation-block import is ASCII.

Save, Save All, Ctrl+Z, Ctrl+Y, external-change checks, and unsaved-close prompts use the normal
document workflow. Playback stops when the view detaches. History is bounded to 100 recent
entries and 32 MiB of serialized snapshots. Projects are bounded to 64 MiB; very dense bakes may
need a lower frame rate. Long timelines draw markers without creating one UI control per key.
Saves preserve a file created by another writer during the final commit. If restoring the captured
version would replace that newer file, the status message gives the retained `.bak` recovery path.
An interrupted save can also leave its captured `.bak` beside the project; keep it until recovery
is complete. glTF buffers share a 128 MiB aggregate limit checked before each buffer allocation.
Mounted rig and starter-model reads use the same 64 MiB limit as loose MDL files. Weighted-model
playback uses a 64 MiB frame budget covering source vertices and expanded face corners; models
that cannot fit two frames can still be posed and scrubbed individually.

## Retargeting

Load a `.glb` or `.gltf` in the Retarget panel and select its clip. The native importer reads
skeletal translation, rotation, and scale channels with LINEAR, STEP, and CUBICSPLINE interpolation.
glTF's Y-up coordinates are converted to NWN's Z-up coordinates once, after hierarchy evaluation.
Buffers may be embedded or local to the source folder. Required extensions, compressed/sparse
animation accessors, singular transforms, and malformed buffers fail with a message.

Map each NWN joint to a source bone. The source skeleton appears in red. Scrub to a reference
frame and pose the NWN rig to match it, then choose **Lock**. Calibration captures orientation
offsets in world space, so differently oriented source bones do not require Euler corrections.
**Bake** writes the source motion onto the normal NWN timeline. Root scale affects displacement
of the mapped animation root, while unmapped joints retain the calibration pose. The source
skeleton keeps its original proportions during calibration. Single-key glTF poses bake as one
held key in a one-second clip, matching static MDL import behavior. Save/load bone
maps as JSON; lock calibration again after changing the matching pose or map, using undo/redo,
or switching clips. A bake may evaluate at most two million combined source/target joint samples;
use a lower frame rate or a shorter source clip for larger rigs. Bake results remain editable
and undoable. Morph-target weights do not move the skeleton and are not retargeted.

## Install and use from C#

Choose a C# name such as `SaluteWithSaber`, then add the target `.mdl` files in the Install panel.
The installer generates a short internal clip name and retains your full C# name. Clip names use
at most 12 characters, leaving room for entry/exit suffixes within the engine's 16-character
animation replacement limit. Colliding short names get distinct numeric suffixes automatically;
reinstalling a registered animation keeps its internal name.
They must be winning resources in the repository's configured HAK source directories, and the
complete supermodel chain must be available there. Target rigs must contain the animated joints
with matching parent relationships.
Only the selected targets receive the new inheritance link; choose every body/phenotype that
should expose the animation.

**Preview installation** lists every file before writing:

- An `an_<target>.mdl` source beside each target contains a dummy skeleton, the new named clip and short entry/exit
  poses, and inherits the target's previous supermodel. The resulting resref must fit NWN's
  16-character limit. Subsequent installs append to this owned overlay; updating the same name
  replaces only its three animation blocks and requires the original target set.
- Each target's supermodel reference points to its overlay. Compiled model payloads remain
  unchanged outside the fixed supermodel-name field; ASCII geometry and unrelated text are preserved.
- `design/animations/registry.json` records the clip, natural duration, and target paths.
- `design/animations/<Name>.swlanim` stores editable authoring data.
- `SWLOR.Game.Server/Service/AnimationService/AuthoredAnimation.cs` supplies typed `AnimationClip` references.
  These SWLOR application types are generated outside the NWScript API.

Input files are checked again after confirmation and after staging, before publishing any output.
All unique input snapshots share a 128 MiB budget, including every selected target and its
supermodel chain. Select fewer targets when creating a new registration if needed.
Generated output payloads have a separate 128 MiB aggregate limit. Unchanged source dependencies
remain leased through publication and rollback so their hierarchy and configuration stay stable.
Missing higher-priority resource paths are held with exclusive, delete-on-close reservations
during the transaction. Competing reads or writes fail while these short-lived reservations
are held; the reservations disappear on success, failure, or process exit.
This includes previously absent higher-priority models that would change resource selection.
Writes are staged and roll back on failure. Publication and rollback capture and verify each
replaced file under an exclusive lease; a concurrent writer's replacement is preserved. Recovery
conflicts report the retained `.bak` path, and interrupted installs may also leave backups beside
the affected outputs.
The editor reserves the workspace during application of the installation transaction.
Target proportions and inherited animation scale are accounted for in the generated tracks.
The installed project becomes the document's saved project, and mounted HAK resources are refreshed.

RGB robe phenotypes also have generated animation bridges for their separate garment joints.
After changing animations inherited from a body rig, regenerate those bridges from the HAK
repository before packaging; otherwise a robe can continue using its older movement tracks:

```powershell
python -B tools/GenerateRobeRgbModels.py --game-data "<NWN installation>/data" --apply
python -B tools/GenerateRobeRgbModels.py --check
```

Run these commands from `SWLOR_Haks`. Package and deploy `sw_pt_root.hak`, `sw_pt_robe.hak`,
and `sw_2da.hak` together with the HAK containing the changed animation overlays.
The generator audits body poses, weapon attachments, garment bindings, and source/output hashes.

After **rebuilding/deploying the HAKs to server and clients** and rebuilding C#, use:

```csharp
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AnimationService;

NamedAnimation.Queue(creature, AuthoredAnimation.SaluteWithSaber);
NamedAnimation.Queue(creature, AuthoredAnimation.SaluteWithSaber, durationSeconds: 4f);
```

`NamedAnimation.Play` uses NWN's immediate `PlayAnimation` semantics. `Queue` preserves existing
actions. The helper temporarily maps `custom1start`, `custom1lp`, and `custom1end` on the target
creature and restores them afterward; the Point Forward emote serves as the playback carrier.
Do not independently replace that same carrier while the helper owns it. Ordinary Point Forward
requests during a named clip share that temporary mapping. A module-owned timeout restores the
mapping if a cleared action queue drops its cleanup action. Per-creature tokens prevent older
callbacks from clearing newer playback or touching a reused object handle.
Cleanup also releases the pose on an idle, living creature. The authored exit remains mapped for
half a second while NWN leaves the emote, preventing the carrier's original pointing gesture from
appearing after the clip. Deferred idle and cleanup callbacks recheck ownership so they cannot
interrupt a newer clip. Movement and combat retain their own actions.
Death clears named and queued attack mappings before player subdual/revival and creature death handling.
Exit poses sample the target's inherited idle, including rigs with an empty local idle clip.

This does not consume additional engine custom slots. SWLOR already assigns the 70 custom slots
exposed by its pinned NWN library; those IDs are not contiguous because mount/dismount intervene.
Installation therefore generates named clips rather than inventing new `Animation` enum values.
Runtime availability still depends on deploying the generated assets for the chosen target models.

The 70 engine slots are playback entry points, not the animation library's capacity. A named
clip can reuse the same creature-local carrier after another finishes; different creatures can
play different named clips simultaneously. The registry and generated constants have no 70-clip
limit. The installer splits large libraries into model banks of at most 256 clips (plus their
entry/exit phases), keeps names within 16 characters, and updates a clip in its original bank.
Constant transform tracks are stored once rather than repeated at every frame. A regression
test installs beyond 1,024 clips and updates an older bank without losing existing animations.
This verifies source installation and lookup, not NWN runtime performance: memory/loading cost
still grows with the assets. The installer validates the complete chain and rejects more than
32 models per target; very large libraries should be divided among the rigs that actually use them.

## Provenance and verification

The feature workflow was informed by the public README of
[NWNAnimationTool](https://github.com/SaverioTrapasso/NWNAnimationTool). No GPL source, assets,
Godot scenes, or GDScript were copied or translated. This implementation remains under SWLOR's
MIT license. The glTF reader follows the
[Khronos glTF 2.0 specification](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html).

`AnimationEditorTests` covers MDL exchange, native model preservation, IK, retargeting, installation
updates/conflicts, and the Avalonia document. `NamedAnimationPlaybackTests` covers playback ownership,
all three phase mappings, interruption cleanup, stale callbacks, and invalid duration handling.
Set `SWLOR_ANIMATION_CORPUS` to a local HAK source root to include the real `a_ba`/`a_fa` model checks.
