---
name: swlor-blaster-models
description: Import, create, replace, or refit SWLOR pistol blaster models through local authoring, NWN model and texture export, inventory icons, appearance catalog registration, and HAK deployment verification. Use for GR2 blaster imports, original blaster designs, or weapon grip, scale, and muzzle alignment changes.
---

# SWLOR Blaster Models

Use the [model workflow](../../../SWLOR.Game.Server/Readmes/BlasterModelImport.md)
for commands, a local import-manifest example, material rules and fitting details.
Inspect the parent checkout and HAK submodule used for testing; worktree changes
do not automatically reach another checkout's build inputs.

## Repository contents

Commit only each weapon's finished MDL, DDS textures, MTR and DDS inventory icon.
Keep imported GR2/DDS inputs, manifests, Blender files, per-model authoring scripts,
fit data, previews and reports in ignored local staging such as `.tmp/blaster/`.
Do not add them to Git for reproducibility or make validation/builds depend on them.
The finished MDL and maps are the starting point for later edits. Retain reusable
conversion, fitting and export helpers in the process tooling.

## Choose and author

- Resolve the requested slot from the catalog, assets and blueprint users. A
  shared slot replacement changes persisted items too. Ask only if intent cannot
  be inferred. Choose another modern pistol for a new model unless asked to revise
  an existing replacement.
- Pistol base item is **11**, class **WBwSh**. Native slot is `model * 10 + color`;
  toolset label is `model-color`; catalog ID is `color * 100 + model`. Imported
  blaster_high41_a01 occupies **44 / 4-4 / Part #404**; Vesper-9 occupies
  **201 / 20-1 / Part #120**. These are not free slots.
- For rigid opaque GR2 imports, use `tools/SetupBlasterImporter.ps1` and
  `tools/ImportBlasterModel.py` with local inputs and a local manifest. Use the
  guide's schema. Other material/animation requirements need explicit handling.
- For original designs, author a distinct silhouette and material treatment in
  a local scene/script. Reuse `write_mdl` from the converter for static export.
  For later edits, work from the committed MDL, MTR and DDS maps; do not require
  the original import files or procedural builder.

## Fit and review

- Fit the grip with uniform scale and preserve natural proportions. Keep approved
  scale for position/pitch changes and approved orientation for size changes.
  Rotate around the fitted grip so hand contact remains fixed. Do not reapply raw
  GR2 transforms to an already fitted MDL.
- Use `tools/RenderBlasterGrip.py` with a local review scene and the actual human
  right hand. Custom pistols use **bowshot**; **xbowshot** rotates the hook
  differently. Existing fit measurements are examples, not universal presets.
- Stage exports in a fresh ignored folder. Review textured views, both hand views
  and the transparent 64x64 icon. Check trigger contact, barrel direction, UV seams
  and material maps. Copy only reviewed game resources to `SWLOR_Haks/sw_weapon/`.
  Use unique meaningful texture resrefs within the 16-character limit.
- Use NWN Crunch EE to compress large maps to DDS before copying, following the
  guide's channel-specific presets, handheld texture budgets and vertical flip;
  validate dimensions, mipmaps, decoded orientation and material quality. Replace
  the corresponding TGA rather than shipping duplicate formats. Use lossless RGBA
  DDS (`-A8R8G8B8 -mipMode None`) for small inventory icons, preserving their
  alpha and pixels exactly; check toolset decoding and icon composition. Compression tools and intermediate images stay local.

## Compile and deliver

- Ship compiled MDLs using `tools/CompileBlasterModels.py` and the installed NWN:EE
  executable. Keep ASCII exports and compile logs local. The old HAK compiler can
  discard EE fields; leave `CompileModels: false` and pack the precompiled assets.
  See the guide for the command and binary/material verification. For later edits,
  read the shipped binary with `MdlReader` into local authoring rather than relying
  on a legacy decompiler to preserve EE materials.


- Check the native slot against `PistolAppearanceDefinition.MiddleParts`, the
  model and inventory icon. Validate new geometry/export changes using the native
  model reader, and check all MTR/texture references directly. Run focused converter
  tests when changing the converter. Do not require source manifests for asset checks.
- For requested deployment, use the normal build route and compare packed content
  with committed game resources. Verification builds use `-p:RunPostBuildEvent=Never`.
  See [troubleshooting](references/troubleshooting.md) for content/editor failures.
- Report slot IDs, checks, deployment and outstanding client review in the PR.
  Offline renders do not establish live fit: check idle/attack grip, muzzle direction,
  body sizes, inventory and ground appearance after restarting NWN to clear its cache.
  Follow companion HAK PR rules when publishing; keep PR review artifacts local.
