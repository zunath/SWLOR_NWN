# Importing and creating pistol blaster models

Use the repository [blaster skill](../../skills/swlor-blaster-models/SKILL.md)
for agent-assisted work. Commit only the finished model, compressed texture maps, material
definition and inventory icon for each weapon. Keep GR2/DDS inputs, Blender
scenes, per-model builders, manifests and review artifacts in ignored local
staging such as `.tmp/blaster/`. The normal game/HAK build uses the committed game
resources directly; it must not require those authoring files. Delete each
intermediate once used and empty the staging folder after the finished resources
are committed (see [AgentEnvironment.md](AgentEnvironment.md), "Disk hygiene").

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

## Generate local weapon sources with TRELLIS.2

`tools/GenerateWeaponModels.py` runs the retained
`tools/workflows/trellis2_weapon.json` image-to-GLB workflow against an already
running local ComfyUI instance. It uses Python's standard library, requires no
Xenomech checkout, and does not call Meshy or paid APIs. It does not install,
restart, or stop ComfyUI. The default endpoint is `http://127.0.0.1:8000`;
`--url` accepts another loopback HTTP endpoint. External API nodes are rejected.

Use a compatible ComfyUI build with the TRELLIS.2 and mesh processing nodes;
preflight checks their current input names, links and model choices. The tested
runtime reports ComfyUI 0.38.0 and includes these nodes in its core installation.
Its configured model search paths must contain:

- `diffusion_models/trellis_2_int8_convrot.safetensors`
- `vae/trellis_2_shape_vae_bf16.safetensors`
- `vae/trellis_2_texture_vae_bf16.safetensors`
- `clip_vision/dino_v3_L_naf_fp32.safetensors`
- `background_removal/birefnet.safetensors`

Create concept images and a schema-1 manifest outside Git, for example in a
new temporary directory. Paths in `image` are relative to the manifest, or
absolute. Explicitly Git-ignored staging is also accepted. Do not commit input
images, per-weapon manifests, prompt journals, GLBs or generated reports.

```json
{
  "schema_version": 1,
  "assets": [
    {
      "id": "spear_field",
      "image": "concepts/spear_field.png",
      "seed": 123,
      "shape_resolution": 512,
      "texture_size": 1024,
      "target_faces": 6000
    }
  ]
}
```

Shape resolution accepts 512 or 1024; 512 bypasses the shape upsampling stage.
Texture size independently accepts 512 or 1024. Seed is required; default shape,
texture and target-face budgets are 512, 1024 and 6000. Face count is a decimator
target, not a guarantee about the final mesh. Choose budgets from the weapon's
silhouette and source quality, then inspect the result before fitting.

```powershell
python -B tools/GenerateWeaponModels.py "$env:TEMP/weapon-sources/manifest.json" `
  --output "$env:TEMP/weapon-sources/generated" --preflight
# After the read-only preflight succeeds:
python -B tools/GenerateWeaponModels.py "$env:TEMP/weapon-sources/manifest.json" `
  --output "$env:TEMP/weapon-sources/generated" --timeout 1800
```

The runner holds one local GPU-owner lock, waits for an empty ComfyUI queue,
and finishes one owned prompt before submitting the next. It never clears the
queue or interrupts someone else's job. Each asset has a persisted deadline,
including queue wait. A rerun renews an expired deadline only while the job is
still `prepared` (never submitted); once submission has started, restarting
cannot extend it. The prompt and unique recovery token are journaled before
submission. Repeat the identical command to collect a
known job or reconcile a lost response against queue/history. If that cannot
identify exactly one job, the runner stops; inspect its journals and ComfyUI
history before deciding what happened. Do not delete the journal or blindly
resubmit. An expired running job remains owned by its recorded prompt ID and
can be collected once complete. A failed job stops the batch.

Each asset folder contains `source.glb`, unchanged embedded PBR image bytes,
`generation.json` with hashes/material bindings/channel metadata, and local
prompt/submission/history records. Base color uses sRGB; packed metallic/roughness
uses linear B/G channels. The workflow bakes an actual OpenGL tangent normal map
from the high-poly remesh into the decimated mesh's UVs and includes it in GLB
`normalTexture`; it does not invent normal detail from color. Preserve those UVs,
material subsets and authored normals during subsequent export. The workflow
version and required live node schemas are checked before any submission.

Generated sources still require geometry/material inspection, uniform fitting to
the native hand and slot, DDS conversion, compilation, matching inventory artwork,
and actual NWN screenshots. Generation or preflight success does not establish
usable topology, correct grip, orientation or native rendering. Use the GLB
adapter below, then complete native verification. Focused runner tests use no GPU:

```powershell
python -B -m unittest discover -s tools/tests -p test_generate_weapon_models.py
```

### Generate closed shield surfaces

Use `tools/workflows/trellis2_shield.json` for thin shield sources. It adds a
Blender 4.0 surface reconstruction stage before UVs and material baking; the
default weapon workflow remains available for other weapon families.

Snapshot `tools/TrellisShieldSurface.py` and `tools/trellis2_shield_nodes/` into
task-owned staging with the same relative layout. Link that snapshot's node
package into the task-owned ComfyUI `custom_nodes` directory, then start that
service with `SWLOR_BLENDER` set to the installed Blender 4.0 executable and
`SWLOR_SHIELD_CACHE` set to an absolute task-owned cache directory. Do not alter
or restart a shared service. Keep the helper snapshot unchanged while jobs run.
The ordinary generation runner, manifests, ownership lock and recovery journals
also apply to this workflow:

```powershell
python -B tools/GenerateWeaponModels.py "$env:TEMP/shield-sources/manifest.json" `
  --workflow tools/workflows/trellis2_shield.json `
  --output "$env:TEMP/shield-sources/generated" --preflight
```

After preflight succeeds, repeat with `--timeout 1800` instead of `--preflight`.
The continuous UDF high surface is voxel reconstructed, then collapsed to the
manifest's `target_faces` budget. This shield stage rejects an oversized result
when collapse returns more triangles than that limit. The
workflow unwraps that low surface and
establishes its normals before baking the original material field and high
surface detail onto that same mesh. Positive-Z float normals are validated
before UNORM8 rounding. The helper rejects textured inputs rather than
discarding authored materials or UVs. Preflight validates the Blender executable and cache settings before submitting
work. Cache keys include the Blender executable hash/build and helper hash;
ComfyUI history records those identities. Incomplete NPZ/report pairs from an
interrupted publication are rebuilt, while changed complete entries are rejected.
Payloads are flushed and their renames persisted before completion; both high
and low surfaces receive component-aware zero-volume checks. Export still
requires strict closed-surface validation.

The default voxel size is `.003` in source coordinates. A source that fails
closure needs inspection; any changed voxel size requires a separate workflow,
fresh output and new journals. Preserve the failed history. Direct raw decode
can retain perforated sheets even after closure, so inspect the front, rim,
back and rear grip before accepting any reconstructed source. Topology tests do
not establish visual quality or native hand fit.

Run the focused repair checks inside Blender 4.0:

```powershell
& 'C:/Program Files/Blender Foundation/Blender 4.0/blender.exe' `
  -b --factory-startup --python-exit-code 1 `
  --python tools/tests/test_trellis_shield_surface.py
```

### Inspect, fit and package a generated GLB

`tools/TrellisWeaponExport.py` runs `inspect` and `export` inside Blender 4.0;
`package` uses ordinary Python with NumPy, NWN Crunch EE and the installed NWN:EE
compiler. Every output directory must be empty and outside the repository.
The adapter reads `baseitems.2da` from the initialized HAK submodule; pass `--repo`
when the intended checkout differs from the tool's own checkout.

Create a separate local fit manifest. Inspection requires only `schema_version`,
and each asset's `asset`, `source`, `sha256` and `base_item`. Export additionally
requires the measured transform, native resource names, texture budget, attachment
review and icon canvas shown below. These values illustrate the schema, not an
approved fit or a free appearance slot; replace them from the inspected source
and audited consumers. `sha256` binds the complete self-contained GLB.

```json
{
  "schema_version": 1,
  "assets": [{
    "asset": "spear_field", "source": "generated/spear_field/source.glb",
    "sha256": "<source-sha256>", "base_item": 58,
    "model": "wplss_t_081", "texture": "spear_field", "texture_size": 1024,
    "scale": 1.0, "orientation": [[1,0,0],[0,1,0],[0,0,1]],
    "source_grip": [0,0,0], "target_grip": [0,0,0], "icon_size": [32,128],
    "parts": [
      {"model":"wplss_b_011", "clip_max_y":-0.1},
      {"model":"wplss_m_013", "clip_min_y":-0.1, "clip_max_y":0.1},
      {"model":"wplss_t_081", "clip_min_y":0.1}
    ],
    "attachment": {
      "review_notes": "Replace with measured hand, blade/edge and shared-slot findings",
      "axis_pairs": [
        {"source": [0,1,0], "target": [0,1,0]},
        {"source": [1,0,0], "target": [1,0,0]}
      ]
    }
  }]
}
```

```powershell
$blender = 'C:/Program Files/Blender Foundation/Blender 4.0/blender.exe'
& $blender -b --factory-startup --python-exit-code 1 `
  --python tools/TrellisWeaponExport.py -- --mode inspect `
  --manifest "$env:TEMP/weapon-sources/fit.json" --output "$env:TEMP/weapon-sources/inspect"
# After inspecting axes/materials and replacing the illustrative fit values:
& $blender -b --factory-startup --python-exit-code 1 `
  --python tools/TrellisWeaponExport.py -- --mode export `
  --manifest "$env:TEMP/weapon-sources/fit.json" --output "$env:TEMP/weapon-sources/export"
python -B tools/TrellisWeaponExport.py --mode package `
  --export "$env:TEMP/weapon-sources/export" --output "$env:TEMP/weapon-sources/package" `
  --crunch '<local-nwn_crunch.exe>'
```

Transforms use Blender's imported world coordinates, after glTF axis conversion:
`target_grip + orientation * scale * (point - source_grip)`. Use uniform scale,
a proper rotation and two independent reviewed axis pairs. Audit every native
B/M/T consumer before replacing shared resources: ModelPart1 is bottom, 2 middle,
3 top. Top-level `aliases` are only for simple models; composite variants use
role-preserving aliases on their individual parts.

Large shields use base item **56**, simple model family `ashlw_###`, inventory
artwork `iashlw_###.dds`, and a **64-by-128** icon canvas (64-by-96 logical
artwork). Export one complete model per shield appearance. Measure its native
root, front and up axes from the source shield and verify the left-hand grip.
Measure rear hardware in the actual native shield attachment frame; do not reuse
a handheld weapon's `lhand_g` transform. Do not apply composite weapon cuts. Preserve the two native shield impact
hooks with required `attachment_dummies`, each containing `name`
(`imp_shield_1` or `imp_shield_2`), root-space `position`, and a unit XYZW
`quaternion`. These hooks position impact effects; they are separate from the
grip geometry. Keep the existing appearance IDs and persisted item instances.
`parts` entries have `model`, `plane_y`, `clip_side` (`above`/`below`) and optional
`aliases` for complementary uncapped splits. Optional top-level `empty_parts`
entries require `model`, `base_item` and `icon_size` and are permitted only for
explicitly audited simple-model IDs. Composite weapons must export real bottom,
middle and top geometry; blank companion parts are rejected. Fit common joins,
cap or cover the cuts, and verify independent mixed assemblies in the native
client. Composite exports must supply all three roles within each asset, or explicitly
list `composite_assemblies` with `base_item`, `bottom`, `middle`, `top` and
`icon_bounds: {"min":[x,y,z],"max":[x,y,z]}` in fitted world coordinates. Every
referenced model must be exported in the same manifest. Grouped sources share
that framing for cameras and lighting; their icon canvases, art sizes, directions
and rolls must match, and geometry outside the frame is rejected. This supports
separately fitted source parts without absent companions or misaligned layers. Parts may use
finite `clip_min_y` and `clip_max_y` bounds for a middle interval, or the legacy
one-plane split. Multiple parts cut from one source must partition its Y axis
without gaps or overlaps; normal interpolation stays linear across all cuts and
normalizes only at the end. Neither clipping mode caps cuts automatically; author covered
connectors or closed ends before native qualification. The tool checks output
coverage and family/name collisions, not the full consumer graph.

Before choosing cuts, inspect world-transformed native geometry for the full PR
delta and the unchanged selectable parts in each affected appearance catalog.
Do not infer universal compatibility from three nonempty models or new-only
joint envelopes. Staff roles illustrate the difference: stock 01x-04x use bottom
for the central grip (Y -0.3 to 0.9), middle for both adjacent end bands (-0.6 to
-0.3 and 0.9 to 1.2), and top for both terminal sections (-0.8 to -0.6 and 1.2 to
1.4). Stock 05x has an authentic lower/center/upper partition. A paired-band role
therefore needs both disconnected sections within its native component; a generic
two-plane split does not fit every family. Inspect legacy whole-weapon, blank,
shifted-origin and differently partitioned exceptions separately. Empty axial
slabs can disprove a fit, while continuous axial coverage does not prove surface
contact, material continuity or visible compatibility. Confirm representative
mixed assemblies in the native client and state the remaining limits explicitly.

`icon_size` is the measured native power-of-two canvas, not just inventory cells.
For example, a 2-by-5 logical area occupies 64-by-160 artwork in a 64-by-256 canvas;
the adapter pads transparently without stretching it. Source material subsets,
UV seams and authored corner normals are retained. Unsupported material features
fail explicitly. Samplers support repeat wrapping, linear magnification and
trilinear minification; clamp, mirror and other explicit filters are rejected.
Normal reprojection fills all unused RGB texels from native-frame coverage before
mip generation, preserving covered texels and alpha. Every matched face must
cover a texel center; subtexel/degenerate UV faces fail explicitly. This removes untouched
source-frame padding from every mip footprint. A double-sided source is rejected unless the fit explicitly sets
`double_sided_policy` to `closed_surface` and its shells pass the closure/winding
and enclosed-cavity checks; inspect the resulting culling and shading too.

Inspection produces component/material reports, axis views and a local scene.
Export produces ASCII models, MTRs, TGA intermediates, icon previews and a local
`source_basis.npz` whose hash is bound into `export_manifest.json`. Package verifies
those hashes and compiles MDLs first. Its reusable `tools/TrellisWeaponNormals.py`
helper then matches native triangles, including barycentric matches for splits,
and reprojects baked source normals into the actual compiled normal/tangent/sign
basis before DDS compression. This accounts for glTF UV orientation and native
handedness without applying SWTOR channel unpacking. UV ownership conflicts and
unrepresentable negative tangent Z fail explicitly; they are never silently clamped.
The same package command above performs this step; keep the source-basis file
beside the export until packaging finishes. Package reports retain the matching,
quantization and map-hash evidence alongside directed corner and dependency checks.
The default `normal_basis_policy: "reproject"` remains strict and rejects ambiguous
interpolated tangent handedness. An explicitly reviewed asset may instead select
`normal_basis_policy: "authored_tangents"`: after compilation, the helper patches
only existing, uniquely owned tangent/sign streams. It preserves compiler normals,
positions, UVs, indices, materials and the staged normal TGA, and uses NWN's smooth
handedness interpolation followed by its sign threshold. Mixed-sign triangle
shading is undefined in glTF; this policy does not recover the high-poly normal
field or promise identical glTF shading. Reports bind the source GLB and immutable
basis hashes and record mixed-sign faces, byte preservation and encoding error.
Both policies require close native shading review, including seams and highlights.

Only the reviewed `package/runtime_resources` belong in the HAK. Local geometry
repairs must preserve or reproject their baked normal field before this final
source-to-native conversion; unit tangent checks alone do not establish matching shading.
Keep repair inputs, manifests and proof reports local, and verify hand contact,
attack poses, shared joins, ground appearance and inventory in NWN before adoption.

Run the committed adapter and shared converter tests inside Blender so geometry
and image checks execute as well as the pure Python checks:

```powershell
& $blender -b --factory-startup --python-exit-code 1 --python tools/tests/test_trellis_weapon_export.py
& $blender -b --factory-startup --python-exit-code 1 --python tools/tests/test_import_blaster_model.py
```
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
composite grips and glass; avoid strong baked highlights.

TRELLIS marks most hard surfaces as metal but gives them dark base colors
(median linear luminance 0.085; real metal reflects 0.5-0.95) and satin
roughness. NWN tints the reflection of a metal texel by its base color and picks
the environment-map mip as `roughness * 30 - 1`, so that output renders near
black and matte, which players read as cloth or plastic. `TrellisWeaponExport.py`
therefore passes every material through `tools/TrellisNwnMaterial.py`: metal
base color rises toward the physical range while keeping its hue (saturated
finishes gain less), metal roughness tightens to 0.06-0.25, and non-metal texels
get specularity 0.06 instead of a glossy 0.2. Its command line applies the same
correction to already shipped maps and refuses maps it has converted before. Prefer few meshes and
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
-DXT5 -dxtQuality uber -gamma 2.2 -mipMode None` for these icons. DXT5 retains
transparency and produces a 4,224-byte standard DDS with one image level. Do not
use `-A8R8G8B8`: the native `CResDDS::GetChannelCount` recognizes DXT1, DXT5,
ATI1 and ATI2, but returns zero for the uncompressed RGBA FourCC. A generic DDS
decoder accepting the image does not establish game compatibility.
Verify both decoding and layered icon composition in the toolset,
then verify native inventory selection as described below. Keep the artwork in
the DDS; when no TGA of that name exists in any layer, add the 8x8 registration
stub described below.

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
both formats for the same map: texture lookup can prefer the TGA. The one exception is the 8x8
inventory-icon registration stub described under "Inventory lookup and unique artwork". MTR bindings are
extensionless and do not change. Vesper-9 keeps four 1024-square maps; the imported
blaster keeps two 1024-square maps and its 256-square specular map.

## Inventory lookup and unique artwork

Every catalog appearance needs its own visible artwork matching the finished
model. Replacing geometry in an occupied slot also requires reviewing that slot's
icon. Audit the entire pistol catalog, including the transparent end layers:

```powershell
python -m unittest discover -s tools/tests -p test_pistol_inventory_icons.py
```

This requires Pillow and the initialized HAK submodule. It checks committed game
resources without authoring inputs: model/icon presence, 64x64 dimensions,
DXT5 encoding, transparency, distinct visible pixels and stale TGA shadows of DDS icons. Legacy
TGA-only slots are included in the artwork audit; new/replacement icons use DDS.
Review a labeled contact sheet and provide an icon preview with the handoff.

**Resource validation is not native inventory validation.** A DDS can decode in
the custom toolset and be packed correctly while the game still selects an old
icon. Restart the client and switch between distinct appearances, including the
starting pistol. Confirm the inventory/equipment icon changes with the held model.
If it does not, inspect resource discovery and load precedence before redrawing
artwork or changing item stats/appearance IDs.

NWN:EE Windows x64 build **8193.37-17 / 26c6e573** has a second, separate issue:
`CNWCItem::UpdateIcons` checks only TGA resource type 3 for each composite part
and for complete (simple) item icons such as shields.
Without a same-named TGA in any loaded resource layer, it replaces the requested
resref with the base item's generic icon, even when the requested DDS exists.
The Basic Pistol's native slot 201 and Part #111 demonstrate this fallback;
simple shield slots 57-60 also fall back when their icons are DDS-only.
The artwork can be unique and correctly packed while the displayed icon is wrong.

Once that TGA check passes, the inventory draws the same-named DDS, not the TGA
(verified on stock 8193.37-17 with a magenta stub TGA beside a DDS icon, and again
with the shield and greatsword icons in the weapon/shield texture fix). So keep
the artwork in the DDS and give each icon that has no TGA in any layer (HAKs or
the base game) an 8x8 registration stub: `tools/FixWeaponInventoryIcons.py audit`
lists them and `stubs` writes a bottom-left TGA thumbnail of the DDS art beside
each one. Do not change persisted weapon appearances to dodge the check. The
pistol and rifle icon audits (`test_pistol_inventory_icons.py`,
`test_rifle_inventory_icons.py`) still reject a TGA beside a DDS; their icons
have no stubs yet. Broader engine background is available in the
[texture notes](https://nwn.wiki/spaces/NWN1/pages/38174958/Textures).

The inventory shows only the top-left `InvSlotWidth x InvSlotHeight * 32` pixels
of each icon layer (64x160 for greatswords and twinblades, inside a 64x256
canvas). Render inside that area, in the stock frame of the family, so layers
join where retained stock layers expect them; `FixWeaponInventoryIcons.py audit`
also lists art outside the slot, and `reframe` scales it back uniformly.

### Optional Windows client compatibility patch

`tools/PatchDdsInventoryIcons.py` creates a separate client executable for the
verified build. At the original composite-part and simple-icon lookup calls, it
preserves the TGA check, then tries DDS resource type 2033 for the **same resref**
before allowing the original generic-icon fallback. Existing TGA icons and genuinely missing
resources retain their old behavior. No TGA resources are added or fabricated.

The tool validates the complete source SHA-256, refuses other builds and previously
modified binaries, and refuses to overwrite either the source or an existing
output. It validates both lookup call sites and preserves the entry point and
original code apart from those two calls; generic fallback and PLT lookups are
unchanged. It adds a readable/executable section with Windows unwind metadata, and recalculates
the PE checksum. It does not patch a running process, replace Steam's executable,
change the server or distribute a game binary through Git/NWSync.

To validate the supported executable without writing:

```powershell
python tools/PatchDdsInventoryIcons.py --source 'C:/Program Files (x86)/Steam/steamapps/common/Neverwinter Nights/bin/win32/nwmain.exe' --check-only
```

To opt into the patch, use `--output` instead of `--check-only`, choosing a new
filename such as `nwmain-dds.exe` **beside the original executable** so the normal
game assets and DLLs resolve. Launch that separate executable directly to use the
fix; the normal Steam launch still uses the original. Roll back by launching the
original executable. Each affected player needs a compatible client, and any game
update needs a fresh verification; do not bypass the version check.

Native regression tests require `pefile`, `unicorn`, the initialized HAK submodule
and `NWN_EE_NWMAIN` pointing to the original Windows executable:

```powershell
python -m unittest discover -s tools/tests -p test_dds_inventory_client_patch.py
```

These tests execute the original and patched lookup instructions with a simulated
resource index, reproduce the fallback for the reported pistol and shield slots,
cover every shipped DDS pistol icon, and check native DDS format recognition, preserved TGA
behavior, missing-resource fallback, unchanged generic/PLT calls, PE integrity and
unwind metadata. They do not exercise the GPU or inventory window; record live
client verification separately.

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
& "$env:LOCALAPPDATA/SWLOR/tools/cleanmodels-qt/1.1.7/cleanmodels-qt/cleanmodels.exe" decompile `
  SWLOR_Haks/sw_pt_rhand/pmh0_handr001.mdl .tmp/hand001.mdl
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
Set `CLEANMODELS` to the pinned `cleanmodels.exe` path to include the
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

Ship **compiled NWN:EE MDLs** using [cleanmodels-qt v1.1.7](https://github.com/plenarius/cleanmodels-qt/releases/tag/v1.1.7),
which bundles CleanModels CLI v4.4.2. ASCII models force the client to compile
geometry on first load, which can cause a visible hitch. Install the pinned
Windows release once (archive and executable hashes are verified):

```powershell
powershell -ExecutionPolicy Bypass -File SWLOR_Haks/tools/SetupModelCompiler.ps1
```

The tools discover `%LOCALAPPDATA%/SWLOR/tools/cleanmodels-qt/1.1.7/cleanmodels-qt/cleanmodels.exe`.
For a different install folder, set `CLEANMODELS` or pass `--compiler <path>`.
Use the CLI bundled with the GUI; neither the old `nwnmdlcomp.exe` nor the game
executable is part of the compile path. HAK packaging copies the precompiled
bytes; leave the unused `CompileModels` configuration field false.

Stage ASCII MDLs and all maps/MTRs in a local resource folder, then run:

```powershell
python tools/CompileBlasterModels.py `
  --resources .tmp/blaster/review/resources --output .tmp/blaster/compiled
```

The helper compiles the selected models as one CLI batch, makes staged MTRs
available for tangent generation, records `compile.log`, and checks every output's
binary section sizes before publishing any MDL. Optional `--models wbwsh_m_044`
limits the selection. Use a fresh output directory. Copy only compiled MDLs into
runtime staging; logs and ASCII intermediates stay local.
Check compiled geometry, UVs, normals, EE material names and texture dependencies
with the native reader before packing. Verify tangent data when changing export or
compiler paths, and compare visible triangle corners and UVs against the staged
ASCII to detect compiler changes. Compilation may split seam vertices or
remove degenerate triangles; vertex counts alone do not establish geometry loss.
Remove exact zero-area export faces and compact unused vertices where practical.
When comparing before/after compilation, match triangle corner positions and UVs,
not vertex indices. Any removed face must be proven zero-area; do not waive a
mismatch for a visible face. On vertices referenced by faces, check finite unit
normals/tangents, near-zero normal/tangent dot product and handedness of +1 or -1.
The compiler may retain unused vertices after removing degenerate faces; their
zero handedness is not a rendered tangent failure. The helper's header/report checks
alone do not establish material, tangent or visual correctness.
For later edits, read the binary with `MdlReader` and export a local editable mesh;
use CleanModels when an ASCII decompile is needed, and verify the round trip.

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
[troubleshooting reference](../../skills/swlor-blaster-models/references/troubleshooting.md)
for stale deployment or appearance-editor problems.

Publish companion HAK PRs for parent submodule changes and link them both ways.
Only game resources belong in an asset PR; reusable tooling belongs in the
process PR. Repeat slot replacements individually and preserve unrelated changes.
