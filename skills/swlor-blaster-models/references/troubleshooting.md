# Blaster integration troubleshooting

Read this when the model or appearance editor differs from the staged result.
Paths below are relative to the parent repository root.

## Changes missing in the client

1. Check the active branch/worktree and the checkout from which the normal build
   ran. Inspect `SWLOR.Game.Server/SWLOR.Game.Server.csproj`, `SWLOR.CLI/Program.cs`,
   and `SWLOR.CLI/DeployBuild.cs` if the build/deploy connection is in doubt.
   The post-build hook passes the initiating output through `--server-output`;
   standalone CLI `-o` uses the Release output produced by `RunCLI.cmd`.
2. Check source assets and `Build/hakbuilder.json`. `sw_weapon` uses
   `CompileModels: false` because the legacy MDL compiler can discard NWN:EE
   material fields. Ship precompiled MDLs produced by the installed NWN:EE
   `compilemodel` command; the helper and verification steps are in the guide.
   An ASCII header in a shipped replacement means the client must compile it
   on first appearance, which can cause a hitch.
3. Verify `debugserver/hak/sw_weapon.hak` from the testing checkout, then the HAK
   actually mounted by the server and loaded by the client. Read client content
   paths from its configuration instead of assuming the default Documents folder.
   Search higher-priority HAKs, module resources, and override for duplicate resrefs.
4. Extract the specific model, icon, MTR and maps with `nwn_erf.exe` into a fresh
   scratch directory; compare their SHA-256 hashes with the committed
   `SWLOR_Haks/sw_weapon/` inputs, or the staging report for a new export.
   A successful build message alone does not prove the right assets were deployed.
5. Restart NWN to clear cached HAK models/textures. Reload toolset catalogs as
   needed. A resource-only slot replacement does not require a module repack;
   saved blueprint changes do. Restart only the intended local test server when
   needed and covered by the current request, not unrelated services.

The repository's normal Windows server build rebuilds changed HAK inputs and
deploys them. `-p:RunPostBuildEvent=Never` deliberately skips this. Repair a broken
normal build path instead of relying on permanent manual copies to hide it.

## First-view lag or sustained rendering cost

Distinguish a cold first appearance from continued slow rendering. Restart the
client for the cold check, then show the same weapon again in the same scene.
Record the model, lighting/body setup and before/after frame times if measuring.

- A cold hitch with shipped ASCII models calls for native NWN:EE precompilation.
  Inspect the MDL extracted from the actual loaded HAK, not only a staging copy.
  Binary MDLs start with four zero bytes; check EE material names and referenced
  vertex tangent frames as well as the header. Leave the legacy build compiler off.
- Large maps and missing mipmaps increase texture load/memory cost. Use the guide's
  DDS presets and per-map sizes; preserve smaller sources and use tiny maps for
  constant material values. Remove superseded maps and competing TGA resources.
- Continued slow rendering needs mesh/triangle, material/draw-call and texture
  cost inspection. Reduce wasted faces or maps without sacrificing the accepted
  silhouette. Compilation does not lower visible triangle count or guarantee FPS.

The latest optimization compiled all 17 shipped blasters and reduced the earlier
batch's texture payload by 8,607,008 bytes: Lifeday22 color and MTX19 color/normal
maps went from 2048 to 1024; constant MTX36 fallback maps went from 256 to 4.
Native-reader, material/tangent, DDS/icon and packed-HAK checks passed. These prove
asset delivery and integrity; live-client frame-time improvement still requires
retesting. Do not report compiler process wall time, which includes game startup,
as a measured first-view hitch or FPS improvement.

## Model missing from appearance choices

`SWLOR.Game.Server/Feature/AppearanceDefinition/ItemAppearance/PistolAppearanceDefinition.cs`
uses `color * 100 + model`, while resource suffixes use `model * 10 + color`.
Examples: native 44 -> 404; native 201 -> 120. Add the matching middle catalog
entry if absent. Do not change gameplay base type or overwrite a different slot
to make the model show up. Verify the catalog ID and the committed model/icon directly.

## Weapon changes only after unequipping

Inspect `Feature/AppearanceDefinition/ItemAppearance/EquippedItemAppearance.cs`
under `SWLOR.Game.Server`. Existing refresh support invalidates the relevant
client appearance state and uses delayed hand/quickbar refresh. Keep appearance
editor callers routed through this service; do not introduce actual unequip/equip
actions as a rendering workaround, which can trigger gameplay events.

## Blank weapon tint area or skin turning black

These are UI/binding problems, not asset geometry. Follow the repository GUI skill
if changing the NUI layout or view model. Inspect `AppearanceEditorDefinition.cs`
and `ViewModel/AppearanceEditorViewModel.cs` under `Feature/GuiDefinition`:

- Weapon mode omits unsupported tint controls and their reserved space; preserve
  the armor/body tint layout when changing weapon mode.
- Initial picker binding values must not mutate character skin. `_tintPickerActive`
  gates picker writes on deliberate interaction. Initialization and programmatic
  color changes reset pending state, and mouse-up flushes the intended user color.
- Focused coverage lives in `AppearanceEditorLayoutTests`,
  `AppearanceEditorEngineTests`, and `EquippedItemAppearanceEngineTests`. Exercise
  first-open and weapon/body tab switching if touching these paths.

Ordinary model imports should not require changing any of these shared UI paths.
