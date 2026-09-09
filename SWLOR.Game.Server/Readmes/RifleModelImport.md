# Importing rifle models

Use `tools/ImportRifleModel.py` in Blender 4.0 with the pinned GR2 4.2.1
add-on installed by `tools/SetupBlasterImporter.ps1`. It shares static geometry,
normal-map decoding and material export with the [blaster converter](BlasterModelImport.md),
but uses rifle attachment coordinates, material validation and inventory dimensions.
Keep original archives, extracted files, manifests, scenes and review images in
ignored `.tmp/rifle/` staging. Only finished MDL, MTR and DDS resources belong in HAKs.

Rifles are base item **7**, item class **WBwXl**. The appearance editor ID is
`color * 100 + model`; the native middle part is `model * 10 + color`.
For example, editor 115 replaces `wbwxl_m_151.mdl` and `iwbwxl_m_151.dds`.
Do not interpret editor IDs as native part numbers. Register new editor IDs in
`RifleAppearanceDefinition.MiddleParts`; existing replacements need no catalog change.

Extract nested archives separately so similarly named texture sets do not overwrite
one another. Import rifle meshes, including rifle bowcasters and scoped rifles.
Exclude cannons regardless of naming: supplied cannon archives may start with `as_`,
and meshes may start with `as_a0x` or `assaultcannon_`. The rifle entry point accepts
only `rifle_*.gr2`. Shared `blaster_*` texture names do not change mesh classification.

Use the blaster manifest schema with `base_item: 7`, the native `middle_slot`,
an inspected `source_material` matching the imported GR2 assignment, and
`preserve_emission: true`. Supply all source file hashes. Resolve diffuse, normal
and specular files independently: their variant names need not match. This rigid
single-material preset rejects multiple material assignments instead of flattening them.

Rifle attachment space is **barrel -Z, grip -Y** after composing mesh-node transforms.
The legacy `wbwxl_m_151` vertex arrays alone point +X with grip -Z; each mesh
also has axis-angle rotation `[0.5773503, -0.5773503, -0.5773503, -2.0944]`
and position `[-0.00521167, 0.0562191, -0.0280922]`. Ignoring those node fields
leaves the weapon hanging below the hands. Always inspect complete node transforms,
including ancestors, when deriving attachment conventions from an existing model.
The supplied SWTOR rifles use source barrel +Y and grip +Z: Euler rotation
`[90, 180, 0]` maps those axes correctly. Uniform scale `9.6` and translation
`[-0.00521167, 0.0362191, -0.0580922]` compose the batch fit with that reference
transform. These values are a starting fit, not a universal fit guarantee. Inspect grip/trigger
contact, stock clearance and support-hand placement against rifle animation poses;
never reuse the pistol bowshot fixture unchanged. Retain proportions and fit around
the grip rather than normalizing rifles and long sniper barrels to equal lengths.

```powershell
& '<Blender 4.0>/blender.exe' -b --factory-startup --python-exit-code 1 `
  --python tools/ImportRifleModel.py -- `
  --manifest .tmp/rifle/inputs/rifle.json `
  --addon-root .tmp/blaster-tools/Granny2-Plug-In-Blender-2.8x-4.2.1 `
  --output .tmp/rifle/review
```

Review the textured preview and the **64 x 128** transparent middle inventory layer.
The converter preserves smaller source maps, caps maps at the manifest size,
unpacks normal alpha/green once, preserves colored specular, and derives emission
from normal blue multiplied by diffuse in linear light. Zero-mask pixels stay black.
The normal blue channel is not part of the decoded normal vector.

Compress maps with the documented NWN Crunch channel presets; icons use DXT5
DDS with alpha and no mipmaps (`-fileformat dds -unflip -yflip -DXT5 -dxtQuality uber -gamma 2.2 -mipMode None`). Native CResDDS cannot decode uncompressed RGBA DDS. Replace the matching old icon TGA so it cannot override
the DDS. Compile staged ASCII models with `tools/CompileBlasterModels.py` and the
installed NWN:EE executable. It accepts `wbwxl` resrefs as well as pistol resrefs.
Keep `CompileModels: false` when packing HAKs. Validate native-reader geometry,
normals, material bindings, texture dependencies and compiled triangle corners.
Do not make validation depend on local source manifests.

Run both converter test files under Blender when modifying shared conversion code.
For client acceptance, restart NWN and check inventory/ground appearance, rifle idle
and attack poses, muzzle direction, and different body sizes. Offline previews alone
do not establish live fit. Resource-only replacements require a weapon HAK rebuild;
they do not require a module repack.

`tools/RenderRifleGrip.py` reads the rifle pose from an ASCII `a_ba_med_weap.mdl`
reference. Supply `--source <review.blend>`,
`--hand <ASCII pmh0_handr001.mdl>`, `--animation <ASCII a_ba_med_weap.mdl>` and
`--output <local review folder>`. For fitting rifles, also supply
`--skeleton <ASCII pmh0.mdl>` and `--left-hand <ASCII pmh0_handl001.mdl>`.
This composes the complete arm hierarchy and shows both hands from both sides.
Skeleton/animation node names must be matched case-insensitively: the skeleton's
`Lbicep_g` and animation's `lbicep_g` are the same bone. Skipping this ancestor
places the support hand incorrectly even when the right hand appears plausible.
Use `--pose xbowrdy` for the holding pose or `--pose xbowshot` for the initial
attack frame. Moving attack frames and body clearance still need client review.

Review every replacement separately. A shared attachment-axis correction does not
establish a good fit for different fore-end thicknesses, stocks and grips. Small
vertical seating adjustments and pitch around the grip can close support-hand gaps;
preserve uniform scale and inspect trigger contact after each adjustment. Record
per-model fits locally and apply changes to the existing compiled node transforms
where possible, preserving the verified geometry, UVs, normals and tangents.
