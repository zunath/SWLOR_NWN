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
   material fields. Preserve exported ASCII MDL.
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
