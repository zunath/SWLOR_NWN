# Animation Editor

Open **Tools → Animation Editor**. This editor uses C#, Avalonia, `System.Numerics`, the native
SWLOR MDL reader, and the existing OpenGL model preview. Godot, GDScript, and a separate game
engine are not required.

## Authoring

1. Enter a mounted NWN model resref (for example `a_ba`) and choose **Load rig**, or choose
   **Load rig file** for an ASCII or compiled `.mdl`. The model supplies the real joint names,
   hierarchy, and rest transforms. Loading a new rig prompts before replacing unsaved work.
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
5. Save an editable `.swlanim` project or export a `newanim`/`doneanim` MDL text block. Import
   accepts one ASCII animation block against the loaded rig, including constant transforms,
   transform keys, and animation events. Unsupported controllers fail explicitly rather than
   disappearing during a round trip. Compiled MDLs can supply rigs; animation-block import is ASCII.

Save, Save All, Ctrl+Z, Ctrl+Y, external-change checks, and unsaved-close prompts use the normal
document workflow. Playback stops when the view detaches. History is bounded to 100 recent
entries and 32 MiB of serialized snapshots. Projects are bounded to 64 MiB; very dense bakes may
need a lower frame rate. Long timelines draw markers without creating one UI control per key.

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
of the mapped animation root, while unmapped joints retain the calibration pose. Save/load bone
maps as JSON; lock calibration again after loading or changing a map. Bake results remain editable
and undoable. Morph-target weights do not move the skeleton and are not retargeted.

## Install and use from C#

Choose a C# name such as `SaluteWithSaber`, then add the target `.mdl` files in the Install panel.
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
- `SWLOR.NWN.API/NWScript/Enum/AuthoredAnimation.cs` supplies typed `AnimationClip` references.

Input files are checked again after confirmation. Writes are staged and roll back on failure.
The editor reserves the workspace during application of the installation transaction.
Target proportions and inherited animation scale are accounted for in the generated tracks.
The installed project becomes the document's saved project, and mounted HAK resources are refreshed.

After **rebuilding/deploying the HAKs to server and clients** and rebuilding C#, use:

```csharp
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

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

This does not consume additional engine custom slots. SWLOR already assigns the 70 custom slots
exposed by its pinned NWN library; those IDs are not contiguous because mount/dismount intervene.
Installation therefore generates named clips rather than inventing new `Animation` enum values.
Runtime availability still depends on deploying the generated assets for the chosen target models.

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
