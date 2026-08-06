# SWLOR Toolset — SWLOR-Only Aurora Toolset Replacement

> **Historical plan.** This records the original toolset implementation. Its Radoub dependency
> and licensing guidance were superseded by `RADOUB-REPLACEMENT-PLAN.md`.

> Canonical in-repo copy of the approved execution plan. Progress and recorded decisions
> live in the companion `WORKLOG.md` (same folder) — on session start, read this file, then
> WORKLOG.md, then recent git history to recover state. Format-spec corrections discovered
> during execution are folded into the "ground rules" section below and take precedence over
> anything older.

## Context

The team opens the legacy Aurora Toolset only for area work, instance placement, and blueprint editing — everything else (dialogs, quests, spawns, scripts, stores) already lives in C#. The module source of truth is diffable per-resource JSON (`Module/{are,git,gic,utc,uti,utp,...}/<resref>.<ext>.json` in neverwinter.nim `nwn_gff` format), packed via `tools\SWLOR.CLI\RunCLI.cmd -p`. This plan adds a modern Avalonia desktop editor to the solution that reads/writes that JSON directly, reuses the existing pack pipeline unchanged, and understands SWLOR's C# content conventions.

**Foundation (user-directed):** [Radoub](https://github.com/LordOfMyatar/Radoub) — GPL-3.0, C#/.NET 9, Avalonia, active. Its `Radoub.Formats` library has binary parsers for GFF/ERF/KEY/BIF/TLK/2DA/MDL/MTR/PLT/DDS/TGA/SSF + typed wrappers + resource resolver; Quartermaster/Reliquary prove Avalonia+OpenGL MDL preview. Not on NuGet → pinned git submodule. Gaps we build: JSON-GFF serialization, GIT typed wrapper, SET parser, WOK, the area editor itself.

## Locked decisions (do not re-litigate in work packages)

- Projects: `SWLOR.Toolset` (Avalonia WinExe), `SWLOR.Toolset.Domain` (headless lib — all logic lives here), `SWLOR.Toolset.Tests` (NUnit). Submodule at `External\Radoub` pinned to a release tag.
- References: `Toolset → Domain + Radoub.UI`; `Domain → Radoub.Formats + SWLOR.Game.Server`. **No existing project may ever reference SWLOR.Toolset.\*** (GPL boundary — the server stays unencumbered).
- Target the **lowest .NET version that satisfies all references** — floor = Radoub.Formats' own TargetFramework (expected `net9.0`; verified in WP0.1). `PlatformTarget=x64`, `Nullable=enable`, `ImplicitUsings=enable`. A net9 exe may reference the net8 SWLOR.Game.Server.
- UI: Avalonia + CommunityToolkit.Mvvm; docking via Dock.Avalonia unless the WP4.1 spike finds a better Radoub pattern.
- Viewport: OpenGL, copying whatever control/shader approach Radoub Quartermaster uses.
- Editor saves must produce **zero spurious git diff** — lexical preservation, not re-serialization.
- Naming: domain terms only; no initiative labels (AGENTS.md). PascalCase types, `_camelCase` private fields, `var`-heavy.

---

## Ground rules for ALL executing agents

Read these before any work package. They are the shared contract.

### Repo facts
- Solution: `D:\source\repos\SWLOR_NWN\SWLOR.Game.Server.sln` (8 existing projects, net8.0 x64, no Directory.Build.props/.editorconfig — each csproj is self-contained).
- Module JSON source: `Module\{are,dlg,fac,gic,git,ifo,itp,jrl,utc,utd,uti,utm,utp,uts,utt,utw}\*.json` (~17,900 files; 438 are/git/gic triplets, 897 utc, 6597 uti, 8341 utp). `Module\nss`,`Module\ncs` are raw scripts, not JSON.
- Haks: `SWLOR_Haks\` (git submodule): `sw_2da\*.2da` (747 raw V2.0 text), `sw_tlk\sw_tlk.tlk.json`, ~90 tileset haks `sw_t_*` each with `.set/.mdl/.wok/.tga/.dds/.txi/.itp`. Hak list/order: `Build\hakbuilder.json`.
- Pack pipeline source: `SWLOR.CLI\ModulePacker.cs` (shells to `nwn_gff.exe`/`nwn_erf.exe`; `GetModuleFolders()` is the authoritative resource-type list). Prior art for blueprint↔instance mapping/validation: `SWLOR.CLI\StoreInstanceSync.cs`.
- Test conventions: mirror `SWLOR.Game.Server.Tests\SWLOR.Game.Server.Tests.csproj` (NUnit 4, NUnit3TestAdapter, Microsoft.NET.Test.Sdk, FluentAssertions 7).

### The nwn_gff JSON format (spec — verified against corpus, corrected during WP1.1)
- Root object: `"__data_type": "<4-char type incl. trailing space, e.g. 'ARE '>"` first, then fields.
- Every field: `"FieldName": { "type": "<gfftype>", "value": <v> }`. GFF types: `byte, char, word, short, dword, int, dword64, int64, float, double, cexostring, resref, cexolocstring, void, struct, list`.
- `cexolocstring`: the strref `"id"` sits at the FIELD level — `{ "id": N, "type": "cexolocstring", "value": { "0": "text", ... } }`; value holds language-keyed entries only.
- `void`: raw binary bytes embedded directly in the JSON string token (NOT base64), including invalid UTF-8 — all handling must be byte-level, never through .NET strings.
- `list`: array of struct objects (each with `__struct_id` first). `struct` field: `__struct_id` at field level AND inside the value object (both, kept in sync).
- Formatting: 2-space indent; key order is CASE-INSENSITIVE ASCII (Nim `cmpIgnoreCase`; e.g. `fortbonus` < `LawfulChaotic`, `Tile_List` < `Tileset`). `JsonGffStruct.Add` implements it; parsed order is preserved verbatim.
- Floats: C `%.16g` with MSVCRT semantics (exact expansion → 17 sig digits → 16, both roundings half away from zero; 3-digit zero-padded exponents `e-039`), then Nim appends `.0` when no '.'/'e'. Implemented in `NimFloatFormatter`; conformance-tested over every corpus literal THROUGH THE FLOAT32 FUNNEL (text → double → float32 → double → format), since GFF floats are 32-bit.
- Preserve each file's EOL style and trailing-newline state on save (`.gitattributes` is `text=auto`; working tree is CRLF).
- The implemented model lives in `SWLOR.Toolset.Domain\Gff\` (byte-level tokenizer, raw-token preservation); the permanent gate is `SWLOR.Toolset.Tests\RoundTripCorpusTests.cs`.

### Commands
- Build: `dotnet build SWLOR.Game.Server.sln` (from repo root).
- Test: `dotnet test SWLOR.Toolset.Tests --filter <FullyQualifiedName~Pattern>`.
- Pack (only when a refreshed .mod is needed): `Module\PackModule.cmd`.
- Never start background processes/watchers (AGENTS.md). Never edit `.claude\skills\` (mirror only).

### Definition of done for EVERY work package
1. `dotnet build SWLOR.Game.Server.sln` green.
2. All existing + new tests green (`dotnet test SWLOR.Toolset.Tests`; run the full round-trip suite from WP1.3 onward — it must stay green forever).
3. No diff outside the files the package names.
4. Code matches repo naming/style conventions above.

---

## Pre-execution checklist (before WP0.1)

1. **Branch:** create and work on `feature/swlor-toolset` (user decision). All toolset work lands there.
2. **.NET version (in WP0.1, first step):** use the **lowest** version that satisfies all references — floor is set by Radoub.Formats' own target (currently .NET 9). Verify `External\Radoub\...\Radoub.Formats.csproj`'s actual `TargetFramework` after adding the submodule, and set the three toolset projects to exactly that version (expected `net9.0`; a net9 project may reference the net8 SWLOR.Game.Server). Only go higher if the Radoub submodule itself requires it. Verify the SDK is installed (`dotnet --list-sdks`) and record the chosen version in the WP0.1 summary.
3. **Radoub.UI (in WP0.1):** verify `Radoub.UI` is a cleanly consumable csproj worth referencing; if it's app-coupled, drop the reference and take only patterns/styles from it (Radoub.Formats is the load-bearing reuse). Record the decision.
4. **CI:** none exists for this project — no pipeline work needed.

## Orchestration protocol (for the controlling agent)

The controller (lead agent) owns sequencing, dispatch, verification, and state. Subagents own only their assigned package.

### Durable state — the worklog
- WP0.1 creates `SWLOR.Toolset\WORKLOG.md` (committed with the code). One entry per work package:
  `## WP<id> — <status: pending|in-progress|done|blocked> — <date>` followed by: executor tier used, files touched, tests added, **decisions recorded** (e.g., chosen .NET version, Radoub.UI in/out, GIT list key spellings found), and any deviations from this plan with rationale.
- The worklog is the single source of truth for progress. On session resume (or context loss), the controller re-derives state from: this plan file → `WORKLOG.md` → `git log --oneline -20` + `git status` on `feature/swlor-toolset`. Never rely on conversation memory.
- Decisions that later packages depend on (WP0.1 .NET version, WP2.5 reflection-vs-Roslyn, WP4.1 GL approach) MUST be in the worklog — later subagent briefs quote them from there.

### Dependency graph / parallelism
- Strict order: WP0.1 → Phase 1 (1.1 → {1.2, 1.3} → 1.4 → 1.5; 1.6 after 1.1) → everything else.
- WP0.2 is independent (any time after WP0.1; needed before WP3.5).
- After Phase 1: Phase 2 packages 2.1/2.2/2.3 are parallel-safe (disjoint files); 2.4 after 2.1+2.3; 2.5 independent; 2.6 last in phase.
- Phase 3: 3.1 first (it's the stamp-pattern), then 3.2/3.3/3.4 parallel-safe, 3.5 any time, 3.6 last.
- Phases 4→5→6→7 sequential; within Phase 4: 4.1 → 4.2 → {4.3, 4.4} → 4.5.
- Parallel dispatch is allowed only for packages marked parallel-safe AND touching disjoint files.

### Dispatching a subagent — brief template
Every subagent brief must contain, verbatim or by concrete file reference (never "as discussed"):
1. The **Ground rules for ALL executing agents** section of this plan (copy it in).
2. The full text of the assigned WP (goal, inputs, deliverables, acceptance).
3. Relevant recorded decisions quoted from `WORKLOG.md`.
4. Hard constraints: touch ONLY the files/dirs the WP names; do not modify locked decisions, other projects, `.claude\skills\`, or the round-trip harness; if any input file is missing or an instruction is ambiguous or acceptance can't be met — STOP and report the blocker instead of improvising; never weaken/skip a failing test to get green.
5. Required report format: files changed (paths), tests added + full `dotnet test` result output, decisions made, deviations, open questions.

### Verifying a subagent's work — controller MUST, before marking a WP done
1. Run `dotnet build SWLOR.Game.Server.sln` and `dotnet test SWLOR.Toolset.Tests` itself — never trust the report alone.
2. From WP1.3 onward, confirm the round-trip corpus suite specifically is green.
3. Run `git status`/`git diff --stat` and confirm the change surface matches the WP's named files.
4. Spot-read the diff for locked-decision violations (references from existing projects to toolset, initiative labels in identifiers, style drift).
5. Write the worklog entry (or correct the subagent's draft), commit with a message naming the WP.
If verification fails: return the report + failure evidence to the same subagent for one fix cycle; after two failed cycles, escalate the package one tier and re-dispatch.

### Escalation rules
- Any byte-fidelity failure (round-trip, float conformance, edit locality) escalates to Lead regardless of which package surfaced it.
- Ambiguity about SWLOR conventions → resolve from `.codex\skills\` docs and `Readmes\`; if still ambiguous, ask the user rather than guessing.
- Tier is a ceiling on delegation, not a floor: the controller may always execute a Low/Mid package itself.

## Work packages

Each package lists: **Tier** (Low = mechanical, executable by a low-tier subagent with only this file + named inputs; Mid = needs judgment within one subsystem; Lead = architectural/empirical, keep with the lead agent), **Inputs** (files to read first), **Deliverables**, **Acceptance**.

### Phase 0 — Scaffolding

**WP0.1 — Submodule + projects + solution wiring.** Tier: **Mid** (first-build unknowns).
Inputs: `SWLOR.Game.Server.sln`, `SWLOR.Game.Server.Tests\*.csproj`, Radoub repo.
Steps: add submodule `External\Radoub` pinned to latest release tag; create the 3 projects with the locked csproj settings; add ProjectReferences per locked decisions; add to sln under solution folder "Toolset"; create `SWLOR.Toolset\LICENSE-NOTICE.md` (GPL-3.0 implications: none for internal use; public distribution of the toolset exe requires GPL source release; server projects must never reference toolset projects); create `SWLOR.Toolset\WORKLOG.md` seeded with all WP ids as `pending` plus this package's entry (recording .NET version and Radoub.UI decisions); add one placeholder NUnit test.
Acceptance: build + test green in Debug and Release; toolset→Radoub→Game.Server reference chain compiles at the chosen (lowest workable) .NET version, recorded in the summary; `git status` shows only new files + sln + `.gitmodules`.

**WP0.2 — CLI `--no-prompt` flag.** Tier: **Low**.
Inputs: `SWLOR.CLI\Program.cs`, `SWLOR.CLI\ModulePacker.cs` (`Console.ReadKey()` at ~:111 and ~:200), `SWLOR.CLI\LanguageBuilder.cs:73`.
Deliverable: a `--no-prompt` option (Microsoft.Extensions.CommandLineUtils, matching existing option style) that skips the ReadKey calls in pack/unpack; default behavior unchanged.
Acceptance: `SWLOR.CLI` builds; `-p` without the flag still prompts (code inspection); with flag, no interactive wait.

### Phase 1 — JSON-GFF core + fidelity gate (gates everything after it)

**WP1.1 — Generic model + reader/writer.** Tier: **Lead** (empirical format edge cases).
Inputs: format spec above; sample files `Module\are\bank.are.json`, `Module\git\bank.git.json`, one large file `Module\git\pw_ar_czarmrange.git.json`; Radoub `Radoub.Formats\Gff\*`.
Deliverables in `SWLOR.Toolset.Domain\Gff\`: `GffFieldType.cs`, `JsonGffField.cs` (Type, Value, `string? RawText` for numbers), `JsonGffStruct.cs` (StructId + ordered field dict), `JsonGffDocument.cs` (DataType, Root, EolStyle, TrailingNewline), `GffJsonReader.cs` (Utf8JsonReader-based, captures raw number spans), `GffJsonWriter.cs` (re-emits RawText untouched; formats new values), `NimFloatFormatter.cs`.
Acceptance: WP1.3 harness passes on a 500-file sample across every resource type (full corpus gate lands in WP1.3).

**WP1.2 — Corpus conformance utilities.** Tier: **Low**.
Inputs: WP1.1 code.
Deliverables in `SWLOR.Toolset.Tests\`: `CorpusLocator.cs` (finds repo root/Module from test context), `NimFloatFormatterTests.cs` — extract every raw `float`/`double` literal from all Module JSON (streaming, parallel), assert `Format(Parse(raw)) == raw`, report distinct failures with file+path.
Acceptance: test runs <2 min; failures (if any) enumerated, handed back to WP1.1 owner.

**WP1.3 — The round-trip gate.** Tier: **Low** (harness) + **Lead** (fixing what it finds).
Deliverables: `RoundTripCorpusTests.cs` — for every `Module\**\*.json` (16 GFF folders only): read → model → serialize to memory → byte-compare. Parallel, zero-write. Plus `EditLocalityTests.cs`: for one sample file per resource type, programmatically change one field via the model, serialize, assert the unified diff is exactly the expected changed lines.
Acceptance: **byte-identical for all ~17,900 files**; edit-locality exact; suite ≤2 min. This suite is permanent and must stay green in every later package.

**WP1.4 — Typed documents.** Tier: **Low** (pattern-stamped after the first two).
Inputs: WP1.1 model; field references: `.codex\skills\swlor-quest-generation\SKILL.md` + `references\implementation-checklist.md` (creature instance fields: TemplateResRef, Tag, Conversation, VarTable, X/Y/ZPosition, X/YOrientation), `SWLOR.CLI\Templates\*.json` (minimal blueprint shapes), `SWLOR.CLI\StoreInstanceSync.cs`.
Deliverables in `Domain\Documents\`: `AreDocument` (Tileset, Width/Height, Tile_List, lighting/fog props), `GitDocument` (typed access to "Creature List", "Placeable List", "Door List", "WaypointList", "Encounter List", "TriggerList", "SoundList", "StoreList", AreaProperties — verify exact list key spellings against corpus before coding), `GicDocument`, `UtcDocument`, `UtiDocument`, `UtpDocument`, `UtdDocument`, `UtwDocument`, `UtsDocument`, `UttDocument`, `UtmDocument`, `ItpDocument` (palette tree: MAIN list, STRREF/RESREF nodes), `IfoDocument` (Mod_Area_list), `FacDocument`; shared `VarTable.cs` (Name/Type/Value locals; semantics per Radoub `VarTableHelper`), `LocString.cs`.
Pattern: documents are **views** over `JsonGffDocument` — getters/setters address fields by path; unknown fields untouched. Build `AreDocument` + `GitDocument` first as the reference pattern; stamp the rest.
Acceptance: unit tests per document reading real corpus files and asserting known values; round-trip suite still green after typed writes (edit-locality style).

**WP1.5 — Transactions/undo/dirty.** Tier: **Mid**.
Deliverables in `Domain\Editing\`: `IDocumentEdit` (Apply/Revert/Describe), `FieldEdit`, `ListInsertEdit`/`ListRemoveEdit`/`ListMoveEdit`, `DocumentTransaction`, `UndoStack` (saved-marker dirty tracking), `DocumentSession` (path + mtime for external-change detection). All typed setters route through the ambient transaction; edits clear `RawText` on modified fields only.
Acceptance: undo/redo/dirty unit tests incl. transaction grouping and marker semantics; edit-locality still exact.

**WP1.6 — Radoub bridge.** Tier: **Low**.
Deliverables: `GffJsonBridge.cs` (JsonGffDocument ↔ Radoub `GffFile`, both directions); `GffBridgeTests.cs` — JSON→GffFile→JSON byte-identical on a ~500-file sample covering all types.
Acceptance: bridge tests green.

### Phase 2 — Game-data services + read-only browser

**WP2.1 — 2DA + TLK services.** Tier: **Low**.
Inputs: Radoub `TwoDA`; `SWLOR_Haks\sw_2da\appearance.2da` (sample), `SWLOR_Haks\sw_tlk\sw_tlk.tlk.json` (shape: `{ "language": 0, "entries": [{ "id": n, "text": "..." }] }`).
Deliverables in `Domain\GameData\`: `TwoDaService` (cached, name→table over `sw_2da\`), `TlkJsonFile.cs` + `TlkService` (custom tlk json + base `dialog.tlk` via Radoub; strref ≥ 16777216 → custom entry id = strref − 16777216).
Acceptance: tests resolve known rows (e.g. appearance.2da row labels) and known TLK strrefs both sides of the 16777216 boundary.

**WP2.2 — SET parser.** Tier: **Mid**.
Inputs: `SWLOR_Haks\sw_t_dungeon\tde01.set` (INI-style: `[GENERAL]`, `[TERRAIN TYPES]`, `[CROSSER TYPES]`, `[TILES]`/`[TILEn]` with Model/terrain corners/crossers/doors); every other `.set` under `SWLOR_Haks\sw_t_*`.
Deliverables: `SetFileParser.cs`, `TilesetDefinition.cs`, `TileDefinition.cs`.
Acceptance: `SetParserTests` parses **every** `.set` in all `sw_t_*` haks without error and asserts spot values from `tde01.set`.

**WP2.3 — Resource index.** Tier: **Mid**.
Inputs: Radoub `Key/Bif/Erf/Resolver`; `Build\hakbuilder.json` (hak order); NWN:EE install (Steam/GOG/Beamdog registry lookup + settings override).
Deliverables: `NwnInstallLocator.cs`, `KeyBifCatalog.cs`, `HakDirectoryCatalog.cs` (loose hak-source dirs), `ResourceIndex.cs` — layered lookup (base KEY/BIF lowest, haks in hakbuilder order highest), async build, mtime-keyed cache at `%LOCALAPPDATA%\SWLOR.Toolset\index.cache`, `Lookup(resref, restype) → ResourceHandle` with provenance.
Acceptance: precedence unit tests (hak overrides base); cold index build seconds not minutes; graceful "install not found" state.

**WP2.4 — Lookup services.** Tier: **Low** (stamp after first).
Deliverables in `Domain\GameData\Lookups\`: `AppearanceService` (appearance.2da + TLK name + model resref), `PortraitService`, `PlaceableAppearanceService`, `DoorTypeService`, `SoundService`, `TilesetCatalog` (enumerate tilesets from haks + base; `.set` → TilesetDefinition cache).
Acceptance: per-service tests against known 2DA rows.

**WP2.5 — Game-code index.** Tier: **Mid**.
Inputs: `SWLOR.Game.Server\Service\NPCService\NPCGroupType.cs` (`[NPCGroup]` attributes), spawn-table and quest definitions.
Deliverables: `IGameCodeIndex` + `ReflectionGameCodeIndex` (reflect over referenced SWLOR.Game.Server for NPCGroupType values, spawn table IDs, quest IDs, key items). If the server reference proves too heavy in a GUI process, fall back to `RoslynGameCodeIndex` parsing source (precedent: Tests already use Microsoft.CodeAnalysis.CSharp). Record which path was taken.
Acceptance: tests assert known enum values/quest IDs resolve.

**WP2.6 — Shell + read-only browser.** Tier: **Mid** (UI).
Inputs: `SWLOR.Admin\Program.cs` (DI pattern), Radoub.UI components, Dock.Avalonia.
Deliverables in `SWLOR.Toolset\`: `App.axaml`/`Program.cs` (DI: Microsoft.Extensions.DependencyInjection), `Settings\ToolsetSettings.cs` (module path default = repo `Module\`, NWN path, recents; persisted to `%LOCALAPPDATA%\SWLOR.Toolset\settings.json`), `Shell\MainWindow` with docked panels (Module Explorer: areas/blueprints/palettes; Properties; Output), `Workspace\ModuleWorkspace.cs` (lazy per-file load, FileSystemWatcher → reload prompt on external change), `Workspace\BlueprintCatalog.cs` (background-built virtualized index of resref/name/tag/palette-node — never full-parse-all-upfront), read-only property display with resolved 2DA/TLK names, search by resref/tag/name.
Acceptance: open module <10s cold/<2s warm; browse all areas + all blueprint types; search across 8341 utp without UI stalls; zero file writes.

### Phase 3 — Editing + validation + pack (daily driver ships here)

**WP3.1 — Editor schema infrastructure + first editor.** Tier: **Lead** (the pattern everything stamps from).
Deliverables: `Editors\Schema\{EditorSchema,FieldDescriptor,EditorKind}.cs` (declarative: label, GFF path, editor kind — text/int/float/check/2da-dropdown/tlk-locstring/resref-picker/script-slot/VarTable-grid), generic `BlueprintEditorView` + VM binding schema→controls→transactions, `Schemas\UtcSchema.cs` complete (stats, appearance dropdown, portrait, faction, scripts, inventory display, VarTable grid with known-key completion: `QUEST_NPC_GROUP_ID`, `CREATURE_SPAWN_TABLE_ID`, `CREATURE_SPAWN_COUNT`, `QUEST_ENCOUNTER_*` — values validated live against `IGameCodeIndex`).
Acceptance: edit a utc, save, git diff = exactly intended lines; undo works; round-trip suite green.

**WP3.2 — Remaining blueprint schemas.** Tier: **Low** (stamp from WP3.1).
Deliverables: `UtiSchema` (properties list, model parts), `UtpSchema`, `UtdSchema`, `UtwSchema`, `UtsSchema`, `UttSchema`, `UtmSchema`, `AreSchema` (area properties: lighting, fog, skybox, audio, tileset read-only).
Acceptance: per-schema smoke test (open real file, toggle one field, verify diff); suite green.

**WP3.3 — Instance editing.** Tier: **Mid**.
Inputs: `GitDocument`, `InstanceFieldMap` port from `StoreInstanceSync.cs`, skills docs for placement fields.
Deliverables: `AreaInstancesView` (per-list grids: add-from-palette via `InstanceFieldMap` — blueprint fields → instance struct + position defaults; duplicate; delete; edit X/Y/Z/orientation; per-instance VarTable editor), `PaletteEditorView` (itp tree: add node, move blueprint, rename).
Acceptance: place a creature instance from palette into a git.json entirely in-tool; diff shape matches a hand-authored placement from the skills docs; suite green.

**WP3.4 — Validation rules.** Tier: **Low** (rules are enumerated; fixtures mechanical).
Inputs: `.codex\skills\swlor-quest-generation\references\implementation-checklist.md`, `Readmes\CapstoneQuestLinePlan.md`, `StoreInstanceSync.cs`.
Deliverables in `Domain\Validation\`: `IValidationRule` + `ValidationIssue`; rules: `ResRefExistsRule` (TemplateResRef → blueprint file exists), `ResRefLengthRule` (≤16, lowercase), `VarTableEnumRule` (known keys have valid IGameCodeIndex values), `QuestActivatorNotInPaletteRule` (OnUsed=quest_enc placeables are world-instance-only), `SpawnWaypointPaletteRule` (spawn waypoint blueprint Tag == spawn table ID and palette entry exists), `DanglingInstanceTemplateRule`, `PaletteOrphanRule`; `ValidationPanelViewModel` in the app.
Acceptance: fixture tests per rule (seeded-error JSON + known-good corpus files as negative controls).

**WP3.5 — Save + pack services.** Tier: **Low**.
Deliverables: `SaveService` (dirty-docs only; atomic temp+`File.Replace`; EOL/trailing-newline preserving), `PackService` (shell a solution-built `SWLOR.CLI.exe -p "<mod>" --no-prompt` with CWD=`Module\`, stream output to Output panel).
Acceptance: save-then-`git status` shows only edited files; pack completes from in-app.

**WP3.6 — End-to-end daily-driver gate.** Tier: **Lead + human.**
Do the real task: place a quest NPC (utc blueprint + git instance + VarTable + spawn waypoint + palette entry) per skills conventions, entirely in-tool → save → pack → deploy via existing debugserver flow → verify NPC spawns and quest wiring in game.
Acceptance: task completes with no hand-edited JSON; human confirms in-game.

### Phase 4 — Viewport foundation

**WP4.1 — GL spike (how Radoub renders).** Tier: **Lead**.
Read Quartermaster/Reliquary source: which Avalonia GL control, shader setup, MDL→mesh path. Decide + document the approach in `SWLOR.Toolset\Viewport\README.md`. Small proof: one MDL rendered in a toolset window.
**WP4.2 — Mesh/texture pipeline.** Tier: **Mid**. `Domain\Render\MdlMeshBuilder.cs` (Radoub Mdl → positions/normals/UVs/node transforms), `TextureLoader.cs` (Tga/Dds/Plt + TXI transparency), `MaterialResolver.cs` (Mtr). Headless tests: build meshes for a 20-model sample list without error.
**WP4.3 — Model preview panes.** Tier: **Low** (after 4.1/4.2). 3D preview in utc/utp/utd editors (appearance-driven model lookup via services). Acceptance: 20-item visual spot-check vs in-game.
**WP4.4 — Area scene assembly.** Tier: **Mid**. `AreaSceneBuilder.cs`: `AreDocument.Tile_List` (Tile_ID/Tile_Orientation 0–3 ×90°/Tile_Height × tileset transition height) on the 10m grid × `TilesetCatalog` × `ResourceIndex` → scene; missing resources = logged fallback cubes, never fatal. Headless smoke test: assemble **all 438 areas** with zero exceptions.
**WP4.5 — Area view.** Tier: **Mid**. `GlAreaControl` (orbit/pan/zoom camera), render tiles + instances (creatures via appearance model or capsule+label, placeables, doors, waypoint/sound/store markers, trigger polygons from Geometry). Acceptance: all areas render; visual spot-check of 10 areas vs Aurora/in-game.

### Phase 5 — Viewport interaction

**WP5.1 — Picking + selection sync.** Tier: **Mid**. Ray pick (AABB then triangle); selection syncs both ways with instance lists/Properties.
**WP5.2 — Gizmos + placement.** Tier: **Mid**. Translate (grid snap) + rotate-Z gizmos; drag-from-palette into 3D; all edits emit `DocumentTransaction`s on the git doc. Acceptance: WP3.6's task done fully in 3D; diff shape identical to list-editor path; undo spans 3D edits.

### Phase 6 — Walkmesh + polish

**WP6.1 — WOK.** Tier: **Mid**. Check Radoub Mdl for WOK support; else small parser (`WokMeshLoader.cs`). Overlay toggle; surface-snap uses walkmesh height. Acceptance: placement Z matches in-game ground on sampled points (human check).
**WP6.2 — Perf + fidelity pass.** Tier: **Mid**. Tile main-lights, per-area lighting scheme, large-area frame-rate, catalog/index perf. Acceptance: largest area (`pw_ar_czarmrange`) interactive. Nice-to-haves queued for this pass: Output panel auto-scrolls to the latest line (user request); shared resource dictionary for duplicated field DataTemplates; refresh committed tools CLI binary and drop PackService fallback.

### Phase 7 — Tile painting (last)

**WP7.1 — Adjacency corpus.** Tier: **Low**. Extract from all 438 existing areas: for every adjacent tile pair, record (tileset, tile IDs, orientations, edge/corner terrain per SET). Emit `SetRuleCorpusTests`: every existing area must be rule-consistent under the SET definitions — this validates our rule reading before the matcher ever writes.
**WP7.2 — Rule matcher.** Tier: **Lead**. `SetRuleMatcher.cs`: given a target terrain/crosser paint at (x,y), solve legal tile IDs/orientations for affected cells (corner/edge constraint propagation), corpus as fallback for underspecified sets.
**WP7.3 — Paint tools + new area.** Tier: **Mid**. Paint/raise-lower/rotate tools regenerating `Tile_List` transactionally; new-area wizard (are/git/gic triplet from template + `IfoDocument.Mod_Area_list` entry). Acceptance: create a small area, paint terrain, pack, walk it in game (human check); paint→save→reopen idempotent; all suites green.

---

## Risks / open items

1. **Radoub churn (0.x):** pin submodule; wrap Radoub types behind Domain interfaces (`ResourceIndex`, `GffJsonBridge`, `MdlMeshBuilder`); consider upstreaming generic pieces (JSON-GFF, SET parser) to cut fork drift.
2. **Nim float/collation edge cases:** WP1.2/1.3 make failures enumerable; budget Lead time in Phase 1.
3. **GIT list key spellings** ("Creature List" vs "CreatureList" etc.): verify against corpus in WP1.4 before coding accessors.
4. **SWLOR.Game.Server reference weight** in GUI: Roslyn fallback specified in WP2.5.
5. **Avalonia GL control quirks:** WP4.1 spike decides from Quartermaster's working code.
6. **GPL:** internal use fine; public distribution of the toolset exe requires GPL-3.0 source release (server never references toolset).

## Critical files

- `SWLOR.Game.Server.sln`, `SWLOR.CLI\Program.cs`, `SWLOR.CLI\ModulePacker.cs` (ReadKey ~:111/:200; `GetModuleFolders()`), `SWLOR.CLI\StoreInstanceSync.cs`, `Build\hakbuilder.json`, `SWLOR.Game.Server.Tests\SWLOR.Game.Server.Tests.csproj`, `.codex\skills\swlor-quest-generation\` (conventions; edit skills only in `.codex\skills\` then `powershell -ExecutionPolicy Bypass -File tools/SyncAgentSkills.ps1`), `Readmes\CapstoneQuestLinePlan.md`.
