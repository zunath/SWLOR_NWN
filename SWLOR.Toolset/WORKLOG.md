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
- **PENDING HUMAN GATE** — needs a person in the app + game:
  1. Module Explorer → "New Area...", create a small area (e.g. 4x4), confirm it opens.
  2. In its 3D View, toggle **Paint**, pick a terrain, click tiles — the clicked tile should fill and
     its neighbours should blend. Try Rotate/Raise/Lower. Esc disarms.
  3. Save, then reopen the area and paint the same terrain on the same tile again — it should be a
     no-op (nothing marked dirty).
  4. Pack the module and **walk the new area in game** — the floor should be solid and walkable.
