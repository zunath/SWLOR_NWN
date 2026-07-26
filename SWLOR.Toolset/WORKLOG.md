# SWLOR Toolset — Work Log

Single source of truth for work-package progress. The approved plan (phases, work packages,
orchestration protocol, ground rules, corrected format specs) lives in-repo at
`SWLOR.Toolset\PLAN.md`. One entry per work package; update the status line in place and
append details as work happens. Statuses: `pending | in-progress | done | blocked`.

## WP0.1 — done — 2026-07-19
- Tier: Mid (controller-executed).
- Files: `External/Radoub` submodule (pinned `radoub-v0.11.0`, commit `8dd65638`),
  `SWLOR.Toolset{,.Domain,.Tests}` projects, sln entries, `LICENSE-NOTICE.md`, this file,
  placeholder test, minimal Avalonia scaffold (`Program.cs`, `App.axaml`, `Shell\MainWindow`).
- **Decisions recorded:**
  - **.NET version: `net10.0`** for all three toolset projects. Deviation from the expected
    `net9.0`, with rationale: the reference floor is Radoub.Formats (`net9.0`), which rules
    out `net8.0`; but .NET 9 is an STS release that left Microsoft support in May 2026 AND
    no 9.0 runtime is installed on the dev machine (tests failed to launch). `net10.0` is
    therefore the lowest *supported* version meeting all cross-compatibilities (LTS, runtime
    10.0.10 + SDK 10.0.302 installed; net10 projects reference net9 Radoub and net8
    SWLOR.Game.Server downward without issue — verified by full-solution Debug and Release
    builds plus a passing test run).
  - **Radoub.UI: referenced** — cleanly consumable csproj; contains `ModelPreviewGLControl`
    (OpenGL model preview, Silk.NET.OpenGL) which WP4.x will reuse. Chains
    `Radoub.Dictionary` transitively.
  - Radoub uses **Central Package Management** via `External\Radoub\Directory.Packages.props`
    (scoped to its subtree; repo root unaffected). Our packages pin matching versions:
    Avalonia 11.3.17, CommunityToolkit.Mvvm 8.4.2.
  - Radoub uses Nerdbank.GitVersioning — works because the submodule is its own git repo.

## WP0.2 — done — 2026-07-20 — CLI `--no-prompt` flag
- Tier: Low (controller-executed inline; cheaper than dispatch).
- Files: `SWLOR.CLI\Program.cs` (new `--no-prompt` option), `SWLOR.CLI\ModulePacker.cs`
  (`PackModule`/`UnpackModule` take `bool noPrompt = false`; ReadKey prompt skipped when set).
- Default behavior unchanged (prompt still shown without the flag). CLI builds clean.
## WP1.1 — done — 2026-07-20 — Generic JSON-GFF model + reader/writer
- Tier: Lead (controller-executed).
- Files: `SWLOR.Toolset.Domain\Gff\{GffFieldType,JsonStringCodec,NimFloatFormatter,
  JsonGffField,JsonGffStruct,JsonGffDocument,GffJsonReader,GffJsonWriter}.cs`.
- **Format spec corrections discovered against the corpus (supersede the plan's spec):**
  - cexolocstring strref `"id"` lives at the FIELD level (`{"id": N, "type": "cexolocstring",
    "value": {...}}`), not inside `value`.
  - Key ordering is CASE-INSENSITIVE ASCII (Nim `cmpIgnoreCase`): `fortbonus` < `LawfulChaotic`.
    Implemented in `JsonGffStruct.CompareIgnoreCase` for new-field insertion; parsed order is
    preserved verbatim for round-trip.
  - `void` values are NOT base64 — they embed raw binary bytes (including invalid UTF-8)
    inside JSON string tokens. Parser is therefore a byte-level tokenizer (no Utf8JsonReader,
    no .NET-string round-trips); all scalar tokens preserved as raw bytes.
  - Struct-typed fields carry `__struct_id` at field level AND inside the value object.
  - Float literals: nwn_gff prints C `%.16g` (MSVCRT semantics) then appends `.0` when no
    '.'/'e'. MSVCRT double-rounds: exact expansion → 17 significant digits → 16, both half
    away from zero; scientific exponents are 3-digit zero-padded (`e-039`). Implemented via
    exact BigInteger decimal expansion in `NimFloatFormatter`; verified partly by P/Invoking
    msvcrt.dll's own sprintf during investigation.
  - **Float conformance must run through the float32 funnel** (text → double → float32 →
    double → format) because GFF floats are 32-bit; comparing against the parse-double alone
    is wrong for literals the printer didn't emit round-trip-faithfully.

## WP1.2 — done — 2026-07-20 — Corpus conformance utilities
- Files: `SWLOR.Toolset.Tests\{CorpusLocator,NimFloatFormatterTests}.cs`.
- Float conformance covers EVERY float/double literal in the corpus (>100k literals) with
  distinct-failure reporting; float-typed literals funneled through float32.

## WP1.3 — done — 2026-07-20 — Round-trip gate
- Files: `SWLOR.Toolset.Tests\{RoundTripCorpusTests,RoundTripSampleTests,EditLocalityTests}.cs`.
- **Gate green: all ~17,900 Module JSON files round-trip byte-identically.** Edit-locality
  exact (one field change → exactly one changed line). Suite ≈37s. One benign skip:
  `module.jrl.json` has no mutable integer at any depth (explicit Assert.Ignore).
- This suite is permanent and must stay green in every later package.
## WP1.4 — done — 2026-07-20 — Typed documents
- Tier: Low (Sonnet subagent); controller-verified (full suite green, 57/58 + benign skip).
- Files: `SWLOR.Toolset.Domain\Documents\*` (18 files: GffDocumentBase, GffStructExtensions,
  LocString, VarTable, Are/Git/Gic/Utc/Uti/Utp/Utd/Utw/Uts/Utt/Utm/Itp/Ifo/Fac documents),
  `SWLOR.Toolset.Tests\DocumentTests.*.cs` (8 files).
- **Corpus findings recorded:** GIT list keys verified: "Creature List", "Door List",
  "Encounter List", "List" (= LOOSE ITEM instances, not sounds), "Placeable List",
  "SoundList", "StoreList", "TriggerList", "WaypointList" — spacing inconsistent by key.
  UTM uses "ResRef" (not "TemplateResRef"). VarTable entry:
  `{Name: cexostring, Type: dword (1=int,2=float,3=string), Value: typed}` with __struct_id 0;
  setters mutate Value in place for minimal diffs. Palette CC/DELETE_ME absent from this
  corpus (exposed as nullable for toolset-authored files).

## WP1.5 — done — 2026-07-20 — Transactions/undo/dirty
- Tier: Mid (Sonnet subagent); controller-verified (83/84 green incl. corpus gate).
- Files: `SWLOR.Toolset.Domain\Editing\*` (IDocumentEdit, EditScope, FieldValueEdit,
  StructFieldEdits, ListElementEdits, LocStringEdits, DocumentTransaction, UndoStack,
  DocumentSession), `SWLOR.Toolset.Tests\EditingTests.*.cs`; additive guard hooks in
  JsonGffField/JsonGffStruct/Documents\LocString.
- **Design decision recorded:** ambient `EditScope` (AsyncLocal) — mutations throw when a
  DocumentSession is open with no active transaction; replay suppression on undo/redo;
  value mementos restore RAW BYTES (fidelity through undo), structural edits replay through
  the guarded methods. Guard is process-ambient (one-document-editing usage), not per-document.
- **Open item for WP3.x:** `VarTable.cs` pre-existing direct list mutations (and raw
  `Elements`/`LocStringEntries` list access generally) bypass the guard — route through the
  new guarded `InsertElement`/`RemoveElementAt` APIs when instance editing lands.

## WP1.6 — done — 2026-07-20 — Radoub bridge
- Tier: Low (Sonnet subagent); controller-verified.
- Files: `SWLOR.Toolset.Domain\Gff\GffJsonBridge.cs`, `SWLOR.Toolset.Tests\GffBridgeTests.cs`,
  additive byte-level `DecodeToBytes`/`EncodeBytes` in `JsonStringCodec.cs`.
- Bridge round-trip (JSON → Radoub GffFile → JSON) byte-identical over 30 files × 16 folders.
- **Findings recorded:** locstring language-id order is NOT always ascending (e.g. MapNote
  keys 0,2,256,258,260,4,8) — insertion order must be preserved. CExoString/locstring text can
  embed raw non-UTF-8 bytes (NWN inline color codes) — string bridging goes through
  Windows-1252 + byte-level codec, matching Radoub's own binary I/O encoding. Field types
  char/double/dword64/int64 occur ZERO times in the module corpus (covered by unit test only).
  Radoub locstring no-strref sentinel is 0xFFFFFFFF.
## WP2.1 — done — 2026-07-20 — 2DA + TLK services
- Tier: Low (Sonnet subagent); controller-verified (100/101 green incl. corpus gate).
- Files: `Domain\GameData\TwoDa\{TwoDaTable,TwoDaService}.cs`,
  `Domain\GameData\Tlk\{TlkJsonFile,TlkService}.cs`, tests `TwoDaTests.cs`/`TlkTests.cs`.
- Radoub TwoDAReader/TlkReader used as-is behind thin wrappers.
- **Findings:** `sw_2da\iprp_spells past.2da` is garbage (no 2DA V2.0 header — leftover
  scratch data); handled tolerantly, 746/747 parse. Custom TLK: 21,884 entries, SPARSE ids
  ranging 0–192,552 (dictionary-keyed, not array).
## WP2.2 — done — 2026-07-20 — SET parser
- Tier: Mid (Sonnet subagent); controller-verified.
- Files: `SWLOR.Toolset.Domain\GameData\Tilesets\{SetFileParser,TilesetDefinition,
  TileDefinition}.cs`, `SWLOR.Toolset.Tests\SetParserTests.cs`.
- All 70 `.set` files under `SWLOR_Haks\sw_t_*` parse; spot values verified against tde01.set.
- **Grammar findings recorded:** rule sections are `[PRIMARY RULE0]` (space before index);
  every corpus file has SECONDARY RULES Count=0; one file uses lowercase `floor=`; strings
  decoded via Latin-1 (one file has a raw Windows-1252 byte). **Declared counts are
  untrustworthy** (sw_t_season wsf10.set has garbage `Doors` counts like -481034240) — parser
  discovers repeated blocks by sequential index scan, never by declared Count. Duplicate keys:
  last-wins (documented convention choice).
## WP2.3 — done — 2026-07-20 — Resource index
- Tier: Mid (Sonnet subagent); controller-verified.
- Files: `Domain\GameData\Resources\{ResourceIdentity,NwnInstallLocator,KeyBifCatalog,
  HakDirectoryCatalog,ResourceIndex}.cs`, test `ResourceIndexTests.cs`.
- **Decisions/findings recorded:** cold scan of 113 hak layers = 165ms → persisted index
  cache NOT implemented (unneeded). **NWN:EE install present on dev machine via GOG Galaxy**
  (`C:\Program Files (x86)\GOG Galaxy\Games\Neverwinter Nights Enhanced Edition`) — KEY/BIF
  layer verified against the real install. Radoub's ResourceTypes table lacks NWN:EE `mtr`
  (type 2072, per SWLOR.NWN.API ResType) — patched locally in ResourceIdentity. Hak
  precedence: hakbuilder.json order, later-wins, base game lowest (per-module order would be
  module.ifo Mod_HakList — noted in code).
## WP2.4 — done — 2026-07-20 — Lookup services
- Tier: Low (Sonnet subagent); controller-verified (118/119 green incl. corpus gate).
- Files: `Domain\GameData\Lookups\{DisplayNameResolver,AppearanceService,PortraitService,
  PlaceableAppearanceService,DoorTypeService,SoundService,TilesetCatalog}.cs`,
  test `LookupServiceTests.cs`.
- **2DA findings:** appearance.2da RACE is dual-purpose (model resref when MODELTYPE=S,
  phenotype letter when =P); every appearance STRING_REF in corpus is ****; portraits.2da has
  no strref column; placeables.2da Label is already display text; ambientsound.2da
  Description is the strref column, Resource the sound resref. 70 tilesets discovered, no
  name collisions. ResourceIndex lacks enumeration API — TilesetCatalog scans hak layer dirs
  for discovery, resolves bytes via TryLookup (noted in XML docs; consider adding enumeration
  to ResourceIndex if a third consumer needs it).

## WP2.5 — done — 2026-07-20 — Game-code index
- Tier: Mid (Sonnet subagent); controller-verified.
- Files: `Domain\GameData\GameCode\{IGameCodeIndex,GameCodeIndex,ReflectionEnumReader,
  SourceIdScanner}.cs`, test `GameCodeIndexTests.cs`.
- **Decisions recorded:** enums (NPCGroupType 267 entries, KeyItemType 200) read via direct
  reflection over the referenced SWLOR.Game.Server assembly — verified no static-ctor/native
  hazards, no ModuleInitializers. Quest IDs (271) and spawn table IDs (191) via two-pass
  regex source scan (literal + same-file const resolution); Roslyn dependency deliberately
  NOT added. **Known gap:** IDs passed through helper-method parameters (guild item tasks,
  ~19 fishing points) aren't resolved — acceptable for validation; revisit only if WP3.4
  false-positives it. Missing source root → empty collections + IsSourceScanAvailable=false.
## WP2.6 — done — 2026-07-20 — Shell + read-only browser (PHASE 2 COMPLETE)
- Tier: Mid (Sonnet subagent); controller-verified (130/131 green, exe builds clean).
- Files: `Domain\Workspace\{ResourceType,ModuleWorkspace,BlueprintCatalog}.cs` (headless,
  tested — controller deviation from plan which placed these in the app),
  `Tests\WorkspaceTests.cs`; app: DI in App.axaml.cs, `Settings\ToolsetSettings`,
  `Workspace\{OutputLogService,WorkspaceContext,ModuleFileWatcher}`, Dock.Avalonia shell
  (`ToolsetDockFactory`, ShellViewModel, 4 panel VMs + views, ViewLocator, status bar).
- Packages: Dock.Avalonia/Themes.Fluent/Model.Mvvm 11.3.12.1 (12.x needs Avalonia 12 — rejected).
- **Measured on real launch:** module open 0ms; catalog build 2259ms for 16,686 entries
  (counts verified exact). Explorer uses two-level virtualized lists (Avalonia TreeView won't
  virtualize 8341-entry nodes). Game-data services registered only when repo paths resolve;
  optional ctor params degrade to raw ids. Read-only guarantee held (settings.json only write).
## WP3.1 — done — 2026-07-20 — Editor schema infrastructure + UTC editor
- Tier: Lead (controller-executed). 135/136 tests green; app launch smoke-verified.
- Domain: `Editors\{EditorKind,FieldDescriptor,EditorSchema,SchemaFieldAccessor}.cs`,
  `Editors\Schemas\UtcSchema.cs` (field names/types verified against corpus),
  tests `EditorSchemaTests.cs` (schema-vs-corpus conformance, transaction edit →
  undo → byte-identical, create-missing-field lands at case-insensitive sorted position).
- App: `Editors\{LookupOptionProvider,FieldViewModels,VarTableSectionViewModel,
  BlueprintEditorViewModel,EditorService}.cs`, `Editors\Views\BlueprintEditorView.axaml`,
  DocumentDock added to ToolsetDockFactory (+OpenDocument/ActivateDocument), explorer
  double-click opens editors, DI wiring (Func<EditorService> breaks the factory cycle),
  DataGrid Fluent theme added to App.axaml.
- **Pattern for WP3.2 stamping:** add `Editors\Schemas\<Type>Schema.cs` + register in
  `EditorService.GetSchema`. Dropdowns degrade to numeric when lookups missing; VarTable grid
  has known-key completion + live NPCGroupType hint via IGameCodeIndex.
- Notes: editor Save is a minimal atomic write (temp + File.Move overwrite) — WP3.5 replaces
  it with SaveService (EOL audit, dirty-only multi-doc). VarTable Type consts made public.
  In-UI end-to-end diff verification rides with the WP3.6 human gate; the equivalent
  Domain-level guarantee is covered by EditorSchemaTests.
## WP3.2 — done — 2026-07-20 — Remaining blueprint schemas
- Tier: Low (Sonnet subagent); controller-verified (198/199 green).
- Files: `Domain\Editors\Schemas\{Uti,Utp,Utd,Utw,Uts,Utt,Utm,Are}Schema.cs`,
  EditorService.GetSchema extended, `Tests\EditorSchemaStampTests.cs`.
- **Corpus findings:** door `Appearance` is ALWAYS 0 in corpus — real doortypes.2da id is
  `GenericType_New` (dropdown binds there). Trigger scripts are `ScriptOnEnter`/`ScriptOnExit`
  style. UTM and ARE use `ResRef` not `TemplateResRef`. No VarTable anywhere in uts/utm/are
  corpus files (HasVarTable=false for those). Item PropertiesList, store item lists, and
  sound Sounds list deliberately excluded from schemas (future packages).
## WP3.3 — done — 2026-07-20 — Instance editing
- Tier: Mid. Split execution: Sonnet subagent (killed twice mid-run by server error + spend
  limit) produced InstanceFieldMap + tests + the three editor VMs; controller finished the
  package inline (EditorService Area routing, AreaEditorView.axaml, missing using, verification).
- Files: `Domain\Documents\InstanceFieldMap.cs`, `Tests\InstanceEditingTests.cs`, app
  `Editors\{AreaEditorViewModel,InstanceListSectionViewModel,PaletteBrowserViewModel}.cs`,
  `Editors\Views\AreaEditorView.axaml(.cs)`, EditorService Area branch.
- Design: composite area editor owns TWO DocumentSessions (.are + .git) with split undo
  (toolbar Undo/Redo = instances/.git; separate pair for area properties); palette browser is
  an inline flyout per section with light category rename/delete on its own itp session;
  add-from-palette creates instances via InstanceFieldMap at 0,0,0 for numeric editing.
- Verified: 203/204 tests green (incl. instance create → sorted serialize → undo →
  byte-identical), app builds clean, 12s launch smoke OK.
- Note: field DataTemplates duplicated between BlueprintEditorView and AreaEditorView —
  candidate for a shared resource dictionary in a later cleanup pass.
## WP3.4 — done — 2026-07-20 — Validation rules
- Tier: Low (Sonnet subagent); controller-verified.
- Files: `Domain\Validation\*` (ValidationIssue/Context, IValidationRule, PaletteTraversal,
  6 rules, ModuleValidator), `Tests\ValidationRuleTests.cs` (31 tests + [Explicit] full-corpus
  run), app ValidationViewModel/View tabbed with Output.
- **Corpus findings:** `CREATURE_SPAWN_TABLE_ID` (string) + `CREATURE_SPAWN_COUNT` (int) live
  on the .git file's ROOT VarTable (GitDocument.VarTable), not AreaProperties/.are. Spawn
  waypoint convention confirmed: utw Tag == spawn table ID + RESREF leaf in waypointpalcus.
- Full-corpus run: 15,248 issues — 15,243 from DanglingInstanceTemplateRule, almost all
  base-game/hak templates (1078 distinct resrefs) invisible to a Module-only check.
  **Controller follow-up applied post-commit:** ValidationContext gained optional
  ResourceIndex (and the app now attaches the base-game KEY/BIF layer to its ResourceIndex
  when an install is found — benefits the viewport later too). Suppression measured:
  15,243 → 5,528 dangling issues. The remainder is legacy provenance noise (git instances
  are self-contained; templates deleted years ago cause no runtime failure), so the rule was
  downgraded to Warning severity with rationale in code. Full-corpus totals now:
  5 Errors (4 ResRefLength + 1 PaletteOrphan) + 5,528 Warnings.
## WP3.5 — done — 2026-07-20 — Save + pack services
- Tier: Low (controller-executed inline due to subagent spend-limit failures).
- Files: `SWLOR.Toolset\Services\{SaveService,PackService}.cs`; editors refactored to
  SaveService.WriteAtomic; EditorService.SaveAll; ShellViewModel SaveAll/PackModule commands;
  MainWindow toolbar (Save All, Pack Module); DI.
- **Bug fixed in passing:** closed editor tabs were never removed from EditorService's
  registries (reopening would activate a disposed document) — Closed events added.
- **PackService CLI resolution:** prefers solution-built `SWLOR.CLI\bin\{Debug,Release}\net8.0`
  (understands --no-prompt); falls back to committed `tools\SWLOR.CLI\SWLOR.CLI.exe` WITHOUT
  the flag + redirected stdin (that binary predates WP0.2). **Open item:** refresh the
  committed tools binary next time CLI changes ship, then simplify. Module filename read from
  `Module\config.json` ModuleFileName (fallback to the known default).
- Verified: 203/204 green, app builds clean, launch smoke OK. Real pack + in-game load is the
  WP3.6 human gate.
## WP3.6 — done — 2026-07-20 — End-to-end daily-driver gate (PHASE 3 COMPLETE)
- Tier: Lead + human. **Human-verified in game:** quest NPC placed entirely in-tool
  (palette add → position/Tag/VarTable → Save All), packed via Pack Module, deployed, and
  the creature spawned in game. Two issues found and fixed during the gate: file-watcher
  pack-noise flood (filtered) and stale config.json module filename (corrected + hardened
  resolution). **The daily driver ships.**

## Backlog — nice-to-haves (not scheduled)
- PLT color-layer rendering for body parts/armor (skin/hair/cloth/leather tints from utc +
  armor per-part colors) — user-accepted follow-up from the WP4.3 gate (2026-07-21).
  Candidate: WP6.2 polish pass.
- Base-game/hak fallback when an equipped armor's uti resref isn't in the module (e.g.
  atris_robes) — would need ResourceIndex bytes → GffJsonBridge. Low priority.
- Output panel: auto-scroll to the latest entry as lines arrive — OR consider rendering it
  tailed in reverse order (newest at top), user's suggested alternative (re-requested
  2026-07-21). Candidate: WP6.2 polish pass.
- Shared resource dictionary for the field DataTemplates duplicated between
  BlueprintEditorView and AreaEditorView (noted in WP3.3).
- Refresh committed tools\SWLOR.CLI\SWLOR.CLI.exe with the --no-prompt build, then drop
  PackService's fallback path (noted in WP3.5).
- Input-convention audit: sweep every mouse binding and keybind in the toolset for anything
  still following the legacy Aurora/BioWare paradigm rather than modern-application
  convention, and fix each (user-requested 2026-07-21 alongside the camera orbit/pan swap).
  Known starting points beyond the viewport camera: verify gizmo/manipulation modifiers,
  placement-mode cancel, and any shell/panel shortcuts. Candidate: WP6.2 polish pass.
## WP4.1 — done — 2026-07-20 — GL spike
- Tier: Lead (controller-executed).
- **Spike findings (full record in `SWLOR.Toolset\Viewport\README.md`):** Radoub renders via
  Avalonia's built-in `OpenGlControlBase` + Silk.NET.OpenGL; `ModelPreviewGLControl` +
  `OpenGLShaderManager` + `TextureService` + `MdlPartComposer` are all public in Radoub.UI,
  which we already reference — the blueprint preview control is REUSED, not rebuilt.
  TextureService needs only FindResource/FindBaseResource/FindResourceWithSource from
  IGameDataService.
- Files: `Viewport\README.md`, `Viewport\SwlorGameDataService.cs` (minimal IGameDataService
  adapter over ResourceIndex/TwoDaService/TlkService), `Shell\Panels\ModelPreviewViewModel.cs`
  + `Shell\Views\ModelPreviewView.axaml(.cs)` (Model Preview panel tabbed with Properties:
  creature selection → appearance.2da → MODELTYPE S/L model resref → MdlReader →
  ModelPreviewGLControl with textures; MODELTYPE P reports "arrives with WP4.3").
- Verified: build clean, 203/204 tests green, 12s launch smoke with the GL panel docked.
  Visual confirmation of an actual rendered model = human spot-check (select a beast/simple
  creature in the explorer and look at the Model Preview tab).
## WP4.2 — done — 2026-07-20 — Mesh/texture pipeline
- Tier: Mid (Sonnet subagent); controller-verified (build clean, 217/218 green incl.
  corpus gate; scope exact: Render\*, Domain csproj Pfim line, RenderPipelineTests).
- Files: `Domain\Render\{MdlMeshBuilder,TextureLoader,TxiInfo,MaterialResolver}.cs`,
  Domain csproj (+Pfim 0.11.4, matching Radoub.UI's pin), `Tests\RenderPipelineTests.cs`
  (14 tests; mesh build over 30 tile models → 552 meshes, 0 empty).
- **Decisions/findings recorded:**
  - Geometry filter = `node is MdlTrimeshNode` (covers Skin/Dangly/Anim/Aabb subclasses)
    gated on `Render == true`; `ComposeNodeTransform` mirrors Radoub.UI
    `ModelViewController.GetWorldTransform` (Scale × Rotation × Translation, node→root) so
    preview and area renderer place nodes identically. Faces index shared per-vertex
    Normals/TextureCoords[0] directly — corpus is binary MDL where `TvertIndex` is always -1
    (only MdlAsciiReader populates it).
  - Radoub.Formats has NO DDS/MTR/TXI parsers (plan overstated). DDS: Pfim + BioWare-header
    conversion adapted line-for-line from Radoub.UI TextureService into Domain (Domain must
    not reference Radoub.UI); includes BGR 5:6:5 endpoint swap. TXI/MTR: fresh minimal text
    parsers.
  - **Tile `Model=` resrefs are mostly BASE-GAME resources** — SWLOR tileset haks only
    add/override a subset; KeyBifCatalog (113,472 resources) supplies the rest. Mesh-sample
    test builds ResourceIndex with the KEY/BIF base layer and Assert.Ignores without an
    install (runs for real here via the GOG install).
  - Corpus: 14,828 .dds in SWLOR_Haks (BioWare variant confirmed); 2,426 .mtr (mostly
    sw_cr_creature) — parser tested against real `c_huttbomb1.mtr`; TXI keys verified against
    real files. NO `pal_*.tga` in haks (base-game only) — PLT test rides PltReader's
    grayscale fallback; palette-accurate PLT coverage deferred to a base-game-backed consumer.
  - MaterialResolver/TxiInfo deliberately not yet chained into TextureLoader — WP4.4/4.5
    decide composition.
## WP4.3 — code done, visual RE-gate pending — 2026-07-21 — Model preview panes
- **First gate result (human, 2026-07-20):** simple creatures, placeables, doors, and
  live-update all render correctly. Segmented (P-type) creatures broken: parts detached/
  floating, some apparently missing.
- **Root cause (found via headless probes, 2026-07-21):** several SWLOR hak body-part MDLs
  author their mesh vertices OFFSET from the part origin and correct them with node
  Positions inside the part file — e.g. `sw_pt_lthigh\pfh0_legl001.mdl` mesh node
  pos=(0.026, 0.013, -0.459) with vertices spanning Z=[-0.01..+0.53] (pointing UP), and
  `sw_pt_lshin\pfh0_shinl001.mdl` 'Shin' node pos=(-0.458, -1.033, -0.562). Radoub's
  MdlPartComposer discards those transforms when attaching parts to bones (sets attached
  mesh Position=Zero; documented assumption "body part MDLs have geometry at local origin"
  — true for BioWare parts, false for these SWLOR ones). Right-side counterparts are
  authored at origin, hence the asymmetric floating. Composition/bones/renderer math were
  all verified correct along the way (composite walk matched the renderer's
  GetWorldTransform for all 22 meshes; all 19 bones present; no skin meshes; nothing hit
  the skip heuristic). "Missing" parts in the screenshot are data-true: that utc has
  BodyPart 0/absent for both shoulders, belt, and right foot (Aurora renders part 0 as
  invisible — same in game).
- **Fix:** `Domain\Render\MdlGeometryFlattener.cs` — bakes each node's composed model-root
  transform into vertices/normals and resets node transforms to identity;
  `MdlGeometryFlattenerTests.cs` (4 tests: both real corpus offenders, an at-origin control
  part asserting no-op, synthetic nested-dummy+rotation asserting the transform order matches
  Radoub's GetWorldTransform). ModelPreviewViewModel now feeds the composer through
  `LoadComposerModel`: parts (withSupermodelAnims=false) get flattened; the skeleton NEVER is
  (its node transforms are the bones), nor are directly-rendered simple models (the control
  applies node transforms itself — baking would double-transform).
- Verified: build clean, 235/236 green (round-trip gate intact), launch-and-kill smoke OK.
- **Re-gate #1 (human, 2026-07-21):** bodies now assemble (flattener works) — but right foot
  missing on most segmented models, and worn armor renders inconsistently (some parts white).
- **Second fix round (controller, corpus-evidenced):**
  - **Right foot:** the creature's right-foot part number lives in `ArmorPart_RFoot` on the
    utc root — an Aurora format quirk. Corpus proof: 447 utcs carry ArmorPart_RFoot, ZERO
    carry BodyPart_RFoot (which my resolver had been reading). Fixed in BlueprintModelResolver.
  - **Armor:** segmented creatures were rendered naked — worn chest armor (Equip_ItemList
    struct id 2 → uti ArmorPart_*) now overrides body parts with Quartermaster's precedence
    (creature 0 always wins; armor >0 beats creature; head never overridden), including robe
    handling (ArmorPart_Robe>0 + model-exists check → robe part added, covered parts
    suppressed per QM's RobePartSuppression set: everything except head/neck/feet/belt;
    120 corpus armors have Robe>0). Resolver gained optional itemBlueprintLoader +
    partModelExists delegates; preview VM supplies both.
  - **White parts:** MdlPartComposer overwrites each attached mesh's Bitmap with the part
    resref (its BioWare stale-bitmap workaround) — but SWLOR custom parts name their real
    textures differently (pmh0_bicepl249's meshes use 'N_RepSold01'), so the override pointed
    at nothing → white. Preview VM now records authored bitmaps during part load and restores
    them post-Compose wherever the authored name resolves to a real plt/tga/dds; the
    composer's override is kept when it doesn't (the genuine stale-bitmap case).
  - **B1 crash fixed:** AreaEditorView.OnRootTabSelectionChanged NRE — TabControl raises
    SelectionChanged during XamlIlPopulate before named fields are assigned; null-guarded.
  - Verified: build clean, 260/261 green (2 new armor/robe resolver tests; naked-body test
    updated for footr), smoke OK.
- **Re-gate #2 (human, 2026-07-21):** armor now renders; three findings:
  robed NPCs amputated (crystal_refugee = head+skirt+feet), residual white/PLT-flat parts
  (user accepted PLT color fidelity as a follow-up), area 3D view loads but no camera input
  and needs a hide-ceilings toggle.
- **Third fix round (controller, corpus-evidenced):**
  - **Robes:** `sw_pt_robe\pfh0_robe033.mdl` is a LOINCLOTH — its only renderable meshes span
    Z 0.38–1.24 (probe); everything else in it is render=false rigging. QM's fixed
    RobePartSuppression list assumes every robe is a near-total body (true for CEP robes,
    false for SWLOR's partial robes) and amputated the torso/limbs. Fix: suppression is now
    GEOMETRY-DRIVEN — new `Domain\Render\RobeCoverage.IsFullBodyRobe` (renderable geometry
    must span ankles-to-shoulders: minZ<0.5 && maxZ>1.35); the resolver always emits robe +
    all body parts and exposes `RobeCoveredParts`; the preview VM filters covered parts only
    when the loaded robe model proves full-body. QM's own docs flagged exactly this
    ("half-robe variants would over-suppress… needs a data-driven signal").
  - **Camera input (WP4.5):** OpenGlControlBase is not hit-testable (no Background brush) —
    the exact limitation Radoub documents in ModelPreviewGLControl; the subagent copied the
    fallback overrides but not the overlay workaround, so GlAreaControl never received
    pointer events. Fix: transparent input Border overlay in AreaEditorView forwards
    pressed/moved/released/wheel into new public Handle* methods (pointer capture then routes
    drags to the control itself); toolbar sits above the overlay in z-order.
  - **Hide ceilings (WP4.5, user request):** shader gained WorldPos + `ceilingClipZ` uniform;
    fragments above each tile's own base height + 4m are discarded (per-tile-relative, so
    multi-elevation interiors clip correctly); markers/trigger outlines never clipped;
    "Hide ceilings" checkbox in the 3D View toolbar (default off).
  - Notes: atris_jedi's equipped 'atris_robes' uti does not exist in the module (dangling
    equip resref — loader degrades to naked body by design). Her white bracers/boots are the
    PLT-default-color rendering, i.e. the accepted follow-up, not a bug.
  - Verified: build clean, 264/265 green (4 new RobeCoverageTests incl. the corpus loincloth;
    robe resolver test rewritten for no-resolver-suppression), smoke OK.
- **Backlog added:** PLT color-layer rendering for body parts/armor tints (user-accepted
  follow-up, target WP6.2); consider base-game/hak fallback for equipped-armor uti resrefs
  missing from the module (e.g. via ResourceIndex + GffJsonBridge).
- **Re-gate #3 (human, 2026-07-21): WP4.3 GATE PASSED** — segmented previews work for most
  models; residual missing-texture polish explicitly tabled to a later phase (backlog).
  Camera orbit/pan/zoom and ceiling toggle confirmed working. Two WP4.5 findings fixed:
  - **Viewport not filling the panel:** glViewport was fed logical Bounds while Avalonia's
    framebuffer is physical pixels (Bounds × RenderScaling) — at 150% Windows scaling the
    render occupied the lower-left ⅔ exactly. Fixed (logical units retained for input math).
    Radoub's ModelPreviewGLControl has the same latent bug — worth an upstream report.
  - **Placeables/doors as real models:** instance appearance lives ON the git instance
    (placeable `Appearance` → placeables.2da ModelName; doors carry BOTH `GenericType_New`
    and `Appearance` as doortypes candidates — corpus shows either populated — first row
    with a real Model wins). AreaSceneBuilder resolves through the shared TileModelCache
    when the appearance services are supplied; InstanceMarker gained `Model`; GlAreaControl
    draws model instances textured/lit and everything else as pyramids. Corpus test:
    coxxian_hq zep_barricade resolves geometry.
  - **"Placeable models" toggle (user request):** checkbox in the 3D View toolbar switches
    placeables between 3D model (default) and pyramid marker. Same switch planned for
    creatures when creature models land in the area view (noted for WP5.x/6.x).
  - Verified: build clean, 265/266 green, smoke OK.
- **WP4.5 GATE PASSED (human, 2026-07-21):** placeables and doors render as real models,
  toggles work, viewport fills the panel, camera works — "Phase 4 looking good."
  **PHASE 4 COMPLETE.** (Residual texture/PLT polish deliberately tabled — see backlog.)
- Tier: Low (controller-executed inline; subagent dispatch avoided — the WP4.4 subagent
  died on the monthly spend limit, so remaining Phase 4 packages run inline).
- Files: `Domain\Render\BlueprintModelResolver.cs` (headless, tested),
  `Tests\BlueprintModelResolverTests.cs` (6 tests), app: `Shell\Panels\ModelPreviewViewModel.cs`
  (rewired), `Editors\BlueprintEditorViewModel.cs` (+BlueprintType/DocumentRoot/DocumentChanged),
  `Editors\EditorService.cs` (binds preview to live editor), `App.axaml.cs` (DI:
  Placeable/DoorType services + preview injected into EditorService).
- Verified so far: build clean, full suite 231 passed / 1 benign skip (round-trip gate green),
  app launch-and-kill smoke OK (DI chain EditorService→ModelPreviewViewModel resolves, catalog
  builds, killed cleanly). **Remaining: the human 20-item visual spot-check vs in-game.**
- **Design:** the standalone Model Preview dock panel now serves creatures (simple + segmented),
  placeables, and doors, driven two ways: explorer selection previews the on-disk blueprint;
  an open utc/utp/utd editor previews its LIVE in-memory document and refreshes on every edit
  (BlueprintEditorViewModel.DocumentChanged → EditorService.PreviewEditorModel →
  ModelPreviewViewModel.ShowForDocument). Chose panel-follows-editor over one embedded GL
  control per editor tab to avoid N concurrent GL contexts and keep the WP4.1 wiring intact.
- **BlueprintModelResolver (headless):** maps a blueprint doc → BlueprintModelReference:
  UTC MODELTYPE S/F/W/L → Simple(RACE resref); UTC MODELTYPE P → Segmented(skeleton
  `p{gender}{race}{phenotype}` + body parts from utc BodyPart_* fields, head from
  Appearance_Head); UTP Appearance → placeables.2da ModelName; UTD GenericType_New →
  doortypes.2da Model. Field paths verified against corpus + the WP3.2 schemas. Simple path is
  fully unit-tested against real corpus blueprints (ashwing→c_anurog, _mdrn_chair→PLC_X02,
  _mdrn_dt_bars→TCN_UDoor_10). Segmented spec is unit-tested (agr_guildmaster → pmh0 + 15
  parts, exact resrefs); the app composes it via Radoub's MdlPartComposer.
- **Known limitations for the visual gate to confirm/correct:** (1) segmented HEAD model naming
  (`{prefix}_head{NNN}`) is the least-certain part of the convention — MdlPartComposer silently
  skips any part whose model doesn't resolve, so a wrong head naming omits the head rather than
  crashing; the body still composes. (2) Part indices of 0 are treated as "none" and omitted.
  (3) Phenotype>0 parts that don't exist fall back to nothing (no phenotype-0 fallback). These
  are the primary things to check in the 20-item spot-check; fixes are targeted if needed.
## WP4.4 — done — 2026-07-20 — Area scene assembly
- Tier: Mid. Split execution: Sonnet subagent produced all four files then died on the
  monthly spend limit before running any build/test; controller verified inline (build
  clean on first try — the subagent's API assumptions were all correct — full suite green,
  scope exact).
- Files: `Domain\Render\{AreaScene,AreaSceneBuilder,TileModelCache}.cs`,
  `Tests\AreaSceneBuilderTests.cs` (8 tests). No csproj change (Pfim already present from WP4.2).
- **438-area acceptance gate PASSED (ran for real via the GOG install, not skipped):**
  438 areas, 103,913 tile placements, 5,831 distinct tile models parsed, **0 fallbacks**
  (every tile model across the whole corpus resolved and parsed), 106,221 instance markers,
  29.5s (< 2-min target). Full suite 225 passed / 1 benign skip.
- **Decisions/findings recorded:**
  - Tile_List field spellings verified against corpus: `Tile_ID`, `Tile_Orientation` (0-3,
    each step 90° CCW about +Z), `Tile_Height` (integer level × tileset transition height).
    Tileset transition height is `TilesetDefinition.Transition` (from SET `[GENERAL]`).
  - Grid: 10m cells (`AreaSceneBuilder.TileSize`), tile index i → col `i % Width`, row
    `i / Width`; TilePlacement.Transform = translate-to-origin × rotateZ(orient×90°) ×
    translate-to-(centerX,centerY,heightOffset), so a rotated tile keeps its grid cell.
  - TileModelCache: ConcurrentDictionary<resref, RenderModel?> caching hits AND misses;
    caller owns one instance across the batch so placements share RenderModel objects and
    each distinct model parses at most once (what makes 438-area assembly 30s not minutes).
    GetOrBuild never throws — blank/missing/unparseable → null → fallback placement.
  - Instance markers cover all git lists (Creature/Door/Item/Placeable/Sound/Store/Trigger/
    Waypoint via InstanceFieldMap; Encounter via geometry centroid). Trigger/Encounter carry
    the Geometry polygon (PointX/Y/Z); appearance-model resolution deferred to WP4.5 by design.
  - Encounter path is defensive/unverified — zero Encounter List entries in the corpus (re-
    confirmed). Missing/zero area Width degrades to single-column layout rather than div-by-0.
## WP4.5 — code done, human visual gate pending — 2026-07-21 — Area view
- Tier: Mid (Sonnet subagent, post-limit-reset); controller-verified (build clean, 258/259
  green incl. corpus gate, launch-and-kill smoke OK, scope matches report, Domain helpers
  reference no Radoub).
- Files: app `Viewport\GlAreaControl.cs` (OpenGlControlBase + Silk.NET, own minimal GLSL pair
  — Radoub's shader lacks the alphaCutoff/unlit uniforms and is unmodifiable),
  `Editors\AreaEditorViewModel.cs` (+AreaScene/EnsureSceneBuilt/RebuildSceneCommand,
  background Task.Run build), `Editors\Views\AreaEditorView.axaml(.cs)` ("3D View" tab,
  lazy build on first activation, Rebuild button), `EditorService.cs` (+3 optional ctor
  params threading TilesetCatalog/TileModelCache/ResourceIndex — flagged deviation, necessary
  plumbing), `App.axaml.cs` (TilesetCatalog+TileModelCache singletons inside the
  ResourceIndex guard), csproj (Silk.NET.OpenGL 2.23.0 pin + AllowUnsafeBlocks, matching
  Radoub.UI's own flag — flagged deviation, required for pointer-offset GL calls);
  Domain `Render\{AreaCameraMath,AreaDrawBatcher}.cs` + 23 tests
  (`AreaCameraMathTests` 16, `AreaDrawBatcherTests` 7).
- **Decisions:** one GPU upload per distinct RenderModel (reference-keyed — TileModelCache
  already dedupes), draw per placement×mesh with model uniform = mesh.Transform ×
  placement.Transform; textures via MaterialResolver → TextureLoader, cached by resolved
  name, TXI punchthrough as alpha-cutoff discard (no blending/sorting); fallback tiles render
  as a 10×10×1.5m footprint box (a 1m cube would vanish at grid scale); camera math pure in
  Domain (framing fits Width×10 × Height×10 bounds), control owns only mutable state +
  pointer glue; scene build lazy on tab activation, manual Rebuild (DocumentChanged
  auto-rebuild deliberately deferred to WP5.x).
- **WP5.1 hooks noted by executor:** expose view/projection/camera state for unproject;
  pick against TileBatch.Placements + AreaScene.Instances reusing draw matrices; shader
  needs a selected/highlight uniform.
- **Remaining: human visual gate** — all areas render; 10-area spot-check vs in-game.
## WP5.1 — done — 2026-07-21 — Picking + selection sync
- Tier: Mid (Sonnet subagent); controller-verified (build clean, 290/291 green incl. corpus
  gate — 25 new tests; scope exact; AreaPicking references no Radoub; launch smoke OK).
- Files: Domain `Render\AreaPicking.cs` (new: ray/AABB slab + Möller–Trumbore triangle,
  ConditionalWeakTable-cached per-mesh local AABBs, PickClosestInstance honoring the
  marker-vs-model display rule) + `AreaCameraMath.ScreenPointToRay` (PickRay via
  Invert(view×projection) NDC near/far unproject, row-vector convention); app:
  GlAreaControl (click-vs-drag <4px, InstancePicked event, SelectedInstance + GL_LINES
  wireframe highlight box — chosen over glPolygonMode for GLES portability),
  AreaEditorViewModel (SelectedSceneInstance/SelectionStatus, kind↔section maps, single
  re-entrancy-guarded ApplySelection funnel both directions route through),
  InstanceListSectionViewModel (+public BlueprintType), AreaEditorView wiring + status line.
- **Decisions recorded:** index-mapping verified — scene instances are built per kind in git
  list order and sections iterate the same lists, so index-within-kind == section row index.
  Item/Encounter kinds have no section (highlight-only pick). Selection clears on scene
  rebuild (fresh InstanceMarker identities). ComputeInstanceTransform/DrawsAsModel
  deliberately duplicated Domain-vs-control (Domain can't see the app control); WP5.2 may
  have the control delegate to AreaPicking.
- **WP5.2 notes from executor:** consider rebinding selection across rebuilds (Tag+kind
  match); highlight box is axis-aligned (not heading-oriented) — gizmos may want tighter
  bounds; shader has no selected-tint uniform (highlight is a separate wireframe pass).
## WP5.2 — done (gate passed) — 2026-07-21 — Gizmos + placement
- Tier: Mid (Sonnet subagent); controller-verified (build clean, 311/312 green incl. corpus
  gate + the two mandated byte-exactness tests; scope exact; AreaManipulation Radoub-free;
  launch smoke OK).
- Files: Domain `Render\AreaManipulation.cs` (ray∩horizontal-plane, 0.5m grid snap,
  heading↔orientation; 17 tests) + `AreaPicking.PickInstance` single-instance test (3 tests);
  app: GlAreaControl (drag gizmo: press must hit the SELECTED instance else falls through to
  orbit; live preview via local marker copy — document untouched until release; Alt+drag
  rotate at 0.01 rad/px; placement mode with Esc/right-click cancel; 4 new events),
  InstanceListSectionViewModel (OpenPaletteBrowser factored from Add; AddInstanceAt/
  SetInstancePosition/SetInstanceOrientation — one transaction each via existing _runEdit),
  AreaEditorViewModel (Place... flow, Move/RotateSelectedInstance, BuildSceneAsync reselect
  key by kind+index, Undo/Redo now refresh the 3D scene when built), AreaEditorView (event
  wiring, Place combo+button, palette popup, status hint "Drag: move | Alt+drag: rotate |
  Ctrl: snap 0.5m").
- **Acceptance core verified by test:** programmatic move/rotate through the gizmo path
  changes only the intended value lines and UNDO RESTORES BYTE-IDENTICAL content
  (InstanceEditingTests, against a real corpus .git).
- **Decisions:** move drags on the instance's current-Z plane; snap off-by-default (Ctrl
  enables); one transaction per drag committed on release, none for no-op clicks; scene
  refresh after each commit is a full rebuild with selection rebound by kind+index (lazy-
  build contract preserved). InstanceFieldMap untouched (setters already existed).
- **Deviations (flagged):** placement click projects onto Z=0, not the clicked tile's height
  plane (per-tile ray-hit deferred; elevated-tile placements need manual Z via detail form —
  WP6.x candidate). Trigger/Encounter Geometry polygons don't follow a Move (pre-existing
  list-editor behavior too).
- **WP6.x notes:** height-aware placement drop; rotate angle snapping; multi-select move.
- **Gate feedback round 1 (2026-07-21, "otherwise looking great"):** three fixes applied —
  (1) blank Place popup on area load: the popup's IsVisible bound through
  `PlacementSection.ActivePaletteBrowser` while PlacementSection was still null, so the
  binding never resolved and IsVisible stayed default-true; fixed with FallbackValue=False.
  (2) Wheel zoom direction inverted vs common practice — wheel up now zooms in.
  (3) Pack Module now copies the packed .mod to `debugserver\modules` after a successful
  pack (with a hint when the directory doesn't exist). Root cause of the user's confusion:
  the CLI's pack (-p) never deployed — only its full deploy (-o, DeployBuild) copies the
  module (plus binaries/haks); WP3.6's gate must have run that separately.
- **Gate feedback round 2 (2026-07-21, "Everything passes"):** camera mouse buttons swapped
  to modern-application convention. Was the legacy Aurora paradigm (primary/left button
  orbits); now left-drag PANS the view (grab-and-drag like a map) and right/middle-drag
  ORBITS. Shift+left also orbits, preserving an orbit path for laptop/trackpad users without
  a second mouse button. Left-drag-on-selected-object still runs the move/rotate gizmo
  (Alt=rotate), and a non-drag left click still picks/places — the pick-candidate gate moved
  from Orbit to Pan mode accordingly. GlAreaControl.HandlePointerPressed only; no test change
  (input mapping is UI-layer). Follow-up input-convention audit added to backlog.
- **PHASE 5 human gate: PASSED** (2026-07-21) — WP3.6's quest-NPC task completed fully in 3D
  (place creature, drag/rotate, Tag/VarTable, spawn waypoint, Save All → Pack Module
  auto-deploy → verified in game). WP5.1 + WP5.2 accepted. **PHASE 5 COMPLETE.**
## WP6.1 — done (gate passed; elevated-Z deferred to WP7.3) — 2026-07-21 — Walkmesh (WOK)
- Tier: Mid. Domain core (parser/cache/raycast/tests) dispatched to a Sonnet subagent; app/GL
  integration (overlay, snap, toggle, DI) done inline by the controller. Controller-verified:
  build clean (0 errors), 334/335 tests green (311 prior + 23 new; 1 pre-existing skip), launch
  smoke OK.
- **EMPIRICAL FORMAT FINDING:** every real `.wok` in this project — SWLOR hak-source tiles AND
  the retail base game (verified via KeyBifCatalog) — is plain ASCII "NWmax walkmesh" export text
  (reliable marker keyword `beginwalkmeshgeom`), NOT the binary "BWM V1.0" layout WP6.1 originally
  assumed. A byte search for "BWM V1.0" across every tileset .bif found zero matches. WokMeshLoader
  parses the ASCII grammar (the path every real resource takes) and keeps a self-consistency-guarded
  binary BWM parser as a forward-compatible fallback (pinned by a hand-built byte test only). Full
  grammar is documented in the WokMeshLoader class comment.
- Domain (subagent): `Render\WokMeshLoader.cs` (WalkMesh/WalkFace + Parse, never throws),
  `Render\TileWalkmeshCache.cs` (resolves `<tileModel>.wok`, caches hits+nulls like TileModelCache),
  `Render\AreaWalkmesh.cs` (RaycastGround: transform each tile's faces by TilePlacement.Transform,
  two-sided Möller-Trumbore; prefers walkable faces, falls back to any). AreaScene.TilePlacement
  gains a nullable `Walkmesh`; AreaSceneBuilder.Build takes an optional TileWalkmeshCache and
  populates it for non-fallback tiles (null cache = prior behavior exactly). Tests:
  WokMeshLoaderTests (14, incl. a deterministic hand-built byte blob + real-corpus probe),
  AreaWalkmeshTests (9, incl. synthetic raycast, preferWalkable filtering, builder integration).
- App/GL (controller): GlAreaControl — `ShowWalkmesh` toggle; per-scene overlay VBO (world-space
  faces, walkable range then blocked range) drawn translucent (green/red, alpha 0.4, depth-write
  off, lifted 0.06m) via a new default-1.0 `flatAlpha` shader uniform (opaque unlit draws set it
  back to 1); placement clicks (RaisePlacementPointPicked) now snap Z to AreaWalkmesh.RaycastGround,
  falling back to the Z=0 plane when no walkmesh is hit — resolves WP5.2's flagged Z=0 placement
  deviation. DI: TileWalkmeshCache registered from ResourceIndex + a surfacemat.2da "Walk"-column
  predicate (BuildSurfaceWalkability; all-walkable fallback when the table is unreadable), threaded
  through EditorService → AreaEditorViewModel → Build. UI: "Show walkmesh" checkbox in AreaEditorView.
- **Frame note (verify at the gate):** overlay and tiles share the exact same TilePlacement.Transform,
  so they render mutually aligned regardless of the tile-local frame, and the snap raycast hits the
  same walkmesh that is drawn. The subagent measured raw MDL verts as centered [-5,5] vs the assumed
  corner-origin [0,10]; prior gates confirmed instances/tiles align in true area space, so this is
  most likely a raw-vs-node-baked measurement artifact, not a real placement offset — the gate will
  confirm.
- **Gate result (2026-07-21):** overlay renders flush on the tile floors — item 1 PASSED. The
  elevated-tile Z check (item 2) is untestable today because the toolset has no way to alter/place
  tiles yet (that's Phase 7 tile painting); the height-snap logic itself is covered by
  AreaWalkmeshTests (synthetic raycast + preferWalkable). WP6.1 accepted; re-confirm elevated-Z
  once WP7.3 lands tile editing.
- **Gate feedback — camera reset on instance drag (fixed):** dragging a placeable (any gizmo
  move/rotate, place, or undo/redo) reset the camera's zoom/pan/orbit. Root cause was pre-existing
  (WP5.2-era): the GlAreaControl.Scene setter called ResetCameraForScene on EVERY assignment, and
  every edit commits a full scene rebuild that reassigns Scene. Fixed by framing the camera only on
  the first non-null scene (initial load) and preserving the user's camera on all later rebuilds.
  Build clean, 334/335 green, smoke OK.
## WP6.2 — done (gate passed) — 2026-07-21 — Perf + fidelity pass (PHASE 6 COMPLETE)
- Tier: Mid. Lead-driven (GL/perf + small UI); the one remaining UI nice-to-have (shared
  DataTemplates) is queued for a Sonnet subagent.
- **Per-area lighting (fidelity):** AreaScene now carries a decoded `AreaLighting` (Domain:
  `AreaLighting` type + `AreaLighting.DecodeColor` for NWN 0x00BBGGRR packing; `AreaSceneBuilder`
  `ComputeLighting` picks moon colors at night / sun colors by day from the .are). GlAreaControl
  feeds the shader's ambient + diffuse from it, brightened from a floor (AmbientLightFloor 0.30 /
  DiffuseLightFloor 0.25) toward the authored value so night areas keep their cool hue but stay
  editable — replaces the old flat gray constants. Tests: AreaLightingTests (4 — decode byte order,
  night moon selection, day sun selection, neutral default). NOTE: per-tile point/main lights are
  deferred — the area-level sun/moon scheme is the dominant mood effect and enough for an editor
  preview; per-tile point lighting would be a much larger fidelity add for marginal editor value.
- **Perf — uniform-location caching:** SetUniform* now cache glGetUniformLocation per shader program
  (cleared on program (re)create/teardown), removing thousands of per-frame driver string lookups on
  the 256-tile largest area (pw_ar_czarmrange). Visual output unchanged.
- **Output panel auto-scroll:** OutputView scrolls to the newest line as entries arrive.
- Verified: build clean, 338/339 green (334 prior + 4 lighting; 1 pre-existing skip), smoke OK.
- **Shared field DataTemplates (done):** the 7 identical field templates (LocString/Check/Integer/
  Float/Dropdown/Script/Text) moved out of both editor views into App.axaml's
  Application.DataTemplates (listed before the ViewLocator catch-all; ViewLocator only matches
  IDockable, so it never intercepts field VMs). Build clean, 338/339 green, smoke OK.
- **CLI binary refresh — deferred (stays in Backlog):** refreshing the committed tools\SWLOR.CLI
  binary to a --no-prompt build so PackService's fallback can be dropped means committing binaries
  (repo bloat + runtime-dep risk) to replace a path that already works; better done deliberately by
  the maintainer.
- **Gate feedback round 1 (2026-07-21):** (3) field rendering PASSED. (1) night lighting worked
  but a touch bright — lowered floors AmbientLightFloor 0.30→0.25, DiffuseLightFloor 0.25→0.20.
  (2) largest area orbited but was a touch laggy — added two safe perf wins: **per-tile frustum
  culling** (conservative per-tile AABB vs the 8 clip-space corners of view*projection; skips tiles
  fully off-screen, big help when zoomed/panned into a region) and a **raw-texture-name memo**
  (BindMeshTexture no longer re-runs MaterialResolver string resolution on every one of ~2500
  per-frame draws). Build clean, 338/339 green, smoke OK.
- **Gate re-test PASSED (2026-07-21):** (1) night lighting looks good; (2) pw_ar_czarmrange is
  "silky smooth" on orbit/zoom/pan — culling + texture memo were enough; instanced tile rendering
  was NOT needed (stays a backlog option if a future area stresses it). **WP6.2 accepted; PHASE 6
  COMPLETE.** Deferred items remain in the backlog: per-tile point/main lights; CLI binary refresh
  + PackService fallback drop. WP6.1's elevated-tile placement-Z check is still pending tile editing
  (WP7.3).
## WP7.1 — done — 2026-07-21 — Tile adjacency corpus
- Tier: Low, done inline as Lead — the subtle part (the orientation rotation convention + the
  "is a mismatch a model bug or a real exception?" judgment) needed the corpus as oracle and must
  not tempt weakening the assertion; the leftover was mechanical.
- Evidence-first (systematic-debugging): a throwaway probe ran a candidate orientation model against
  all 438 areas BEFORE any production code. It nailed the model — 392,150 shared-corner comparisons,
  99.971% match. The 0.029% (112) corner exceptions are ALL the `fcx01` tileset's special "holes"
  gap terrain abutting Cobble/Cobble2. All 114 edge "mismatches" are blank-vs-crosser (a wall/doorway
  declared by one tile, absent on the neighbour), i.e. crossers only need to match when BOTH sides
  declare one. Probe deleted after use.
- Domain: `GameData\Tilesets\TileAdjacency.cs` — TileEdge/TileCorner enums; WorldCornerTerrain /
  WorldEdgeCrosser (Tile_Orientation 0-3 = CCW quarter turns, north=+Y east=+X, matching
  AreaSceneBuilder); SharedCorners/OppositeEdge adjacency primitives; CornerTerrainsMatch (exact,
  case-insensitive) and EdgeCrossersMatch (blank-tolerant). This is the reusable extraction the
  WP7.2 matcher builds on.
- Tests: TileAdjacencyTests (6 — deterministic rotation at orientation 0/1, four-turn identity,
  shared-corner pairing, both match predicates). SetRuleCorpusTests (1, the acceptance gate): every
  adjacent pair in all 438 areas is corner/edge consistent under TileAdjacency, allowing ONLY the
  documented fcx01/holes exception; asserts unexpected==0, 300k+ corners compared, allowlist live
  (100-130). Base-layer install-gated (16 areas use base-game tilesets); skips without an install.
- Verified: build clean, 345/346 green (338 prior + 7 new; 1 pre-existing skip). Domain + tests only
  (no app change), so no smoke needed. No human gate — the corpus itself is the validation.
## WP7.2 — done (engine; candidate-preference folds into WP7.3) — 2026-07-21 — Tile rule matcher
- Tier: Lead. Built on WP7.1's TileAdjacency; every rule validated against the corpus.
- Domain: `GameData\Tilesets\SetRuleMatcher.cs` (+ `TileConstraint`, `TileCandidate`):
  - `FindMatchingTiles(tileset, constraint)` — every (tileId, orientation) whose world corner
    terrains (exact, case-insensitive) and edge crossers (blank-tolerant) satisfy a
    per-corner/per-edge constraint (null = unconstrained). The irreducible solve.
  - `ConstraintFromNeighbours(tileset, col, row, placedAt)` — the corner constraint a cell inherits
    from its placed orthogonal neighbours (the grid is abstracted as a `Func<int,int,TileCandidate?>`
    so the Domain stays free of AreDocument).
  - `SolveCell(tileset, col, row, placedAt, paintedCorners?)` — neighbours + optional paint override
    → legal candidates. The method the WP7.3 paint tools will drive.
- Tests (SetRuleMatcherTests, 8): hermetic filtering (unconstrained → all×4, corner filter,
  impossible → empty, neighbour gathering, paint override) PLUS a corpus SOUNDNESS GATE — for every
  cell of every non-fcx01 corpus area, the candidate set SolveCell derives purely from placed
  neighbours always includes the tile actually there (60k+ cells; fcx01 skipped per the documented
  holes exception). Proves the matcher never excludes the correct answer given real context.
- Verified: build clean, 353/354 green (345 prior + 8; 1 pre-existing skip). Domain + tests only, no
  smoke needed. No human gate — corpus + unit tests are the validation.
- **Remaining (folds into WP7.3):** "corpus as fallback for underspecified sets" — when SolveCell
  returns many candidates, pick the corpus-preferred tile. That selection policy belongs where
  selection happens (the paint tool), so it ships with WP7.3 alongside affected-cell enumeration on a
  corner paint and transactional Tile_List regeneration.
## WP7.3 — awaiting human gate — 2026-07-21 — Paint tools + new-area wizard
- Tier: Mid. Split into an independently verifiable **Domain engine** (this checkpoint) and the
  **app UI** (next), because the UI half ends in a human gate (walk the painted area in game) while
  the engine half is fully testable headlessly. Kept inline as Lead: paint idempotency is exactly the
  subtle-correctness work a cold subagent would be tempted to weaken a test around, and the whole
  GFF/tileset model was already in context.
- Domain:
  - `GameData\Tilesets\AreaTiles.cs` — (col,row) addressing over the .are Tile_List (row-major,
    index = row*width+col, matching AreaSceneBuilder) plus in-place SetTile/SetOrientation/
    SetHeightLevel that write ONLY the changed field, so a paint is a minimal diff and undoes clean.
  - `GameData\Tilesets\TilePainter.cs` — the paint engine. `PaintTerrain` fills the clicked cell with
    a terrain (preferring a crosser-free solid tile so a dab never drops a wall into the fill) and
    re-blends the 8-neighbour ring, returning the change set (pure — the caller applies it in one
    transaction). Plus FindSolidTile / DefaultFillTerrain / FillableTerrains for the wizard + palette.
  - `GameData\Tilesets\TileUsageStatistics.cs` — the WP7.2 leftover: corpus tile-frequency per
    tileset, and RankByUsage → the tie-break the painter uses when a solved cell is underspecified,
    so auto-solved fills look hand-authored instead of picking an arbitrary legal tile.
  - `Documents\AreaTemplateFactory.cs` — new-area core: CreateTileStruct (corpus shape, __struct_id 1,
    AnimLoop 1/1/1 + zeroed light slots = the toolset default), PopulateNewArea (rewrites identity /
    tileset / dimensions and regenerates Tile_List as a solid fill; every other template field flows
    through untouched), AddAreaToModule (idempotent Mod_Area_list entry, __struct_id 6).
- **Bug found by probing, not by reasoning:** the first blend test failed with east/west neighbours
  blending correctly and north/south not blending at all. A throwaway grid dump made the asymmetry
  obvious in one run. Root cause: `SetRuleMatcher.ConstraintFromNeighbours` resolves a corner as
  `horizontal ?? vertical`, so a cell always believes its horizontal neighbour first. That is right
  over consistent corpus data (all four cells at a vertex agree, which is what the WP7.2 gate proves)
  but wrong *mid-paint*, where the grid is deliberately inconsistent for a moment: the north neighbour
  consulted its stale west neighbour and never saw the cell just painted. Fixed in TilePainter (NOT in
  SetRuleMatcher, whose `??` is validated by the WP7.2 corpus gate) with `ConstraintFromVertices`:
  a corner vertex is shared by up to four cells, so consult all of them and let a cell already decided
  **this pass** win. Because each cell solved later matches whatever was decided before it, the pass
  ends corner-consistent — which is precisely what makes a repeat paint a fixed point.
- Tests (20 new): TilePainterTests (10) — centre fills solid, orthogonal AND diagonal vertices blend
  (the diagonal assertions lock in the bug above), idempotency, repaint-existing-terrain is a no-op,
  out-of-bounds/blank/unpaintable → empty, rank tie-break, crosser-free solid preference, plus a
  **corpus fixed-point gate**: for every real SWLOR tileset, painting terrain B over a terrain-A field
  and painting again rewrites nothing (10+ tilesets exercised). AreaTilesTests (7) — row-major
  addressing, out-of-range null/no-op, minimal diff (unchanged write produces byte-identical output,
  other tile fields preserved), round-trip. AreaTemplateFactoryTests (4) — tile struct shape,
  identity/grid rewrite with template passthrough, serialization round-trip, idempotent ifo entry.
- Verified: solution builds clean (0 errors), full suite 373/374 green (353 prior + 20; 1 pre-existing
  skip), 2m01s. Engine is Domain + tests only — no app behavior yet, so no smoke needed.
- **UI half (same day):**
  - `Domain\Workspace\NewAreaWriter.cs` — the wizard's write path. Started in the app layer, then
    MOVED to Domain: it is pure file/document work with no Avalonia dependency, and the test project
    only references Domain, so leaving it app-side would have made the one genuinely risky part of
    the wizard (writing real files) untestable. The app now contributes only UI. Clones the template
    triplet under a new resref, reshapes the .are to a solid fill, registers it in module.ifo.
    Validates everything BEFORE the first write, and writes the area triplet before the module index
    so a failure leaves harmless orphans rather than an index entry pointing at a missing area.
  - `Viewport\GlAreaControl.cs` — `IsPaintActive` + `PaintPointPicked`/`PaintCancelled`. Unlike
    placement (one-shot), the brush is STICKY: it survives each dab so the user can keep painting,
    and Esc disarms it. Camera navigation is untouched — only a click (under the drag threshold)
    paints, so left-drag still pans and right/middle still orbit. The move/rotate gizmo is suppressed
    while painting so a dab landing on the selection is not swallowed.
  - `Editors\AreaEditorViewModel.cs` — brush state (tool + terrain palette from the area's own
    tileset), and `CommitPaint` mapping the clicked ground point to a grid cell and applying the tool
    as ONE .are transaction. A dab that changes nothing commits nothing, so it costs no undo step.
    Also made .are undo/redo refresh the 3D view — the .are history now carries tile paints, so
    without it undoing a paint left the viewport showing the painted tiles.
  - Corpus tile-frequency ranking is built lazily on a background thread the first time the brush is
    armed (it reads every .are in the module) and memoized per (module, tileset); until it lands,
    tie-breaks fall back to lowest tile id.
  - New-area wizard shown inline in the Module Explorer (the same overlay pattern the palette browser
    uses) rather than as a Window, so it needs no window-lifetime plumbing.
- Tests: NewAreaWriterTests (8) against a THROWAWAY temp module fixture (real template files copied
  to temp — the repo module is never written to): the created triplet loads back as a real area with
  the right dimensions/tileset/identity, every cell carries the SAME fill tile and that tile is
  verified uniform terrain (a plain walkable floor, not just a legal tile), the area is registered in
  module.ifo, a duplicate resref is rejected without touching the existing file, mixed-case resrefs
  are normalized rather than rejected, and invalid resrefs/dimensions are refused.
- Verified: solution builds clean, full suite 381/382 green (373 prior + 8; 1 pre-existing skip),
  2m09s. Launch-and-kill smoke: window came up ("SWLOR Toolset"), ran 12s stably, exited on kill with
  no leftover process.
- **Correction (2026-07-21, from gate feedback):** the New Area dialog offered a **terrain** picker
  next to the tileset, which mis-models the domain — a terrain is not a property of an area. An area
  uses as many terrains as get painted into it; presenting one at creation reads as "this is a Grass
  area". It looked especially wrong because the wizard defaults to `tms01`, the *template* tileset,
  which declares exactly ONE terrain (Grass); real tilesets are rich (`ztd01` 12, `zcn01` 19).
  - Removed the picker. The blank fill now always comes from the tileset's own `[GENERAL]`
    `Floor`/`Default` — which is precisely what those fields mean — via the existing
    `TilePainter.DefaultFillTerrain` (Floor → Default → first fillable). `NewAreaWriter.TryCreate`
    lost its `terrain` parameter, so a caller cannot express the wrong idea at all.
  - No data was affected: the terrain was never persisted on the area, only used once to pick a fill
    tile id. Nothing had to be migrated.
  - The paint side was already correct and is unchanged — `AreaEditorViewModel.TerrainBrushes` comes
    from `TilePainter.FillableTerrains`, which offers every terrain the tileset can fill a tile with.
    Verified against real data: for `ztr01`/`zcn01`/`ztd01`, ZERO declared terrains get filtered out
    and every one has a crosser-free uniform tile. Terrain selection now lives in exactly one place.
  - Test: the existing solid-fill assertion was strengthened to pin the fill to the tileset's declared
    floor, plus a new discriminating gate on `ztd01` — chosen because it declares `Floor=Desert` while
    its terrain list STARTS with `Cliff`, so an implementation that grabbed the first terrain (or
    honoured a caller's choice) would fail. It asserts that precondition too, so the test cannot
    silently stop proving anything. (On `tms01` alone the assertion would be vacuous — one terrain.)
  - Deferred, agreed as its own package: `[GROUPS]` / Aurora "Features" (multi-tile arrangements like
    `tms01`'s AntHill or `zcn01`'s 295 groups). Placing those is a capability the toolset does not
    have yet, distinct from terrain painting.
- **"Floor" is the default FILL, not "walkable ground".** Found by inspecting a real wizard-created
  area (`tib01`, filled entirely with tile 2 = uniform `Wall`). `tib01` declares `Floor=Wall` and its
  terrain list is `Wall, Room, RoomBlood, ...` — for INTERIOR tilesets the declared fill is solid rock
  and `Room` is the walkable terrain, so a new interior area is meant to start solid and have its
  rooms carved out by painting. That is exactly Aurora's behavior, so the fill is correct; what was
  wrong were code comments claiming the wizard produces "a plain walkable floor". Corrected in
  `NewAreaWriter`, `TilePainter.FindSolidTile`, and `TilePainter.DefaultFillTerrain`. Practical
  consequence for the gate: on an interior tileset you must paint `Room` before anything is walkable.
- **Corpus-count gates relaxed to a floor.** Four pre-existing gates asserted the module has EXACTLY
  438 areas (`AreaSceneBuilderTests`, `DocumentTests.Ifo`, `SetRuleCorpusTests`, `WorkspaceTests`).
  Now that the toolset can create areas, any builder-created area turns the whole suite red — as it
  did the moment the wizard was first used. Changed to `>= 438` with a comment: the module is a living
  corpus that legitimately grows, and what these assertions actually guard is that the gate enumerated
  the real corpus rather than an empty set (the substantive strength lives in `cornerCompares > 300k`,
  `unexpected == 0`, `areasUnresolved == 0`, which are untouched). Blueprint counts stay exact —
  nothing in the toolset creates blueprints yet. New areas are now *included* in these gates, so a
  painted area is itself held to the adjacency rules.
- **PENDING HUMAN GATE** — needs a person in the app + game:
  1. Module Explorer → "New Area..." (resref, name, tileset, size — no terrain picker), create a
     small area (e.g. 4x4), confirm it opens as a solid floor of the tileset's own floor terrain.
  2. In its 3D View, toggle **Paint**, pick a terrain, click tiles — the clicked tile should fill and
     its neighbours should blend. Try Rotate/Raise/Lower. Esc disarms.
  3. Save, then reopen the area and paint the same terrain on the same tile again — it should be a
     no-op (nothing marked dirty).
  4. Pack the module and **walk the new area in game**. NOTE: on an INTERIOR tileset (e.g. `tib01`)
     the area starts as solid rock by design — paint the walkable terrain (`Room` for `tib01`) first,
     and walk that. On an exterior tileset (`tms01` Grass, `ztd01` Desert) the initial fill is already
     walkable ground.
## Editor lookups — 2026-07-21 — Resolve names in dropdowns (post-WP7.3 gate feedback)
- Reported: the tileset picker showed only a code, and many pick-lists showed raw numeric ids
  (Door Type 48, placeable Appearance 1068, Base Item 516, Gender/Phenotype/Sound Set, trigger Type).
- **Root cause for three of them was not missing data — it was a silent fall-through.**
  `LookupOptionProvider.Build`'s switch had no `case` for `Placeables`, `DoorTypes`, or
  `AmbientSounds`, even though the schemas already declared those fields as `TwoDaDropdown` AND
  `PlaceableAppearanceService`/`DoorTypeService`/`SoundService` already existed. An unhandled key
  returns an empty option list, and an empty list is exactly how the editor signals "no lookup,
  degrade to a numeric box" — so a wiring omission was indistinguishable from a missing 2DA and
  nothing failed. Wired them (plus `SoundService`, which was never registered in DI at all).
- New lookups: gender, phenotype, soundset, baseitems via one generic
  `GameData\Lookups\TwoDaLookupService` (label column + optional strref column) rather than four
  near-identical service classes — those tables differ only in which columns to read.
  `DoorTypeService`/`AppearanceService` keep their own types because they expose extra columns
  (model resrefs) that the renderer uses. Trigger `Type` is a fixed engine enum (generic/trap/area
  transition), so it gets a static option list, not a 2DA.
- Only the SWLOR custom TLK is loaded (no base `dialog.tlk`), so base-game strrefs do not resolve
  and every wired table falls back to its label column. That is why the tables chosen all have
  readable labels ("shortsword", "Aasimar", "Normal", "Armoire 1"). Wiring a binary `dialog.tlk`
  reader would upgrade these to true in-game names — a worthwhile follow-up, not required here.
- Tileset picker now shows "ztd01 - [CEP] Desert". The readable name is `UnlocalizedName` from the
  .set's [GENERAL] block (`Name` is just the resref in caps and is skipped when it adds nothing).
  Uses a new header-only `SetFileParser.ParseHeader` that stops at the end of [GENERAL]: the corpus
  is 70 files / ~16 MB and the largest tileset declares 1400+ tiles, so fully parsing every file just
  to label a picker would be pure waste. The early stop is tested directly, because the terrain
  blocks that follow [GENERAL] also use `Name=` keys and a reader that ran on would report the first
  terrain's name as the tileset's.
- **Data-safety guard (`Editors\DropdownValueValidator` + `Shell\Views\ErrorDialog`).** Converting a
  numeric field to a dropdown is not free: a combo box can only select values its lookup knows, so an
  unknown id renders BLANK — the stored value becomes invisible and a user editing anything else
  could overwrite it unseen. Measured against the corpus before shipping rather than assumed:
  - **2982 of 8317 placeable blueprints (~36%)** reference appearance rows that are entirely `****`
    in placeables.2da (row 1005 has no label AND no model) — genuinely broken references.
  - 1 creature (`darthmalek001`) has SoundSetFile 954, past the last row.
  - doortypes / baseitems / phenotype / gender: zero.
  Per the decision to prevent data loss first, opening such a blueprint now aborts with a dialog
  naming each field and value; the file is not touched. Area editing and placement are unaffected
  (they do not go through the schema editor), so painting still works.
- **The measurement also caught a bug in the guard itself:** 45 creatures store SoundSetFile 65535,
  which is NWN's Word "none" marker, not a broken row. Blocking those would have been a defect in the
  check rather than in the data, so unset sentinels (255/65535/4294967295 and -1) are exempt. Zero is
  deliberately NOT exempt — row 0 is real in every wired table.
- Tests (+33): `TwoDaLookupServiceTests` (every wired table yields readable, uniquely-keyed options;
  ids are row indices; unknown table/column degrades instead of throwing; caching),
  `TilesetDisplayNameTests` (header stops at [GENERAL]; UnlocalizedName preferred; resref-only
  fallback; every hak tileset gets a label), `SchemaLookupKeyTests` (every dropdown names a DECLARED
  lookup key, plus the seven reported fields pinned by name so a schema edit cannot quietly revert
  one to a numeric box), `DropdownValueValidatorTests` (present/missing/absent/unavailable-lookup,
  all offenders reported, sentinel exemption, zero not exempt).
- Known gap: no test can prove `LookupOptionProvider` has a case for a given key, because it lives in
  the Avalonia app project which the headless test project deliberately does not reference — the same
  boundary that hid the original bug. `SchemaLookupKeyTests` covers the schema half and the provider
  carries a remark pointing at it.
## Tile fill selection — 2026-07-21 — Prefer open ground (paint gate feedback)
- Reported: a new 8x8 `tcn01` area with a little Water painted came out as a field of tall walls and
  seemingly mismatched heights.
- Examined the saved .are rather than the render. Two findings:
  - The **terrain logic was correct**. The water pool was tile 166 (all-Water) ringed by tiles 7/8/10,
    which are genuine Cobble/Water transition tiles - a properly blended shore. Every `Tile_Height`
    was 0, so nothing height-related was wrong in the data; the "stepping" was the wall geometry of
    the fill tile itself.
  - The **fill tile was wrong**. `tcn01` has 244 crosser-free all-Cobble tiles, and no corpus area
    uses `tcn01` at all - so the frequency ranking had nothing to say and the tie-break fell through
    to lowest tile id. That is id 0, `tcn01_a01_01`, whose PathNode is `B`: cobble with a building
    wall on it. All 64 tiles got one.
- Root cause in one line: **corner terrain does not describe what is BUILT on a tile.** `tcn01` ids
  92-99 are all-Cobble yet carry buildings. Matching corners is necessary but not sufficient.
- Fix: `TilePainter.SelectCandidate` now prefers tiles whose .set `PathNode` is `A` - the open,
  unobstructed layout - after the existing crosser-free preference and before the id tie-break.
  Applied as a soft preference (`Narrow` only filters when something matches), so it can never empty
  the candidate set; interior tilesets like `tib01`, whose fill terrain is solid rock with no open
  tiles, are unaffected.
- Evidence for `A` rather than a guess: across 422 hand-built areas and ~99k placed tiles, `A` is
  46.7% of everything placed - far more than any other code - and it is the dominant fill of every
  exterior tileset sampled (`tms01` id 12, `ttd01` id 69, `dgt04` id 75, `fifi` id 34, `tjsb0`).
  The tilesets whose dominant code is `T`/`I`/`H` are corridor-based interiors, where a corridor
  shape being the bulk of the area is correct - which is also why this is a preference, not a rule.
- Tests: a hermetic case (two identical all-Grass tiles differing only in PathNode - the open one
  must win over the lower id) and a real-data regression pinned to the actual failure
  (`FindSolidTile(tcn01, "Cobble")` must return a PathNode `A` tile and specifically not id 0).
- Not a bug: the Lower tool clamps at height 0, so clicking Lower on a fresh area does nothing.
  NWN tile heights are non-negative; Lower only undoes a previous Raise.
## Crosser symmetry — 2026-07-21 — Painting left half-built docks (paint gate feedback)
- Reported: with the fill now correct, painting Water into a `tcn01` cobble plaza left "holes" -
  piers jutting into open water with nothing on the far side.
- Checked the saved .are against the painter's own rules first: **0 corner mismatches**, so terrain
  placement was correct. But all 8 crosser boundaries were ONE-SIDED - `('Dock','')` or `('','Dock')`,
  never `('Dock','Dock')`. A crosser is a structure that SPANS a boundary, so one tile rendered half
  a dock and its neighbour rendered plain water: the visible hole.
- **This was a bad rule I introduced in WP7.1.** `EdgeCrossersMatch` is blank-tolerant ("match only
  required when both sides declare one"), which I derived from seeing ~114 blank-vs-crosser pairs in
  the corpus - without weighing them against how many pairs MATCHED. Measured properly this time,
  per crosser type across all hand-built areas: Corridor 3136 matched / 0 blank, Dunes, Routes,
  Slope, Trench, Road, Alley, Bridge all 100% matched, Doorway 93%. The blanks are a rare exception,
  not the rule. Worse, the blank-tolerant predicate made the WP7.1 corpus gate pass trivially, so the
  gate could never have caught this.
- Fix (`TilePainter.WithMatchingCrossers`): generation now requires each edge crosser to EXACTLY
  equal the placed neighbour's crosser on the shared edge, blank included. `EdgeCrossersMatch` stays
  blank-tolerant for VALIDATION, because the rare one-sided corpus boundaries genuinely exist and the
  WP7.1 gate must keep accepting them - liberal in what we accept, strict in what we emit. A cell
  with no candidate under the strict rule is left untouched rather than forced, so those rare
  layouts are never rewritten.
- The regression test was verified to have TEETH rather than assumed: temporarily disabling the new
  filter makes it fail with exactly the reported symptom - 8 one-sided `Dock` boundaries, the same
  count found in the user's saved area. (Worth doing, since this is the second rule in this feature
  derived from under-weighed evidence.)
## Tile placement transform — 2026-07-21 — Rotated tiles landed a full cell away (paint gate feedback)
- Reported: holes remained in the floor after painting, even with the crosser fix in.
- The saved .are was clean this time - only proper Cobble/Water tiles, no dock tiles - so the fault
  was downstream of painting. Probed the built scene instead: every model resolved (0 fallbacks) and
  every tile had full XY coverage, so nothing was missing. Transforming each tile's geometry into
  world space found the real problem:

      orientation 0 -> offset (-5,-5)     orientation 2 -> offset (+5,+5)
      orientation 1 -> offset (+5,-5)     orientation 3 -> offset (-5,+5)

  A rotated tile lands a FULL CELL from where it belongs - overlapping a neighbour and leaving its
  own cell empty. Those empty cells are the holes.
- Root cause in `AreaSceneBuilder.BuildTiles`, present since WP4.5: the transform pre-translated by
  (-TileSize/2, -TileSize/2) before rotating. That is only correct if tile models have their origin
  at a corner. NWN tile models are ORIGIN-CENTRED (measured: geometry spans -5..+5 on both axes), so
  the pre-translation rotates each tile about a corner instead of its centre. Fixed by rotating about
  the centre and translating straight to the cell centre.
- **Why it hid for three phases:** at orientation 0 the error degenerates to a uniform (-5,-5) shift
  of the entire grid, which still tiles seamlessly and looks perfectly fine. It only becomes visible
  when rotated tiles sit beside unrotated ones - which is exactly what terrain painting produces, and
  why an area that renders "correctly" for months can still be wrong. It affected every area view,
  not just painted ones: with the old transform, `anchor_entreenor` tiles (0,0) and (1,0) both land
  at X=[5,15] Y=[5,15], directly on top of each other.
- It also silently corrupted every world-space consumer of the transform - walkmesh ground raycast,
  instance placement snapping, and click-to-paint cell mapping - all of which were resolving against
  tiles drawn up to a full cell from where the grid math believed they were.
- Test: `Build_EveryTilePlacement_CoversExactlyItsOwnGridCell` transforms each tile's footprint
  corners and requires them to match the cell exactly, over 40 corpus areas; it asserts all four
  orientations are exercised so it cannot pass vacuously. Verified to have teeth - restoring the old
  transform makes it fail immediately on real corpus areas.

## Comprehensive toolset audit — 2026-07-25

- Audited the implemented browse, category, blueprint, area-instance, viewport, tile-paint, preview,
  game-data startup, validation, and workspace-lifecycle paths against their tests and persistence
  invariants.
- Terrain matching now compares absolute corner elevation (`Tile_Height` plus the oriented `.set`
  corner height), not terrain names alone. Painting and rotation therefore reject invisible height
  seams. The real-area matcher corpus gate was upgraded to include height and still admits every
  actually placed tile.
- Category recovery no longer hides understood sections from a newer read-only sidecar. Folder names
  reject the `/` pin-path separator, external sidecar deletion is detected, and a missing/malformed
  palette is retried instead of being permanently marked seeded. Failed seed saves roll back the
  imported roots and seed marker.
- Placed triggers expose width/height editing. Resizing scales an existing polygon around its centre;
  malformed or degenerate geometry is repaired to a usable rectangle.
- Preview requests now version in-flight work. Saving a blueprint, changing workspace, or clearing the
  cache invalidates both memory and disk state and prevents an older render from repopulating either.
  Late waiters also receive the blueprint type fallback when the cached result is "no artwork".
- Area and Output views detach from global display/collection notifications when removed from the
  visual tree. Catalog completion logs capture the catalog that actually started the build and report
  faults instead of announcing success against a later workspace.
- New merchant creation is withheld until `StoreList` inventory can be edited; existing merchants
  remain browsable and editable. Optional base `dialog.tlk` corruption now degrades to custom TLK
  content instead of aborting startup. Malformed creature instances no longer pass a null resref into
  the model resolver.
- Production Toolset projects build without nullable warnings; focused regression and full-suite
  results are recorded in the PR handoff.
## Trigger Properties dialog — 2026-07-26 — the .utt editor
- User direction: build the base NWN Trigger Properties dialog. A trigger blueprint is REUSABLE;
  its dimensions (`Geometry`) are drawn per placement in the area editor and are not a blueprint
  concern, and neither is the per-placement transition target (`LinkedTo`/`LinkedToFlags`). No
  Comments tab - `Comment` is written as an empty string on every trigger the editor saves.
- Chose Design A of four (see `TriggerEditorDesigns.md`): faithful modal, tab for tab, plus
  Variables promoted from the base dialog's `...` button to a tab of its own.
- Files: `Domain\Editors\Schemas\UttSchema.cs` (regrouped into the dialog's tabs in the base
  dialog's own field order; Comment dropped), app `Editors\TriggerPropertiesViewModel.cs`,
  `Editors\TriggerPropertiesTab.cs`, `Editors\FieldViewModelFactory.cs` (extracted from
  BlueprintEditorViewModel so both editors build field VMs one way),
  `Shell\Views\TriggerPropertiesDialog.axaml(.cs)`, `Editors\EditorService.cs` (routes Utt to the
  dialog after the existing lookup-representability check).
- **Commit model:** the modal owns commit, unlike every other blueprint. Field edits still run as
  one-step transactions on the session's undo stack; OK writes atomically, Cancel unwinds the whole
  stack (empty at open, so it restores the document exactly as loaded), and closing the window by
  any route goes through the same discard. Nothing reaches disk until OK. This is the cost of A
  that the study flagged: `.utt` no longer participates in Ctrl+S or shell undo.
- **Deliberately NOT built - "Update Instances".** Measured first: 46 of 188 placements of the five
  placed trigger blueprints differ from their blueprint, and 42 of those differ in `VarTable` -
  every one of the 41 exploration triggers carries its own `DISPLAY_TEXT`. A blanket push would
  erase all of them. The button ships only when it is field-scoped and previewed (Design C).
  Load/Save Script Set is also out: SWLOR script sets are C# handlers, not builder-assembled files.
- **Trap tab** exists only while `Type` is Trap (2), re-read from the document on any change to the
  Type field, because a dropdown reports through `SelectedOption` and a bare numeric box through
  `RawValue`.
- Known gaps, both flagged rather than faked: **Category** is the raw `PaletteID` byte, since the
  toolset's own category tree is a sidecar that does not use PaletteID; **Cursor** is a numeric box,
  because a partial hand-written cursor list would trip `DropdownValueValidator` and refuse to open
  any trigger holding an id the list omitted.
- Tests: `SWLOR.Toolset.Tests\TriggerPropertiesTests.cs` (9). Teeth verified - removing
  `BlankComment()` and emptying `CancelEdits()` fails exactly the two tests that guard them.
  Suite 929 passed / 1 skip / 0 failed.
- Worktree note: both submodules were uninitialized here, which alone produced 82 haks/2DA/tileset
  test failures before `git -c http.sslBackend=schannel -c protocol.file.allow=always submodule
  update --init` against the local clones.

## Trigger behavior editor — 2026-07-26 — replaces the modal shipped earlier today
- User direction: model the trigger editor on the placeable editor (tabbed instance editor whose
  Behavior tab is a picker on the left, that behavior's fields on the right, and a "what this
  behavior manages" block). Serve BOTH blueprints and placements. **Local variables are reachable
  only under Custom** - every other behavior owns its locals and exposes them as named fields.
- The modal Trigger Properties dialog from earlier today was removed wholesale
  (`TriggerPropertiesViewModel/Tab/Dialog` + its tests), per "disregard what you came up with".
- Domain, new `Editors\Triggers\`: `TriggerBehavior`, `TriggerBehaviorCatalog` (9 behaviors +
  classifier), `TriggerFieldDefinition/Kind/Storage`, `TriggerChoice`, `TriggerTagScope`,
  `TriggerManagedValue`, `TriggerValueStore`, `TriggerEditorLayout` (Basic/Advanced rows).
  Plus `Workspace\ModuleTagIndex` (tag -> area, lazy, blueprint-tag fallback) so a transition's
  destination can say which area it resolves in.
- App, new `Editors\Triggers\`: `TriggerEditorViewModel` (shared by blueprint and placement),
  `TriggerDocumentViewModel` (the .utt document tab, Revert/Save), `TriggerRowViewModel`,
  `TriggerBehaviorListItemViewModel`, `TriggerManagedRowViewModel`; view
  `Editors\Views\TriggerDocumentView.axaml` (namespace must be `Editors.Triggers` - ViewLocator
  maps by name, not by folder). New theme classes `Border.card`, `TextBlock.section`,
  `Button.behavior`.
- **One editor, two shapes.** A blueprint root and one entry of an area's TriggerList are both a
  `JsonGffStruct` with the same fields and the same VarTable, which is what lets the same view model
  serve both; the host supplies the struct and its session's edit runner.
- **Behavior swap is one undo step**: clear what the previous behavior owned, then apply the new
  one's managed values. Without the clear, a swapped-away OnEnter keeps firing in game.
- `VarTableSectionViewModel` now takes the edit runner rather than an `EditorFieldContext` - it only
  ever used `RunEdit`, and the context's document half is unreachable for a trigger placement whose
  locals live on an instance struct rather than a document root. Call sites updated.
- Tests: `TriggerBehaviorTests` (10). Two carry the design - every trigger in the corpus must
  classify as something, and a behavior swap must leave nothing of the previous one behind. Suite
  930 passed / 1 skip / 0 failed.
- **Not verified visually.** The app builds and launches, but synthetic double-clicks do not reach
  the palette tiles, so the editor was never rendered on screen. Confirmed this is the input method
  and not a regression: the same double-click fails identically on an item blueprint, which goes
  through the untouched `BlueprintEditorViewModel` path. Needs a human open of a .utt.
- **Not wired yet:** placements. The view model takes `isInstance` and a struct, so the instance
  half is built, but the area editor's trigger rows still use the generic instance detail form.
- Known gaps kept honest rather than faked: Faction and Cursor are numeric rows (a partial
  hand-written cursor list would trip `DropdownValueValidator` and refuse to open any trigger
  holding an id it omitted); Category is the raw PaletteID byte.
## WPS4.1-spike — done (gate PASSED with a documented exception) — 2026-07-26 — NCS byte-identity

Pulled forward ahead of WPS0.1 by design (see `SCRIPT-EDITOR-PLAN.md`): the one finding that could
reshape Phase S4 is whether a vendored compiler reproduces the committed `.ncs` artifacts. It does.

- **Tool:** `nwn_script_comp.exe` from neverwinter.nim release **2.1.2** (x86_64-windows,
  sha256 `B00501CC…AED1`, 25,290,706 bytes — matched the advertised size). Reports
  `neverwinter 2.1.2 (/07a475, nim 2.2.4)`. **Vendoring needs two files**, not one:
  `nwn_script_comp.exe` + `libnwnscriptcomp.dll` (the official Beamdog compiler library it wraps —
  which confirms the plan's central assumption). Not yet copied into `tools/SWLOR.CLI/`; it sits in
  the session scratchpad pending a decision to commit a binary.
- Note the existing `nwn_gff.exe`/`nwn_erf.exe` report `master (651de4)`, an **untagged** build, so
  the vendored set would not be version-uniform. Did not matter for the result below.
- **Result: 68 compiled, 0 errors, 19 skipped (no `main()`), and 65 of 68 byte-identical to the
  committed `.ncs`.**
- **The 3 that differ each differ by exactly ONE byte**, and it is the same root cause in all three:
  the float literal `1.9` is emitted as `3F F3 33 34` (= 1.9000001) where the committed artifact has
  `3F F3 33 33` (= 1.9). `[float]1.9` in .NET is `3FF33333`, so **the committed artifacts carry the
  correctly-rounded value and the modern compiler is the one that is one ULP high.** Source is
  `Module/nss/dmfi_x_emote.nss:42` — `case RACIAL_TYPE_HALFORC: fHeight = 1.9; fDistance = 0.2;`
  (the adjacent `0.2` round-trips exactly, which is why only one byte moves).
  Affected files: `dmfi_execute`, `dmfi_plychat_exe`, `dmfi_x_emote`.
- **Scope of the rounding quirk, measured not assumed:** swept every literal `0.1`–`9.9`. 88 of 90
  real float literals are emitted correctly rounded; **exactly two are one ULP high — `0.9` and
  `1.9`** (confirmed in isolation: `0.9` → `3F666667` vs correct `3F666666`). ~2%, magnitude ~1.2e-7
  relative. Functionally irrelevant, but it means a full recompile would produce a **3-file, 3-byte
  git diff**. Nothing was recompiled into `Module/ncs/` — the repo is untouched.
- **Correction to the plan — an NWN install is NOT optional for all scripts.** With `--no-keys` plus
  `nwscript-8193.37.nss` staged as `nwscript.nss`, 55 of 87 compile; **16 fail** because
  `nw_i0_generic` and friends reach base-game includes that live only in the install's KEY/BIF and
  are absent from `Module/nss`: `x0_i0_anims, x0_i0_assoc, x0_i0_behavior, x0_i0_combat,
  x0_i0_spawncond, x0_i0_stringlib, x0_i0_talent, x0_inc_generic, x0_inc_henai, x2_inc_compon,
  x2_inc_switches, x2_inc_toollib, x3_inc_horse, x3_inc_string` (14 headers). With `--root <install>`
  all 87 resolve and 0 error. So: the *editor* still needs no install (the in-repo header covers
  completion), but *compilation* of the base-AI-derived scripts does. WPS4.1/WPS4.2 must surface a
  clear "NWN install required to compile these" state rather than failing opaquely.
- **Also corrected:** 19 files have no `main()`, not the 18 inferred from "has no committed `.ncs`".
  `dmfi_dmw_inc` is an include that nonetheless has a committed 184-byte `.ncs`, so "no `.ncs`" is not
  a reliable test for "is an include" — the parser's own `main()` detection is.
- **Verdict:** the vendored compiler is the right one. WPS4.1's acceptance is amended from
  "byte-identical" to **"byte-identical except for documented float-literal ULP differences"**, with
  the 3 known files listed above as the permitted exception set. Anything beyond those 3 is a
  regression.

## WPS0.1 — done — 2026-07-26 — Script editor tab (no language smarts yet)

Phase S's foundation: `.nss` files open, edit, save and undo inside the toolset. No highlighting,
completion or compilation yet — those are WPS1.x/2.x/4.x. Ships useful on its own.

- **Dependency:** `Avalonia.AvaloniaEdit` 11.3.0 (MIT), pinned to the 11.3 line to match the
  Avalonia 11.3.17 already in the csproj. Its theme must be registered in `App.axaml`
  (`avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml`) or the control renders as a blank box;
  placed before `ToolsetTheme.axaml` so our overrides still win.
- **`MonoFont` was already in the theme** (`ToolsetTheme.axaml:46`), so the editor just consumes it.
  The plan's "embed a monospace face" item was wrong and is dropped.
- Domain (`Domain/Script/`, so it is testable — the test project references Domain only):
  - `ScriptTextDocument.cs` — the byte-fidelity model. Captures EOL style, trailing newline, BOM and
    encoding; normalises the buffer to `\n` (AvaloniaEdit works in `\n`, and mixed endings inside the
    buffer would leak into every edit) and reapplies the file's own style on save.
  - `ScriptSession.cs` — path binding, mtime-based external-change detection mirroring
    `DocumentSession.HasExternalChange`, and **derived** dirty state (compare against the saved text)
    so typing and then undoing correctly reports clean. Deliberately carries **no undo stack**:
    `DocumentSession`'s models GFF field transactions, and AvaloniaEdit's `TextDocument.UndoStack`
    already does the right thing for a character buffer.
- App: `Editors/ScriptEditorViewModel.cs` (`Document, IEditorDocument, IDocumentStatusSource`) +
  `Editors/Views/ScriptEditorView.axaml(.cs)`. The buffer is **not** data-bound — AvaloniaEdit owns
  the text, its undo stack and the caret, so a two-way binding re-sets `Text` on every keystroke and
  resets the caret to 0. The code-behind seeds the document once, clears the undo stack (otherwise
  Ctrl+Z on a freshly opened tab wipes the file to empty), and pushes changes outward.
  `StatusDetail` contributes `Ln/Col` + EOL style to the shell status bar.
- Wiring: `EditorService` gained an `Nss` branch + `_openScriptEditors` map keyed by path;
  `ModuleExplorerViewModel.CanOpenSelectedType` now admits `Nss` as well as `Area` (Dlg still
  cannot open, and its comment was corrected to say so).
- **Bug found by the corpus gate, not by reasoning:** the first draft read scripts as strict UTF-8 on
  the assumption the corpus was all ASCII. `EveryModuleScript_RoundTripsByteForByte` threw on
  **`colors_inc.nss` and `nbde_inc.nss`**, which embed raw high bytes (`0x80-0x83`, `0xA5`, `0xC6`,
  `0xF8`) *inside string literals* as NWScript colour codes — data, not text. This is the same trap
  PLAN.md records for GFF `void` fields. Fixed by detecting encoding: BOM ⇒ UTF-8, else strict UTF-8
  attempted with **Latin-1 as the fallback**, which maps 0x00-0xFF bijectively and so cannot lose a
  byte. Windows-1252 (the game's own encoding, and the compiler's default) was **rejected**: it
  leaves five byte values undefined so it cannot promise a round trip, and the high bytes here are
  colour codes rather than letters. Both encoders throw rather than substitute, so an unrepresentable
  character fails the save loudly instead of writing `?` into a real file.
- Tests (21 new): `ScriptTextDocumentTests` (13) — **byte-for-byte round trip over all 87 module
  scripts** (the gate that found the above), CRLF/LF preservation, no-trailing-newline, BOM, edited
  text written in the file's own EOL style, the two colour-code files pinned by name, a sweep proving
  every byte 0x80-0xFF survives the Latin-1 path, and empty file. `ScriptSessionTests` (8) — dirty
  derived not flagged, save-unchanged leaves the file byte-identical, MarkSaved resets the baseline,
  external write and deletion detected, reload clears dirty, CRLF input does not read as permanently
  dirty. All against a throwaway temp dir; the repo module is never written to.
- Verified: `dotnet build` clean; **full suite 941/942 green** (920 prior + 21 new, 1 pre-existing
  skip), 3m34s — including the permanent GFF round-trip corpus gate. Launch-and-kill smoke: window
  came up titled "SWLOR Toolset - SWLOR_NWN", ran 12s stably, stopped cleanly.
- **Environment note for the next session:** this worktree had neither submodule checked out.
  `External/Radoub` is required to build at all, and `SWLOR_Haks` is required for ~82 2DA/TLK/tileset
  tests that otherwise fail as false negatives. Git here has no CA bundle, so plain
  `git submodule update --init` fails on SSL; use
  `git -c http.sslBackend=schannel submodule update --init` to clone via the Windows cert store.

## WPS1.1 / 1.3 / 2.1 / 2.2 / 2.3 / 4.1 — done — 2026-07-26 — Language service, highlighting, completion, compiler

The language core and the editor intelligence built on it. Everything analytical lives in
`Domain/Script/` because the test project references Domain only.

- **WPS1.1 lexer** (`Script/Syntax/`). Total and lossless: every character lands in exactly one
  token, including whitespace, comments and unrecognised characters. Nothing throws — an
  unterminated string or block comment runs to EOF — because the editor lexes half-typed lines
  constantly and a throwing lexer would leave the buffer unhighlightable exactly while being written.
  Gate: concatenating every token's text reproduces the input byte for byte, over **all 87 module
  scripts and the 13,870-line engine header**, plus a truncation suite that cuts one file at every
  offset. Strings stop at end-of-line so one stray quote cannot colour the rest of the file.
- **WPS1.3 symbol database** (`Script/Symbols/`). Parses the engine header into functions and
  constants with per-parameter docs, and lifts the `FOO_*` constant family out of each parameter's
  documentation — the thing that makes argument-aware completion possible.
  - **Correction: the header declares 1,187 functions, not the 1,164 in the design doc.** That figure
    came from a grep whose name pattern was `[A-Za-z_]+`, which excludes digits and so missed `d2`,
    `d3`, `d6`, `d20`, `d100` and the rest of the dice helpers. Constants (6,201) were right.
  - **Bug found by the test, not by reasoning:** the family regex demanded two underscore-separated
    segments, so it matched `CREATURE_TYPE_*` but silently missed every single-segment family —
    `ABILITY_*`, `ACTION_*`, `ANIMATION_*` — leaving only 103 of the expected 150+ parameter hints.
    A third of the feature was quietly absent and everything still "worked".
  - Categories come from the `SWLOR.NWN.API/NWScript/*Functions.cs` filenames, reproducing Aurora's
    category tree and keeping it current for free (precedent: `GameCode/SourceIdScanner`).
- **WPS2.1 highlighting** (`Editors/Script/NwScriptColorizer`). Driven by the *same lexer*, not a
  separate `.xshd` grammar, so highlighting can never drift from completion. Token list is memoised
  per text — colorizing per line would otherwise be quadratic on a 13,000-line file. Palette is
  7 existing theme tokens + 2 new analogous hues; engine functions read gold against plain-ink
  locals, which is the contrast that matters when reading unfamiliar legacy NWScript.
- **WPS2.2 completion** (`Script/ScriptCompletionEngine`). Ranking lives in Domain and is tested as
  *caret position + source → expected ordered items*. Two rules carry it: an argument position whose
  parameter documents a family offers that family first (12 constants, not 6,201), and locals always
  outrank engine constants. Matching is prefix → substring → subsequence (`gnc` →
  `GetNearestCreature`). Context detection distinguishes `#include "` paths, ordinary string literals
  (**which offer nothing** — an identifier list while typing prose is pure noise), and argument
  positions, counting commas at depth 1 and resolving nested calls to the innermost. An `if (`
  condition is correctly *not* a call, because `if` lexes as a keyword rather than an identifier.
- **WPS2.3 signature help** (`Script/ScriptSignatureHelp`). Reuses the same enclosing-call walk.
- **WPS4.1 compiler wrapper** (`Script/Compile/ScriptCompiler`). Wraps the vendored
  `nwn_script_comp`; `-s` for check-without-writing, `--root` vs `--no-keys`, output parsed into
  positioned diagnostics. `RequiresGameInstall` distinguishes the 16 base-AI-derived scripts that
  cannot resolve their includes without an install from an ordinary syntax error. **The spike's
  byte-identity gate is now a permanent test**: recompiling `dmfi_unact_nam03` must reproduce its
  committed `.ncs` exactly.
- App wiring: `Workspace/ScriptLanguageService` is a singleton that parses the header **lazily on
  first use** — 13,870 lines is too much to pay per tab, and too much to pay at startup for a window
  that may never open a script. It degrades to an empty database if the header is missing, so the
  editor still opens, highlights and saves without completion. Ctrl+Space forces the list; typing an
  identifier character, `"`, `(` or `,` opens it.
- Tests (+54, 75 total for Phase S): `ScriptLexerTests` (13), `EngineSymbolDatabaseTests` (10),
  `ScriptCompletionEngineTests` (16), `ScriptSignatureHelpTests` (3), `ScriptCompilerTests` (8).
- Verified: build clean; **full suite 995/996 green** (941 prior + 54, 1 pre-existing skip), 3m11s.
  Launch-and-kill smoke passed.

### Still open in Phase S
- **WPS1.2 / 1.5** — full recursive-descent parser and binder. `ScriptOutline` currently scans the
  token stream for functions/includes/variables, which is enough for completion and an outline but
  produces no semantic diagnostics. Tier-1 squiggles (WPS2.4) depend on this.
- **WPS2.5** — go-to-definition, find-references, rename, outline pane UI.
- **WPS3.1-3.3** — Script Reference panel, script-slot pickers, reverse references.
- **WPS4.2** — compile-on-save, Build All Scripts, the stale-`.ncs` validation rule. The Domain half
  (the compiler wrapper) is done; the menu/toolbar commands and staleness rule are not.

## WPS1.2 / 1.4 / 1.5 / 2.4 / 2.5 / 3.1 / 3.2 / 3.3 / 4.2 — done — 2026-07-26 — PHASE S COMPLETE

The analysis layer, the navigation UI, the Aurora-parity browser, and compilation wired to the shell.

- **WPS1.4 include graph** (`Script/ScriptIncludeGraph`) — both directions, cycle-tolerant with the
  compiler's own depth cap (16). Cycles terminate rather than throw: NWScript guards with a depth cap
  instead of forbidding them, and a graph that threw would be useless on the file being fixed.
- **WPS1.2/1.5 analysis** (`Script/ScriptAnalyzer`) — bracket balance, unterminated literals,
  duplicate definitions, too-many-arguments against the engine header. **Conservative by
  construction**; the gate is zero findings across all 87 known-good scripts. Deliberately *not*
  implemented: unknown-identifier reporting. An identifier can come from any include and this pass
  does not resolve include contents, so flagging them would light up every legacy file. Too-few
  arguments is likewise silent — a short call is usually a half-typed one.
  - **Bug the gate caught, not reasoning:** the lexer treated `\"` as a C-style escape. **NWScript has
    no escape sequences at all** — `return "/\/\\";` in `dmfi_plychat_exe.nss` is ASCII art, and the
    escape handling ran past its closing quote, swallowing the rest of the file as one string. Fixed,
    and pinned by a test plus a corpus check that no string token spans a line.
- **WPS2.4 squiggles + Problems** — `DiagnosticSquiggleRenderer` draws wavy underlines; a new
  Problems tool joins Output and Validation in the bottom dock. Every row carries an
  `editor`/`compiler` tag — the visible half of the two-tier rule; without it a disagreement between
  the two reads as a compiler bug. Analysis is debounced 250 ms: squiggles that appear mid-word while
  the author is still typing the name are worse than squiggles a moment later.
- **WPS2.5 navigation** — outline strip under the code (no room for a fourth column, and NWScript
  files are short and function-dense), F12 go-to-definition following includes, Shift+F12 references,
  F2 rename, Ctrl+/ comment toggle, and brace/comment folding. **Folding and rename both run off the
  token stream, not raw text**: a brace inside a string cannot open a phantom fold, and rename cannot
  corrupt `GetLocalInt(oPC, "nCount")` — legacy scripts are full of names that are both a local and a
  string key. Rename applies as one document replace so it is a single Ctrl+Z.
- **WPS3.1 Script Reference** — categorised browser tabbed beside the Palette in `PaletteDock`,
  auto-activated when a script tab takes focus via the existing `ActiveDocumentChanged` hook. Search
  filters *within* categories rather than flattening, keeping Aurora's mental model. Insert at cursor
  writes a call skeleton with required parameters, not a bare name.
- **WPS3.2 slot picker** — redeems `EditorKind.ScriptSlot`'s "picker in a later package". Every script
  field gains `...` and `Open`, and **warns when the slot names a script that does not exist** — live
  and otherwise invisible across the 2,250 module resources that name one. Includes are labelled, not
  hidden. A slot naming a sourceless `.ncs` is still valid: 154 committed artifacts have no `.nss`.
- **WPS3.3 reverse references** (`Script/ScriptUsageIndex`) — which blueprints/areas name each script,
  found by field-name convention (`Script*`/`On*` resref fields) rather than a hardcoded per-type
  list that would silently miss new slots. Built once in the background on first picker use.
- **WPS4.2 compile** — Compile Script (F7), Build All Scripts and Check Script Staleness join the
  **Build** menu beside Pack Module; one Compile button in the quick-access bar's left group, which
  that toolbar reserves for what changes the module. Compile-on-save per the locked decision, async
  and non-blocking. Staleness reports to **Validation**, not Problems: the code is fine, the artifact
  is not.
- Tests (+34): `ScriptAnalyzerTests` (11), `ScriptIncludeGraphTests` (5), `ScriptStalenessScannerTests`
  (6), `ScriptNavigationTests` (12 incl. the rename-ignores-strings case).
- Verified: build clean; **full suite 1031/1032 green** (1 pre-existing skip), 3m06s; launch-and-kill
  smoke passed; `Module/` untouched throughout.

### Phase S is complete. Deliberately not built
- A full recursive-descent parser and type-checking binder. `ScriptOutline` token-scans for
  declarations, which is enough for completion, outline, navigation and the checks above. A real
  binder would enable unknown-identifier and type-mismatch diagnostics — the one thing tier 1 still
  cannot do — and is the obvious next package if that is wanted.
- Debugging/breakpoints, `.dlg` editing, and decompiling the 154 sourceless `.ncs`. All out of scope
  by the plan's own Scope section.

## WPS1.5-binder + gap closure — done — 2026-07-26 — Unknown identifiers, compiler tier, reverse refs

Closes the three items left open when Phase S was first marked complete. Two of them were seams the
previous entry **overstated as done**; that is corrected here.

- **Compiler diagnostics now reach Problems.** `ScriptCompileService.CheckAsync` existed but had no
  callers, so `ScriptDiagnosticSource.Compiler` and the `[compiler]` tag rendered in the Problems UI
  could never appear — compiler errors only ever went to Output. Now `CompileAsync` returns a
  `CompileOutcome` carrying positioned diagnostics and raises `DiagnosticsProduced`, and the shell
  routes it to the panel, focusing Problems when a compile errors.
  - `ProblemsViewModel.SetDiagnostics` is now **per-tier**. The two arrive on completely different
    schedules — the editor re-analyses on every idle keystroke, the compiler only on save or F7 — so
    a single replace-all would have wiped the compiler's findings a quarter-second after they
    appeared. `ProblemsViewModelTests` pins exactly that regression.
  - Compiler findings underline the reported line's trimmed extent, since the compiler gives a line
    but no column. Findings naming a *different* file (an error inside an include) get zero length,
    which the squiggle renderer skips; they still list in Problems where the filename is visible.
  - An include is now compile-*checked* on save even though it produces no artifact, so a syntax
    error in a header is reported where it was made rather than only in whichever dependent is
    rebuilt next.
- **Reverse references have a UI.** `ScriptUsageIndex.UsagesOf` was built and tested but only its
  *counts* were surfaced (in the slot picker). WPS3.3 was written up as done on that basis, which
  overclaimed. The script editor now has a **Used by...** action listing every blueprint/area and the
  slot that names it, grouped by resource type, with a count beside the outline header.
- **WPS1.5 binder — unknown-identifier reporting** (`Script/ScriptBinder`). The earlier analyzer
  deliberately never reported unknown names because nothing resolved include contents. With the
  include graph in place the scope is knowable, so the check is now possible — and is gated on the
  one condition that makes it safe: **if any include fails to resolve, the scope is marked incomplete
  and nothing is reported at all.** 16 of the 87 scripts include base-game headers that live only in
  an NWN install, so an incomplete scope is the normal case for a builder without one; reporting
  there would mean hundreds of false errors. Also skipped: struct member access (needs a type model),
  and unknown SCREAMING_CASE (module includes define their own constants, and casing is the only
  signal). One report per name — a variable used ten times is one mistake, not ten.
  - Runs only when the structural checks found nothing: past an unbalanced bracket the token stream
    is not trustworthy, and one structural error should not spray name errors after it.
  - **Bug the corpus gate caught, not reasoning:** `int i, iBegin, iEnd;` in `dmfi_arrays_inc.nss`.
    `ScriptOutline` recorded only the first declarator, so the other two looked undefined. Multi-name
    declarations (with and without initialisers) are common in these legacy scripts. Fixed and pinned.
- Tests (+21): `ScriptBinderTests` (13, incl. a corpus gate that **no module script produces a false
  unknown-identifier** — either everything resolves or the binder stays quiet), `ProblemsViewModelTests` (8).
- Verified: build clean; **full suite 1052/1053 green** (1 pre-existing skip), 3m27s; launch-and-kill
  smoke passed; `Module/` untouched.

### Remaining, and genuinely so
Nothing outstanding from the plan. Not built, and not planned: full expression-level **type
checking** (argument type mismatches beyond arity, assignment compatibility). It needs a real
expression tree and type inference, and its false-positive risk on legacy code is exactly what the
conservatism rule exists to avoid — a binder that resolves *names* is the safe 80%. Also out of scope
throughout: debugging/breakpoints, `.dlg` editing, and decompiling the 154 sourceless `.ncs`.

## Fix — 2026-07-28 — The script editor rendered a "Not Found" placeholder

**Reported by the user: opening a script showed nothing.** It was a real shipping defect, and the
verification used throughout Phase S could not have caught it.

- **Cause.** `ScriptEditorView` declared namespace `SWLOR.Toolset.Editors.Views`, matching the folder
  it lives in. The existing editor views live in that *same folder* but deliberately declare the
  parent namespace `SWLOR.Toolset.Editors` — which is what `ViewLocator`'s convention expects, since
  it only rewrites `.ViewModels`/`.Panels` → `.Views` and never *appends* `.Views`. So the locator
  looked for `SWLOR.Toolset.Editors.ScriptEditorView`, found nothing, and returned its
  `"Not Found: …"` TextBlock. Fixed by moving the view to the parent namespace.
- **Why nothing caught it.** The build was clean — this is a runtime reflection lookup, not a
  compile-time reference. Every Phase S check passed: 1052 unit tests (none construct a control) and
  a launch-and-kill smoke test, which proves *a window appeared*, not that any particular tab
  renders. I had never once opened a script in the running app.
- **Guards added, both verified to have teeth:**
  - `ViewLocatorTests` — reflects over every `IDockable` view model and asserts the convention
    resolves a real `Control` with a parameterless constructor. Confirmed by restoring the old
    namespace: build stays at 0 errors and the test fails immediately, naming the exact missing type.
  - `ScriptEditorViewRenderTests` — new `Avalonia.Headless.NUnit` harness (version-matched to the
    Avalonia 11.3.17 pin) that boots the real `App` and renders controls without a display. Asserts
    the locator builds a `ScriptEditorView` rather than a TextBlock, that the XAML loads and the
    `TextEditor` exists, that binding a real module script puts its text in the buffer with `\n`
    endings, that a fresh tab has nothing to undo, and that **every** dockable view constructs
    without throwing.
  - `ViewLocator.ResolveViewType`/`ResolveViewTypeName` extracted as public statics so the convention
    is assertable without instantiating controls, with the "never appends .Views" trap documented on
    the method itself.
- Verified: build clean; **full suite 1060/1061 green** (1 pre-existing skip), 3m33s.
- **Lesson for later packages:** a launch-and-kill smoke test is not evidence that a feature works.
  Any new docked view needs a headless render test; the harness is now in place for it.

## Script editor UX — 2026-07-28 — Lexicon links, context-driven compile, real hotkeys, split reference

All four from direct feedback on the shipped editor.

- **NWN Lexicon deep links** (`Domain/Script/ScriptLexicon`). The Lexicon is MediaWiki and titles its
  pages with the exact engine function name, so `ScriptFunction.Name` maps to a page one-to-one with
  no lookup table to maintain. **Linked, not bundled**, deliberately: the content is GFDL 1.1-or-later
  ("2002 onwards NWN Lexicon Group"), so a local copy means carrying its licence text and attribution
  alongside this project's GPL-3.0 — permitted as mere aggregation, but a real obligation over prose
  that goes stale. A link costs nothing and is always current. `UrlFor` refuses anything that is not a
  plausible page title so the action disables rather than landing on a "page does not exist" screen.
  Reachable from the reference panel's **Lexicon** button and **F1** on the symbol under the caret.
  - `Services/ExternalLinkService` launches it. **Only http/https**: `UseShellExecute` will happily
    start a local executable or a `file:` path, so the scheme check keeps an unvalidated string from
    becoming a way to run something from data.
- **Compile moved off the Build menu onto the document.** It acts on the file in front of you, and as
  a module-wide menu item it sat greyed out whenever no script was open. Now: a **Compile (F7)** button
  on the script tab's own header strip, plus a **Compile** item on the Scripts tab's row context menu
  so an include's dependents can be rebuilt without opening them. `Build All Scripts` and
  `Check Script Staleness` stay under Build — those genuinely are module-wide.
- **Hotkeys are real now.** (Ctrl+B replaced F7 in a follow-up; see the entry below.) F7 (compile) had been added as an `InputGesture`, which the XAML's own
  comment warns only *draws* the shortcut — the actual bindings are window `KeyBindings` registered in
  code-behind. It never fired. F7 and F1 are now handled by the script editor itself so they follow
  the document and work with the buffer focused; `Ctrl+Shift+B` (Build All Scripts) is a real window
  binding beside Ctrl+S/Z/Y.
- **Compiler failures are visible in three places**, not just a panel that may be behind another tab:
  a **compile status strip on the document** (green tick or red cross, "failed to compile — see
  Problems", clickable), the **Problems** panel auto-focusing on error, and squiggles on the reported
  lines. Compile also saves first, since it reads the file from disk and unsaved work would otherwise
  silently not be built.
- **Reference panel split into Functions and Constants tabs**, each carrying its own count, using the
  same tab pattern as Module Contents. One combined tree put a "Constants" branch of 6,201 entries
  below 1,187 functions, which buried them. The filter resets on tab change: a term that matched
  functions almost never matches constants, and carrying it across made a freshly-picked tab look
  empty. The footer now reads "N of M shown" while filtering.
- Tests (+13): `ScriptLexiconTests` (6 — identifier/refusal cases, absolute https),
  `ScriptReferenceViewModelTests` (7 — tab selection, watermark, filter reset, Lexicon disabled on a
  `FOO_*` group header, insert disabled with no active script).
- **Note on the earlier "1 failing test":** it did not reproduce. A clean TRX-logged run is
  **1127 passed, 0 failed**. It was flaky under load (a full suite was running alongside other builds);
  it cannot be named because the first run's output filter discarded the detail.

## Fix — 2026-07-28 — Lexicon links used the apex host, which fails TLS for some clients

Reported from a second machine: clicking a Lexicon link gave
`SSL_ERROR_INTERNAL_ERROR_ALERT`, while the same link opened fine elsewhere.

- **Not a missing scheme.** The URL was already absolute, and the error itself proves it — a TLS
  handshake failure means the browser did negotiate HTTPS. A schemeless string would have been
  refused outright by `ExternalLinkService`, which only launches http/https.
- **The host was wrong.** `BaseUrl` was `https://nwnlexicon.com/index.php`. The apex does not behave
  like `www`: `https://nwnlexicon.com/index.php?title=…` answers **403 Forbidden** to some clients
  (observed directly), and the reporter's browser failed the handshake against it on one machine
  while another succeeded. `https://www.nwnlexicon.com/<Page>` serves the same content cleanly and
  needs no `index.php`.
- Changed to `https://www.nwnlexicon.com`, which is **also the form the rest of this repository
  already uses** for Lexicon references (`SWLOR.Game.Server/Readmes/VisualEffectSelection.md`), so
  the toolset now matches instead of inventing a second shape.
- `ScriptLexiconTests.UsesTheWwwHostAndNoIndexPhp` pins the exact host and asserts `index.php` is
  absent, so dropping back to the apex fails a test rather than a builder's browser.
- The previous commit's URL logging is what made this diagnosable at all: the Output panel now prints
  the full link, so "which URL failed" is answerable without reading source.

## Change — 2026-07-28 — Ctrl+B compiles the active script (replaces F7)

Direct request: F7 was the wrong key. **Ctrl+B now compiles the active script, and F7 is gone** — not
kept as an alias, since two shortcuts for one action is just something else to document.

- Bound in **two places on purpose, with no risk of firing twice**: the script editor's own key handler
  (so it works with the buffer focused) marks the key handled, which stops the window `KeyBinding`
  running it again. The window binding covers the case where focus is in the explorer or the reference
  panel — a document-only binding would have made Ctrl+B dead in exactly the situation where reaching
  for it is most natural.
- That window binding needs a shell command, so `CompileActiveScriptCommand` exists again. It is
  **deliberately not in any menu**: it forwards to the active document's own Compile, which still owns
  the save-then-compile sequence and the status strip. The earlier objection was to compiling living
  *in the Build menu*, not to a command existing.
- `CanExecute` is re-evaluated from `NotifyActiveEditorCommandsChanged`, so Ctrl+B goes dead while a
  compile is already running and comes back when it finishes.
- `Ctrl+Shift+B` still builds every script, which pairs the way Visual Studio's does.
- Button relabelled `Compile (Ctrl+B)`. Stale F7 references purged from comments and doc strings too,
  so nothing tells a future reader the wrong key.
## Placeable editor - 2026-07-26 - Tabs, behaviors, model grid

- The placeable editor is now four tabs (Basic / Appearance / Behavior / Advanced) with a fifth,
  Variables, that appears only when it has something to hold. Every other blueprint type still
  renders as one page: `FieldGroup.Tab` defaults to blank, and the view hides the strip below two
  tabs.
- **Behavior replaces the script slots as the primary surface.** `Domain\Placeables\` declares each
  behavior once - script slots, required flags, typed variables, owner file - and that declaration
  drives the list, the fields, what apply writes, and what a switch clears. Derived from the corpus
  rather than invented: 94% of the 8,355 blueprints set no script at all, and the 488 that do use
  only 77 distinct script sets, of which the top twenty cover 88%. Detection is deliberately liberal
  (an extra `plc_death` on a scavenge point is still a scavenge point) and a behavior is never
  stored - it is re-derived on open, so an untouched file round-trips byte-identical.
- **Appearance is no longer a dropdown, and that fixes a live defect.** As a `TwoDaDropdown` it made
  `DropdownValueValidator` refuse to open the 2,982 blueprints whose appearance row is blank in
  placeables.2da, because a combo box cannot represent a value it has no option for. The Appearance
  tab is a searchable model grid over `PlaceableModelCatalog`, which keeps the rows
  `PlaceableAppearanceService` drops for having no label - 15,761 of the 24,304 rows carrying a model
  have none, so tiles are pictures and the caption falls back to the model resref. An unknown stored
  row stays selected and is marked instead of blocking.
- **Grid performance is inherited from the palette, not reinvented.** Previews go through
  `ThumbnailService.RequestTileAsync` (model-resref keyed, bounded MRU + disk PNG + shared render
  pool), and the grid is paged at 200 with a Load more, because the palette's own grid is a WrapPanel
  in a ListBox and does not virtualize - its speed comes from never holding more than a few hundred
  tiles.
- **The 3D preview reuses `GlAreaControl`** via a one-instance `AreaScene` rather than a second GL
  path, so orbit/pan/zoom, lighting, textures and the model cache all come for free.
- Value pickers read the game code: `GameCodeIndex` grew loot table ids (same `_builder.Create("ID")`
  shape `SourceIdScanner` already reads for quests and spawn tables), `DialogBase` subclass names,
  `SkillType` and `VisualEffect`. `ModuleTagIndex` and `PlaceableAppearanceUsageIndex` scan the module
  once per session on a background thread; both report whether they were actually built, because an
  empty index must never read as "this destination does not exist".
- Cut from the UI on measured evidence, all still written back verbatim: trap fields (0 trapped
  blueprints, 2 trapped instances), saving throws (8,353 of 8,355 carry Aurora's 16/0/0), hardness
  (8,199 carry the default 5), portrait (6,295 have none), body bag (0 across all 98,856 instances),
  faction (two values cover 8,329), lock fields (2 locked blueprints, 63 locked instances), the
  legacy .dlg conversation slot, and the Comments tab - whose text survives, since 1,677 blueprints
  carry import attribution in it.
- Tests (+13): `PlaceableBehaviorTests` covers catalog shape, detection over the whole corpus,
  base-game chair aliases, apply/clear, hand-edited slots left alone, and an apply-then-undo
  byte-identical gate. Two existing tests were updated with their reasoning recorded rather than
  deleted: the pinned placeable Appearance dropdown became a test that the schema must NOT declare
  one, and the validator's "would be blocked" case became "is no longer blocked" plus a door case
  that still exercises the guard.
- Known gap: this worktree has SWLOR_Haks uninitialized, so the 2DA/tileset/model-dependent suites
  fail for that reason alone. Everything schema-, editing- and behavior-related is green.
