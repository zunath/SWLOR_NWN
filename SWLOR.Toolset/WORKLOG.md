# SWLOR Toolset — Work Log

Single source of truth for work-package progress. Plan:
`C:\Users\benco\.claude\plans\i-love-it-see-recursive-owl.md` (controller keeps a copy of the
approved plan; phases/packages are summarized there). One entry per work package; update the
status line in place and append details as work happens. Statuses: `pending | in-progress |
done | blocked`.

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
## WP3.3 — pending — Instance editing
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
## WP3.5 — pending — Save + pack services
## WP3.6 — pending — End-to-end daily-driver gate (human verify)
## WP4.1 — pending — GL spike
## WP4.2 — pending — Mesh/texture pipeline
## WP4.3 — pending — Model preview panes
## WP4.4 — pending — Area scene assembly
## WP4.5 — pending — Area view
## WP5.1 — pending — Picking + selection sync
## WP5.2 — pending — Gizmos + placement
## WP6.1 — pending — Walkmesh (WOK)
## WP6.2 — pending — Perf + fidelity pass
## WP7.1 — pending — Tile adjacency corpus
## WP7.2 — pending — Tile rule matcher
## WP7.3 — pending — Paint tools + new-area wizard
