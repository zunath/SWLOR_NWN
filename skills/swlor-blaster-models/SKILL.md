---
name: swlor-blaster-models
description: Import, create, replace, split, or refit SWLOR weapon models, including pistol blasters, knives, longswords, greatswords, staffs, spears, twinblades, electroblades, shuriken and large shields. Covers local authoring, native NWN parts, textures, inventory icons, appearance catalogs and HAK verification. Use for weapon or shield appearance replacements, GR2 imports, original designs, or grip, scale, seam and muzzle alignment changes.
---

# SWLOR Weapon and Shield Models

Use the model workflow at `SWLOR.Game.Server/Readmes/BlasterModelImport.md` (from the repository root)
for commands, a local import-manifest example, material rules and fitting details.
Inspect the parent checkout and HAK submodule used for testing; worktree changes
do not automatically reach another checkout's build inputs.

## Repository contents

Commit only each weapon or shield's finished MDL, DDS textures, MTR and DDS inventory icon.
Keep inventory artwork in the DDS. An icon with no TGA in any layer also gets the
8x8 TGA registration stub from `tools/FixWeaponInventoryIcons.py stubs`; stock
clients then draw the DDS (see the guide's inventory lookup section).
Keep imported GR2/DDS inputs, manifests, Blender files, per-model authoring scripts,
fit data, previews and reports in ignored local staging such as `.tmp/blaster/`.
Do not add them to Git for reproducibility or make validation/builds depend on them.
Delete each intermediate as soon as no later step needs it, and empty the staging
folder once the finished resources are committed; do not wait to be asked.
The finished MDL and maps are the starting point for later edits. Retain reusable
conversion, fitting and export helpers in the process tooling.

## Choose and author

For local image-to-GLB generation, follow the guide's [TRELLIS.2 workflow](../../SWLOR.Game.Server/Readmes/BlasterModelImport.md#generate-local-weapon-sources-with-trellis2); preflight the local runtime and keep all per-model sources in ignored staging.

- Resolve the requested slot from the catalog, assets and blueprint users. A
  shared slot replacement changes persisted items too. Ask only if intent cannot
  be inferred. Choose another modern pistol for a new model unless asked to revise
  an existing replacement.
- Pistol base item is **11**, class **WBwSh**. Native slot is `model * 10 + color`;
  toolset label is `model-color`; catalog ID is `color * 100 + model`. Imported
  Inspect current assets/catalogs and open asset PRs for occupied slots; these
  examples are not the complete replacement list. blaster_high41_a01 occupies **44 / 4-4 / Part #404**; Vesper-9 occupies
  **201 / 20-1 / Part #120**. These are not free slots.
- For rigid opaque GR2 imports, use `tools/SetupBlasterImporter.ps1` and
  `tools/ImportBlasterModel.py` with local inputs and a local manifest. Use the
  guide's schema. Classify the mesh itself, not shared texture filenames; rifles
  need their own workflow. Inspect all material subsets and handle source emission
  explicitly as described in the guide. Unpack SWTOR normal green exactly once.
- For original designs, author a distinct silhouette and material treatment in
  a local scene/script. Reuse `write_mdl` from the converter for static export.
  For later edits, work from the committed MDL, MTR and DDS maps; do not require
  the original import files or procedural builder.

## Composite melee parts

Composite melee replacements must contain usable bottom, middle and top models.
A whole weapon in one slot with empty companion MDLs does not preserve the
appearance editor's independent part choices. Preserve the native IDs, audit
shared slots, cap or cover cuts with common connectors, and check every new
part combination against the same joint envelopes. Verify mixed assemblies
in the NWN client using SWLOR assets. Native simple-model items such as shuriken
and large shields keep their single-model format.

Audit the full PR's changed MDLs against the selectable appearance catalogs,
including unchanged old parts. Blueprint usage and new-only join checks do not
establish backward compatibility. Component roles vary: stock staff 01x-04x use
bottom=center grip, middle=both end bands, top=both terminal tips; stock 05x uses
a different lower/center/upper split. Match the intended legacy convention before
cutting, and record incompatible whole-weapon, blank or shifted legacy slots.
Axial coverage is a failure screen, not proof of three-dimensional fit.

## Large shields

Use base item **56**, simple family **AShLw**, model `ashlw_###.mdl` and
matching `iashlw_###.dds` artwork. Audit the selectable model resources rather
than relying on blueprint usage. Keep appearance IDs and persisted instances.
Each shield exports one complete model; clipped pieces and empty shield models
are rejected. Supply both `imp_shield_1` and `imp_shield_2` using
`attachment_dummies` with measured root-space positions and unit XYZW
quaternions. Preserve these impact-effect hooks separately from the grip.
Use the exact **64-by-128** transparent icon canvas (64-by-96 logical artwork)
and DDS replacement icons, plus the 8x8 TGA registration stub when no TGA of that
name exists in any layer. Inspect the actual SWLOR left hand, rear grip,
forearm/body clearance, attack poses, ground model and native inventory icon.
Follow the guide's shield fit and export schema.

## Fit and review

- Fit the grip with uniform scale and preserve natural proportions. Keep approved
  scale for position/pitch changes and approved orientation for size changes.
  Rotate around the fitted grip so hand contact remains fixed. Do not reapply raw
  GR2 transforms to an already fitted MDL.
- Use `tools/RenderBlasterGrip.py` with a local review scene and the actual human
  right hand. Custom pistols use **bowshot**; **xbowshot** rotates the hook
  differently. Existing fit measurements are examples, not universal presets.
- Stage exports in a fresh ignored folder. Review textured views, both hand views
  and the transparent icon on its native canvas. Check hand contact, orientation, UV seams
  and material maps. Copy only reviewed game resources to `SWLOR_Haks/sw_weapon/`.
  Use unique meaningful texture resrefs within the 16-character limit.
- Use NWN Crunch EE to compress large maps to DDS before copying, following the
  guide's channel-specific presets, handheld texture budgets and vertical flip;
  validate dimensions, mipmaps, decoded orientation and material quality. Replace
  the corresponding TGA rather than shipping duplicate formats. Use DXT5 DDS
  (`-DXT5 -dxtQuality uber -gamma 2.2 -mipMode None`) for native inventory icons
  to preserve transparency. Do not use uncompressed `-A8R8G8B8`: the native DDS
  loader does not recognize it, even though generic DDS decoders do. Review
  the compressed icon itself. Compression tools and intermediate images stay local.

## Compile and deliver

- Ship compiled MDLs using `tools/CompileBlasterModels.py` and CleanModels v4.4.2
  bundled with cleanmodels-qt v1.1.7. Install it with
  `SWLOR_Haks/tools/SetupModelCompiler.ps1`; use `CLEANMODELS` or `--compiler` for
  a different install path. Never compile with `nwnmdlcomp.exe` or the NWN game.
  Keep ASCII exports and compile logs local; pack the precompiled assets.
  See the guide for the command and binary/material verification. For later edits,
  read the shipped binary with `MdlReader` into local authoring rather than relying
  on a legacy decompiler to preserve EE materials.
- Check the native slot against its appearance catalog, the
  model and inventory icon. Each appearance needs unique artwork matching its
  finished model, including replacements of occupied slots. For pistols, run
  `python -m unittest discover -s tools/tests -p test_pistol_inventory_icons.py`
  to audit every catalog slot for missing, blank or duplicate artwork, DXT5 encoding, DDS/TGA
  shadows and end layers that obscure the middle icon. Review a contact sheet
  and provide an icon preview at handoff. Validate new geometry/export changes using the native
  model reader, and check all MTR/texture references directly. Run focused converter
  tests when changing the converter. Do not require source manifests for asset checks.
- For requested deployment, use the normal build route and compare packed content
  with committed game resources. Verification builds use `-p:RunPostBuildEvent=Never`.
  See [troubleshooting](references/troubleshooting.md) for first-view hitches,
  sustained rendering cost and content/editor failures. Compilation and offline
  checks are not a measured client performance result.
- Report slot IDs, checks, deployment and outstanding client review in the PR.
  Offline renders do not establish live fit: check idle/attack grip, muzzle direction,
  body sizes, inventory and ground appearance after restarting NWN to clear its cache.
  A decoded DDS, successful toolset preview or matching packed hash does not
  establish that the native inventory selects that icon. Test switching between
  distinct appearances (including the starting weapon) in a freshly restarted
  client. If the model changes but its icon stays the same, inspect native icon
  discovery and resource precedence; do not regenerate already-correct artwork
  or claim the issue fixed from offline checks. See the guide's inventory lookup
  section. Stock 8193.37-17 checks TGA presence for composite and shield icons,
  so a DDS icon with no TGA in any layer needs the registration stub; with it the
  client draws the DDS. Client patching is a separate opt-in; do not silently
  modify an installed executable during model generation.
  Report unresolved native lookup failures explicitly.
  Follow companion HAK PR rules when publishing; keep PR review artifacts local.
