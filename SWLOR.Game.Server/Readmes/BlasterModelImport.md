# Importing and creating pistol blaster models

Use the repository [blaster skill](../../.codex/skills/swlor-blaster-models/SKILL.md)
for agent-assisted work. Commit only the finished model, compressed texture maps, material
definition and inventory icon for each weapon. Keep GR2/DDS inputs, Blender
scenes, per-model builders, manifests and review artifacts in ignored local
staging such as `.tmp/blaster/`. The normal game/HAK build uses the committed game
resources directly; it must not require those authoring files.

## Slots and existing replacements

Pistol base item **11** uses item class **WBwSh**. Native middle slots encode
`model * 10 + color`; the toolset shows `model-color`; the in-game appearance
catalog encodes `color * 100 + model`.

| Model | Native middle | Toolset | In-game editor | Texture |
|---|---:|---|---|---|
| Imported blaster_high41_a01 | 44 | 4-4 | Part #404 | blstr_h41 |
| Original Vesper-9 | 201 | 20-1 | Part #120 | blstr_vsp9 |

These are examples, not a complete free-slot inventory. Inspect the current HAK
resources, catalog and open asset PRs before selecting a slot. Choose another modern pistol for a new
design unless asked to revise one of them. Replacing a shared slot also changes
persisted items using it. Basic Pistol already uses middle 201. Do not substitute
the native-sling compatibility base item 61 or change item stats to select a model.

A middle slot uses `wbwsh_m_NNN.mdl` and `iwbwsh_m_NNN.dds` in
`SWLOR_Haks/sw_weapon/`. Register its catalog ID in
`PistolAppearanceDefinition.MiddleParts` if missing. Thus native 44 requires 404.
The example weapons use empty top/bottom parts 11. Texture resrefs must be unique,
meaningful and fit NWN's 16-character limit, including map suffixes.

## Import a rigid, opaque GR2 blaster

Use Blender **4.0** and the pinned SWTOR-Slicers GR2 importer **4.2.1**:

```powershell
powershell -ExecutionPolicy Bypass -File tools/SetupBlasterImporter.ps1
```

Setup downloads and verifies the add-on into `.tmp/blaster-tools`; it does not
change saved Blender preferences. Transparent, animated or emissive weapons need
separate material/geometry work rather than silent flattening by this preset.

Extract each archive into its own ignored input folder to avoid overwriting shared
texture filenames. Classify the actual mesh visually: rifle-named shared textures
do not establish that a model is a rifle. This importer and hand fixture are for
pistols; use the rifle-specific workflow for a rifle. Inspect material assignments
for missing or shared texture sets before conversion.

Copy the supplied inputs to an ignored local input folder. Create a local
`import.json` alongside them with this structure, replacing the filenames, slot,
texture resref, transforms and SHA-256 values for the asset being imported:

```json
{
  "schema_version": 1,
  "base_item": 11,
  "middle_slot": 44,
  "texture": "blstr_h41",
  "material_mode": "opaque",
  "scale": 9.6,
  "rotation_degrees": [84, 180, 0],
  "translation": [0, 0.035017366, -0.032737051],
  "texture_size": 1024,
  "sources": {
    "model": "blaster_high41_a01.gr2",
    "diffuse": "blaster_high41_a01_v01_d.dds",
    "normal": "blaster_high41_a01_v01_n.dds",
    "specular": "blaster_high41_a01_v01_s.dds"
  },
  "sha256": {
    "model": "<model SHA-256>",
    "diffuse": "<diffuse SHA-256>",
    "normal": "<normal SHA-256>",
    "specular": "<specular SHA-256>"
  },
  "attachment": {
    "source_muzzle_axis": [0, 1, 0],
    "source_grip_axis": [0, 0, 1]
  }
}
```

Use lowercase SHA-256 values from `Get-FileHash -Algorithm SHA256`. These transforms
apply to the supplied example's raw GR2 coordinates only, not an already fitted
MDL. The converter ignores the importer's automatic viewport rotation.

Run from the parent repository, with a new empty output directory:

```powershell
& 'C:/Program Files/Blender Foundation/Blender 4.0/blender.exe' `
  -b --factory-startup --python-exit-code 1 `
  --python tools/ImportBlasterModel.py -- `
  --manifest .tmp/blaster/inputs/import.json `
  --addon-root .tmp/blaster-tools/Granny2-Plug-In-Blender-2.8x-4.2.1 `
  --output .tmp/blaster/review
```

Inspect the staged model, maps, transparent 64x64 inventory icon, preview and
report. Compress the large maps as described below, then copy only reviewed game files
into `SWLOR_Haks/sw_weapon/`.
Inputs, manifest, generated scene and report stay local.

SWTOR packed normals use alpha for X and inverted green for Y; reconstruct and
normalize Z. Red/blue are not normal channels. Diffuse stays opaque; colored
specular RGB is retained. SWTOR gloss alpha is not NWN roughness. This conversion
uses NWN's default roughness and approximates the source material. Invert source
green exactly once during unpacking; Blender-baked OpenGL normals already have the
target convention and do not need another inversion. DDS row orientation is a
separate operation from changing normal Y.

For the supported SWTOR packed material, inspect the normal texture's blue channel
as the emission mask. A bright diffuse panel alone does not imply emission. Where
the mask is meaningful, multiply decoded diffuse RGB by the mask in linear light,
encode the result as a color DDS and bind it to MTR `texture5`. Zero-mask texels must
remain black; do not add a map when the mask is all zero. Review alignment on the
textured mesh. The generic opaque importer does not perform this emission step;
handle it explicitly after import. Preserve separate material subsets rather than
applying a fallback finish to the whole weapon when one source texture set is missing.

## Original designs and later edits

Author new geometry, UVs and material maps in a local Blender scene or local
design script. Use a distinct silhouette suitable for the setting. Keep model
authoring separate from gameplay stats and canonical manufacturer claims.
`write_mdl` in `tools/ImportBlasterModel.py` is reusable for exporting static
meshes supplied as `(vertices, UVs, triangle faces)` tuples with matching vertex
and UV counts. Split vertices at UV/hard-edge seams as needed before export.

For later changes to an existing weapon, start from its committed MDL,
MTR and DDS maps. They contain the shipped geometry, UVs and material inputs. Work on
a local copy, preserving resource names and UVs for a fit-only change. Apply
position/pitch changes around the grip and retain the approved uniform scale.
Do not apply the raw GR2 example transform a second time to an exported MDL.
Original procedural materials and source texture packing are not required to
edit the finished model or its textures.

NWN:EE PBR uses MTR `texture0` for base color, `texture1` for OpenGL tangent normals,
`texture2` for scalar specularity, and `texture3` for linear roughness. The standard
`inc_material` shader derives metallicness as `clamp(3 * specularity - 0.6, 0, 1)`.
Keep scalar maps linear. Use distinct responses for exposed metals, coatings,
composite grips and glass; avoid strong baked highlights. Prefer few meshes and
512-1024-square atlases sized to visible detail. The imported pistols span roughly
1,200-6,100 triangles; these are reference costs, not a reason to flatten a design
into coarse slabs. Compare shape construction, grip transitions, recessed detail
and material wear against accepted SWTOR assets under matching light. Preserve
silhouette and visual quality while reducing wasted geometry and texture space.

Validate the exported maps themselves: MTR bindings, dimensions, scalar channels,
normal lengths, UV assignment and visible material response. Review renders use
studio lighting; verify PBR in NWN with enhanced lighting enabled as well.

## Compress game textures with NWN Crunch

Use [NWN Crunch Enhanced Edition](https://neverwintervault.org/project/nwnee/other/tool/nwn-crunch-enhanced-edition)
for standard DDS output. Download/extract it into ignored local tooling; do not
commit its executable, archive, logs or intermediate textures. The tested Windows
64-bit build is dated March 3, 2023. These are runtime DDS files, distinct from the
original packed SWTOR DDS inputs kept outside Git.

Use 512-square maps where sufficient and at most 1024-square maps for typical
handheld blasters. Preserve smaller source maps; do not upscale a 256/512 map to a
larger atlas just because the manifest uses a common `texture_size`. The generic
importer resizes maps to that setting, so stage per-map corrections when needed.
Compare textured previews before reducing larger maps;
filter color in linear light and reconstruct/renormalize tangent normals. Constant
material maps can use 4x4 DDS with full mipmaps. Avoid allocating large maps to solid
colors. Preserve approved fit and silhouette when optimizing.
For opaque blaster maps, use these explicit options with `-fileformat dds
-unflip -yflip -dropEmptyAlpha -dxtQuality uber`:

| Map | Additional options | Stored channels |
|---|---|---|
| Base color / colored emission | `-DXT1 -gamma 2.2` | BC1 RGB |
| Tangent normals | `-DXN -normalize -renormalize -uniformMetrics -gamma 1.0` | BC5/ATI2 XY |
| Scalar specularity/roughness | `-DXT5A -setAtoY -gamma 1.0` | BC4/ATI1 scalar |
| Colored specular (imported blaster) | `-DXT1 -uniformMetrics -gamma 1.0` | BC1 RGB |

NWN's normal shader reads RG and reconstructs Z, so BC5 preserves the channels it
uses. Do not apply scalar conversion to colored specular maps. Linear-map mipmaps
need gamma 1.0. Keep the generated mip chain for 3D weapon maps. Small
64x64 inventory icons also use DDS. Use `-fileformat dds -unflip -yflip
-A8R8G8B8 -mipMode None` for these icons: lossless RGBA keeps their artwork and
soft alpha edges pixel-identical, with one image level and no mip chain. Each is
16,512 bytes. Verify both decoding and layered icon composition in the toolset.

For example, after extracting `nwn_crunch.exe` into `.tmp/blaster-tools/nwn-crunch/`:

```powershell
& .tmp/blaster-tools/nwn-crunch/nwn_crunch.exe `
  -file .tmp/blaster/review/resources/blstr_vsp9_n.tga `
  -out .tmp/blaster/review/resources/blstr_vsp9_n.dds `
  -fileformat dds -unflip -yflip -dropEmptyAlpha -DXN `
  -normalize -renormalize -uniformMetrics -gamma 1.0 -dxtQuality uber
```

Standard NWN DDS needs the vertical flip; check decoded orientation against the
TGA using the toolset's texture loader. Verify dimensions, mip levels, opacity,
color error, normal angular error and scalar response before publishing. Normal
Z may be reconstructed for comparison rather than read as a stored BC5 channel.
Inspect texture details and the textured model; confirm game lighting in the client.

Replace each runtime TGA, including inventory icons, with its DDS using the same basename. Do not keep
both formats for the same map: texture lookup can prefer the TGA. MTR bindings are
extensionless and do not change. Vesper-9 keeps four 1024-square maps; the imported
blaster keeps two 1024-square maps and its 256-square specular map. Their combined
texture payload is 5,637,112 bytes, versus 19,074,749 bytes as uncompressed TGAs.

## Hand fit

Fit the grip with uniform scale and preserve natural proportions. NWN pistol
space uses barrel **-Z** and grip **-Y**. The approved example's grip pivot is
`(0, 0.03, 0.015)`; its six-degree pitch correction rotates around that pivot.
Do not normalize every blaster to a compact pistol's total length. The example
scale 9.6 is a starting point, not a batch-wide fit guarantee: source grip sizes
vary. Refit oversized grips uniformly around the contact pivot and review both
sides. Widen the review camera for long barrels rather than truncating the check.

The actual human right-hand reference is `pmh0_handr001`. Custom pistol animation
uses **bowshot**, whose hook retains its rest orientation; generic **xbowshot**
rotates it differently. The human `rhand` hook sits at
`(0.0110681, 0, -0.0961281)` relative to `rhand_g`.

Render a local review scene already in NWN coordinates with:

```powershell
SWLOR_Haks/nwnmdlcomp.exe -d -e SWLOR_Haks/sw_pt_rhand/pmh0_handr001.mdl .tmp/hand001.mdl
& 'C:/Program Files/Blender Foundation/Blender 4.0/blender.exe' `
  -b --factory-startup --python-exit-code 1 `
  --python tools/RenderBlasterGrip.py -- `
  --source .tmp/blaster/review/review.blend --hand .tmp/hand001.mdl `
  --output .tmp/blaster/grip-review
```

The helper expects a scene with its camera and lighting, such as the importer's
review scene. `--fit` accepts a local JSON 4x4 `matrix` when an authored scene uses
different coordinates. Omit it for an already fitted scene. The reference hand
is a review fixture and must not enter the exported weapon.

## Validation and normal deployment

When changing the converter, run its focused tests:

The exporter writes transformed corner normals explicitly, including duplicated
vertices at UV seams. Do not drop those normals and rely on smoothing group 1:
the compiler would introduce hard lighting seams at the duplicated vertices.
Set `NWN_EE_NWMAIN` to the installed `nwmain.exe` path to include the native
compiler normal-preservation round trip in the tests below. Ordinary Python
runs the portable tests and skips Blender-only coverage when unavailable.

```powershell
& 'C:/Program Files/Blender Foundation/Blender 4.0/blender.exe' `
  -b --factory-startup --python-exit-code 1 `
  --python tools/tests/test_import_blaster_model.py
```

Check new geometry/export changes with `SWLOR.NWN.Formats.Mdl.MdlReader`: finite
vertices/UVs, valid face indices, material names and complete texture references.
Verify that the target native slot maps to an entry in the appearance catalog
and has both model and inventory resources. Validation must inspect game resources
directly; it must not depend on committed source manifests or Blender files.

Ship **compiled NWN:EE MDLs**. ASCII models force the client to compile geometry on
first load, which can cause a visible hitch. Use the installed game's native
compiler, which retains EE material names and tangent data; the legacy 2003
`nwnmdlcomp.exe` compiler can discard those fields. Keep `CompileModels: false`
in `Build/hakbuilder.json` so that the legacy build step does not recompile them.

Stage ASCII MDLs and all maps/MTRs in an ignored local resource folder, then run:

```powershell
python tools/CompileBlasterModels.py `
  --nwmain 'C:/Program Files (x86)/Steam/steamapps/common/Neverwinter Nights/bin/win32/nwmain.exe' `
  --resources .tmp/blaster/review/resources --output .tmp/blaster/compiled
```

This invokes `compilemodel <resref>` with an isolated user directory, waits for each
process to exit, and checks the engine success log and binary header. Optional
`--models wbwsh_m_044` limits the selection. Use a fresh output directory. Copy only
compiled MDLs into runtime staging; logs and ASCII intermediates stay local.
Check compiled geometry, UVs, normals, EE material names and texture dependencies
with the native reader before packing. Verify tangent data when changing export or
compiler paths, and compare visible triangle corners and UVs against the staged
ASCII to detect compiler changes. Native compilation may split seam vertices or
remove degenerate triangles; vertex counts alone do not establish geometry loss.
Remove exact zero-area export faces and compact unused vertices where practical.
When comparing before/after compilation, match triangle corner positions and UVs,
not vertex indices. Any removed face must be proven zero-area; do not waive a
mismatch for a visible face. On vertices referenced by faces, check finite unit
normals/tangents, near-zero normal/tangent dot product and handedness of +1 or -1.
The compiler may retain unused vertices after removing degenerate faces; their
zero handedness is not a rendered tangent failure. The helper's header/log checks
alone do not establish material, tangent or visual correctness.
For later edits, read the binary with `MdlReader` and export a local editable mesh;
do not use the legacy decompiler to round-trip EE material fields.

The normal Windows server build deploys its initiating Debug/Release/custom
output and rebuilds changed HAKs into `debugserver/hak`. Verification builds use
`-p:RunPostBuildEvent=Never` to skip deployment. Standalone CLI `-o` uses the
Release output produced by `RunCLI.cmd`.

For requested deployment, extract the affected resources from the packed HAK
with `nwn_erf.exe -f <hak-path> -x <resource-names>` into a fresh scratch directory.
Compare hashes with committed `sw_weapon/` inputs. Check the actual server/client
HAK paths and restart NWN to clear cached models and textures. A resource-only
replacement needs no module repack; blueprint changes do.

Verify inventory/ground appearance, idle/attack grip, muzzle direction and body
sizes in the client. Imported slot 44 has user approval for its final size/fit;
Vesper-9 has offline hand and deployed-HAK verification, with live-client review
pending. Record checks and feedback in the PR. See the skill's
[troubleshooting reference](../../.codex/skills/swlor-blaster-models/references/troubleshooting.md)
for stale deployment or appearance-editor problems.

Publish companion HAK PRs for parent submodule changes and link them both ways.
Only game resources belong in an asset PR; reusable tooling belongs in the
process PR. Repeat slot replacements individually and preserve unrelated changes.
