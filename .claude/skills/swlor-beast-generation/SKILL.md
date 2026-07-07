---
name: swlor-beast-generation
description: Add or modify SWLOR beast data from design Bible/source TSV through generated C# definitions, BeastType enum, DNA subtype/TLK assets, haks submodule files, and validation. Use when adding a new tamable or incubation beast, adding a beast mutation path, choosing model/portrait/sound IDs, updating Beast Levels/Bible docs, updating DNA subtype labels, regenerating beast definitions, or repairing SWLOR beast generator output in C:\Projects\SWLOR_NWN.
---

# SWLOR Beast Generation

## Core Rule

Treat beast data as a source-of-truth chain, not as isolated C# edits. Keep the Bible workbook, TSV export, generator, generated server definitions, enum IDs, DNA 2DA, TLK label, and validation in sync.

When the request is underspecified, resolve one blocker at a time. Ask only questions the repo cannot answer, recommend an answer, and inspect existing beasts before asking. Do not replace existing mutation outcomes unless the user explicitly says to replace one. DNA subtype player-facing labels are mandatory; never leave a beast with missing or bad player-visible data.

## Important Files

- Source TSV: `SWLOR.CLI/InputFiles/beast_levels.tsv`
- Bible workbook: `design/bible/SWLOR Design Bible - Combat Upgrade.xlsx`
- Beast workbook sheets: `Beast Levels`, `Beast Lookups`, `Beast Calcs`
- Generator: `SWLOR.CLI/BeastCodeBuilder.cs`
- Templates: `SWLOR.CLI/Templates/beast_builder_template.txt`, `SWLOR.CLI/Templates/beast_level_template.txt`
- Generated scratch output: `SWLOR.CLI/OutputBeasts/...`
- Server definitions: `SWLOR.Game.Server/Feature/BeastDefinition/TamableBeastDefinition` and `IncubationBeastDefinition`
- Beast enum: `SWLOR.Game.Server/Service/BeastMasteryService/BeastType.cs`
- Haks submodule: `SWLOR_Haks`
- Model/portrait/sound lookups: `SWLOR_Haks/swlor2_2da/appearance.2da`, `portraits.2da`, `soundset.2da`
- DNA labels: `SWLOR_Haks/swlor2_2da/iprp_dnatype.2da`
- TLK source/binary: `SWLOR_Haks/swlor2_tlk/swlor2_tlk.tlk.json`, `swlor2_tlk.tlk`

## Workflow

1. Normalize the spec.
   - Capture: beast name, beast enum, tamable or incubation-only, role/class, appearance model, portrait ID, soundset ID, scale, mutation parent, mutation weight, enzyme colors/counts, mutation days, and stat baseline.
   - If the user gives a model display name, verify its appearance enum or row from `appearance.2da` before coding.
   - For portrait and soundset, verify matching IDs in `portraits.2da` and `soundset.2da`.
   - If adding an incubation mutation, decide whether to add another mutation slot or replace an existing one. Default to adding only when the user rejects replacement or existing mutations must be preserved.

2. Inspect current beast patterns.
   - Read a nearby tamable source beast and a nearby incubation beast with the same role.
   - Compare enum ID ranges in `BeastType.cs`; append a stable new ID after the current highest beast ID unless the user gives an ID.
   - Confirm the generator output namespace and APIs match current server code before trusting generated files.

3. Update the TSV.
   - Preserve the first four header rows and existing column ordering.
   - If a third mutation is needed, append columns 60-68: `Mutation 3`, `Mutation 3 Weight`, `Mutation 3 Lyase Color`, `Mutation 3 Isomerase Color`, `Mutation 3 Hydrolase Color`, `Mutation 3 Lyase Count`, `Mutation 3 Isomerase Count`, `Mutation 3 Hydrolase Count`, `Mutation 3 Days`.
   - Add one row per beast level, usually 50 rows. Clone the selected stat baseline, then change only identity/model/sound/portrait/role/acquisition fields required by the spec.
   - For incubation-only beasts, set `Incubation?` to `Y` and place the generated definition under `IncubationBeastDefinition`.
   - Add the parent beast's mutation data in the appropriate mutation block. Leave mutation days blank unless explicitly restricted.

4. Update the generator when the TSV shape changes.
   - Support optional extra mutation blocks without breaking existing two-mutation rows.
   - Emit namespaces matching the server folders: `SWLOR.Game.Server.Feature.BeastDefinition.TamableBeastDefinition` or `.IncubationBeastDefinition`.
   - Use current builder APIs: physical and force use `.MaxDefenseBonus(CombatDamageType.Physical|Force, value)`; elemental/status values use `.MaxResistanceBonus(ResistanceType.Fire|Poison|Electrical|Ice|Mind|Trauma|Mobility, value)`.
   - Do not emit `.MaxSavingThrowBonus(...)`; the current `BeastBuilder` does not expose that method.

5. Generate and copy server definitions.
   - Build the CLI if needed with `dotnet build --no-restore -p:RunPostBuildEvent=Never` from `SWLOR.CLI`.
   - Run `dotnet run --no-build -- -b` from `SWLOR.CLI`.
   - Inspect generated output before copying. Verify name, enum, appearance, soundset, portrait, role, mutation weight, enzyme requirements, namespace, and resistance calls.
   - Copy only the changed generated definitions into `SWLOR.Game.Server/Feature/BeastDefinition/...`.
   - Remove `SWLOR.CLI/OutputBeasts` after copying, because the scratch generated files can be compiled as stray source. Before recursive removal, resolve the absolute path and confirm it stays under `C:\Projects\SWLOR_NWN`.

6. Update implementation IDs and player-facing labels.
   - Add the beast to `BeastType.cs` with the chosen ID.
   - In `SWLOR_Haks`, add a DNA subtype row to `swlor2_2da/iprp_dnatype.2da`.
   - Use a pre-existing empty TLK gap for new labels. Custom strref is `16777216 + tlkId`.
   - Add the TLK JSON entry to `swlor2_tlk.tlk.json` and regenerate `swlor2_tlk.tlk`. Preserve the existing TLK table size and Windows-1252 text encoding if writing the binary directly.
   - Remember `SWLOR_Haks` is a git submodule; check its status from inside `SWLOR_Haks`.

7. Update the Bible workbook.
   - Update `Beast Levels` to match the TSV shape and rows.
   - Update `Beast Lookups` with the new beast row and any new mutation columns/data.
   - Update `Beast Calcs` starting, ending, and delta bands if the beast appears in calculated progression tables. Extend lookup ranges so formulas include the new lookup row.
   - After editing `design/bible/SWLOR Design Bible - Combat Upgrade.xlsx`, run `powershell -ExecutionPolicy Bypass -File tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible` from the repo root.

8. Validate.
   - Run a data check that verifies: TSV width and row count, new beast rows, parent mutation data, server definition tokens, workbook rows, TLK slot text, and DNA row.
   - Run `dotnet build --no-restore -p:RunPostBuildEvent=Never` from the repo root.
   - Run focused Beastmaster/Bible tests when relevant: `dotnet test --no-build --filter "FullyQualifiedName~CombatUpgradeBibleSyncTests|FullyQualifiedName~BeastmasterCombatUpgradeTests"`.
   - Run `git status --short --untracked-files=all` in the parent repo and in `SWLOR_Haks`.
   - `git diff --check` may flag expected trailing tabs/padding in TSV and 2DA files. Do not strip fixed-format padding blindly.

## Handoff Notes

Report both parent repo changes and `SWLOR_Haks` submodule changes. Mention any build warnings separately from errors. Do not stage, commit, or revert unrelated dirty worktree files unless the user asks.
