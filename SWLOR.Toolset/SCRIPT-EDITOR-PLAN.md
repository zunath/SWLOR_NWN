# SWLOR Toolset — NWScript Editor

> Companion to `PLAN.md` (the area/blueprint editor plan, Phases 0–7, complete).
> This is **Phase S** — a self-contained addition, sequenced after WP7.3's human gate.

> **STATUS: Phase S is complete (2026-07-26).** Every work package below is implemented and tested;
> per-package detail, corrections and findings live in `WORKLOG.md`. Two things were deliberately
> left out and are recorded there: a full recursive-descent parser with a type-checking binder (which
> would enable unknown-identifier and type-mismatch diagnostics — the one thing tier 1 still cannot
> do), and the out-of-scope items named under Scope below.
>
> Three figures in this document were measured wrong when it was written and are corrected in
> `WORKLOG.md`: the header declares **1,187** functions, not 1,164 (the original grep excluded digits
> and missed `d2`/`d6`/`d20`/`d100`); `MonoFont` already existed, so no font work was needed; and an
> NWN install **is** required to compile 16 of the 87 scripts.

> **Revision 2 — corrected against the shipped shell.** The first draft described an older layout.
> Corrected throughout: Module Contents is a **three-tab panel** (Areas / Dialogs / Scripts) with the
> count on each tab, not one combined tree, so the Scripts tab is the editor's entry point; the menu
> is `File · Edit · View · Build · Tools · Help` with **no Script menu**, so compile commands join
> `Build`; the quick-access icon bar exists and takes the Compile button; the status bar is the real
> two-slot `● StatusText · StatusDetail` strip; docks are `0.26 / 0.47 / 0.25` over a `0.20` bottom
> dock; Script Reference tabs beside Palette because `PaletteDock` holds one tool and Properties was
> deliberately removed from the default layout.
>
> **One substantive plan change:** `MonoFont` (`Cascadia Mono → Cascadia Code → Consolas`) already
> exists in `ToolsetTheme.axaml:46` and backs every resref, count and status line. The claim that a
> monospace face had to be embedded was wrong, and WPS0.1 loses that item — the editor just consumes
> the existing resource.

## Context

`PLAN.md` scoped scripts *out*: "dialogs, quests, spawns, scripts, stores — already lives in C# and
is not in scope here." That was right for the area-editor effort and is now the last real gap. The
module still contains **87 `.nss` sources and 223 compiled `.ncs`**, and they are not vestigial:
**2,250 blueprint/instance files reference `dmfi_*`, `zep_*`, `nw_*` or `d1_card*` scripts** by name
in their script slots. A builder who needs to touch one today leaves the toolset entirely.

The expectation stays as the user framed it: **C# work happens in Visual Studio.** This editor is for
NWScript only — the plumbing layer of module event scripts, includes, and legacy community content
that will never move to C#. The language service is built behind an interface so a second language
*could* be added later, but no C# support is designed, built, or implied here.

### What already exists to build on

The toolset is further along than "no script support" suggests:

| Already there | Where |
|---|---|
| `ResourceType.Nss`, `IsJsonEncoded = false`, folder/extension rules | `Domain/Workspace/ResourceType.cs:22-27` |
| New-script creation with a compilable `void main(){}` stub | `Domain/Documents/ModuleResourceTemplateFactory.cs:96-109` |
| Scripts tab in Module Contents, with its own count | `Shell/Panels/ModuleExplorerViewModel.cs`, `ExplorerTabViewModel.cs` |
| `MonoFont` — `Cascadia Mono → Cascadia Code → Consolas` | `Styles/ToolsetTheme.axaml:46` |
| Read-only script stats (line/char count, include-vs-executable heuristic) — computed on selection, but the panel is **no longer in the default layout** | `Shell/Panels/PropertiesViewModel.cs:143-154` |
| `EditorKind.ScriptSlot` — commented *"script picker in a later package"* | `Domain/Editors/EditorKind.cs:30` |
| Editor tab contract (`CanUndo/CanRedo/Undo/Redo/TrySaveAsync`) | `Editors/IEditorDocument.cs` |
| Tab open/track/activate, keyed per file | `Editors/EditorService.cs` |
| External-change detection, atomic save, output log, validation panel | `Workspace/`, `Services/` |

Two blockers, both one-liners: `ModuleExplorerViewModel.CanOpenSelectedType` admits only
`ResourceType.Area`, and `EditorService.TryOpenEditor` has no `Nss` branch.

### The asset that makes this cheap

`SWLOR.NWN.API/NWN/nwscript-8193.37.nss` — **13,870 lines, 1,164 function declarations, 6,201
constants** — is already in the repo and version-matched to the `NWN.Core`/`NWN.Native` 8193.37
packages the server runs against. It is the authoritative engine header. **No NWN install is
required to get full autocomplete**, which preserves the toolset's existing "NWN:EE install is
optional" stance (`README.md`).

Its comment format is machine-readable and consistent:

```nwscript
// Get an integer between 0 and nMaxInteger-1.
// Return value on error: 0
int Random(int nMaxInteger);
```

```nwscript
// - nFirstCriteriaType: CREATURE_TYPE_*
// - oTarget: We're trying to find the creature ... nearest to oTarget
// * Return value on error: OBJECT_INVALID
object GetNearestCreature(int nFirstCriteriaType, int nFirstCriteriaValue,
                          object oTarget=OBJECT_SELF, int nNth=1, ...);
```

Two things fall out of that, free:

1. **Rich tooltips and signature help** — the `//` block above each declaration is the doc comment,
   with `// - param:` lines already delimited per parameter.
2. **Parameter-aware constant completion.** **150 parameter doc-lines name a constant family**
   (`CREATURE_TYPE_*`, `EFFECT_TYPE_*`, …) out of **156 distinct families** in the header. When the
   caret sits in argument 1 of `GetNearestCreature`, we can offer *exactly* the `CREATURE_TYPE_*`
   constants instead of all 6,201. **The Aurora toolset never did this.** Default values in the
   signature (`object oTarget=OBJECT_SELF`) give a second, independent hint.

Category grouping for the Aurora-style function browser also comes free: `SWLOR.NWN.API/NWScript/`
has **236 C# files** that mirror the header's sections by filename — `CoreFunctions.cs`,
`ActionFunctions.cs`, `EffectFunctions.cs`, `ItemFunctions.cs`, … Mapping *function name → declaring
file* yields Aurora's category tree and stays current automatically. Precedent for scanning C#
source this way already exists in `Domain/GameData/GameCode/SourceIdScanner.cs`.

---

## The compilation problem (read this before the rest)

This is the part that makes the feature real rather than cosmetic, and it is the one place I need a
decision from you.

**`SWLOR.CLI/ModulePacker.cs` never compiles anything.** It copies `./nss/` and `./ncs/` verbatim
into the packing directory and hands them to `nwn_erf.exe`:

```csharp
var scriptFiles = Directory.GetFiles("./ncs/").Union(Directory.GetFiles("./nss/")).ToList();
```

So the `.ncs` files are **pre-built artifacts checked into git**, and NWN runs the `.ncs`, not the
`.nss`. The consequence: *editing a `.nss` in a text editor changes nothing in game.* The current
toolset is honest about this — creating a script prints "It must be compiled to .ncs by the build
before the game will run it" — but that build step does not exist anywhere in the repo. There is no
`nwnsc`, no `nwn_script_comp`, no compiler reference of any kind.

The data confirms the model is otherwise clean: of the 87 sources, the **18 with no matching `.ncs`
are all `_inc` include headers** (`colors_inc`, `dmfi_db_inc`, `zep_inc_main`, …), which correctly
produce no output. Every genuine entry-point script has its compiled artifact committed.

**An editor without a compiler would be a trap** — it would make edits feel effective while silently
shipping stale bytecode. So compilation is in scope, and I recommend:

> **Vendor `nwn_script_comp` from neverwinter.nim into `tools/SWLOR.CLI/`.**

It is the same toolchain, same author (niv), same license, and same distribution the repo *already*
vendors `nwn_gff.exe` and `nwn_erf.exe` from — so it introduces no new trust, licensing, or
provenance decision. It wraps the **official Beamdog compiler library**, meaning output is
bit-identical to what the game ships, and its CLI is a direct fit:

| Flag | Use here |
|---|---|
| `-s` | **Simulate: compile but write no file** — exactly our "check without building" mode |
| `-c <spec>… -d DIR` | Batch-compile `Module/nss/` → `Module/ncs/` |
| `-y` | Continue on error (batch) |
| `-j N` | Parallel |
| `-g` | Emit `.ndb` debug symbols (we will **not**; not currently committed) |
| `--max-include-depth=16` | Our include-graph walker mirrors this limit |

A source file's own directory is auto-added to the include search path, and `--dirs` takes additional
ones — flags now **verified empirically by the spike**, not guessed.

**Corrected by the spike:** staging `nwscript-8193.37.nss` as `nwscript.nss` with `--no-keys`
compiles **55 of 87** scripts with no NWN install. It does **not** cover all of them: `nw_i0_generic`
and the scripts derived from it include 14 base-game headers (`x0_i0_*`, `x2_inc_*`, `x3_inc_*`) that
live only in the install's KEY/BIF, so 16 scripts need `--root <install>`. The editor's own
intelligence still needs no install — the in-repo header carries every engine function and constant —
but compilation partially does, and the UI must say which scripts are blocked and why rather than
failing opaquely.

**Rejected: writing a compiler in C#.** Codegen must match the official compiler exactly or scripts
misbehave in ways that surface as gameplay bugs, not build errors. Enormous risk, no upside.

### The rule that follows

**Our parser is never the source of truth for output.** Diagnostics are two-tier:

- **Tier 1 — our language service.** Runs on idle (~200 ms debounce), never blocks, powers squiggles,
  completion, and navigation. Deliberately *conservative*: it reports only what it is certain of, so
  a gap in our parser degrades to "no squiggle," never to a false error on valid code.
- **Tier 2 — the real compiler.** Runs on save / on demand. Authoritative. Its errors win and are
  labelled as compiler errors in the Problems list.

Where the two disagree, tier 2 is right by definition. This is how real IDEs are built, and it means
our parser can ship useful before it is complete.

### Stale-bytecode detection

Because `.ncs` is committed and the packer copies blindly, a `.nss` edited without recompiling ships
stale — silently. Worse, editing an include invalidates **every dependent script**, and this module
has deep include chains (`dmfi_*` alone has 10 include headers). The include graph from WPS1.4 gives
us this exactly: a validation rule flags any `.ncs` older than its `.nss` **or older than any file in
its transitive include set**. This is a real correctness win specific to this repo, and the Aurora
toolset could not do it.

---

## Locked decisions

Settled with the user on 2026-07-26. Do not re-litigate these in a work package; the reasoning is
kept so a later reader knows what was traded away.

| # | Decision | Resolution |
|---|---|---|
| 1 | **Compiler** | **Vendor `nwn_script_comp`** (neverwinter.nim) into `tools/SWLOR.CLI/`. Same author, license and distribution as the `nwn_gff.exe`/`nwn_erf.exe` already there, so no new provenance decision; wraps the official Beamdog compiler library; `-s` gives check-without-writing. `nwnsc` was the alternative — equally capable, but a separate project with its own licensing story to evaluate. Editor-only was rejected as shipping a trap. |
| 2 | **Compile on save** | **Always, async, non-blocking.** Entry-point scripts compile in the background on save; failures go to Problems and the `.ncs` is left untouched. Saving an `_inc` header triggers a staleness check of its dependents instead. Prompt-on-save was rejected: a dialog on every Ctrl+S is dismissed reflexively within a day, which is worse than either alternative. |
| 3 | **`Avalonia.AvaloniaEdit` 11.3.0** (MIT) | **Approved.** Pinned to match the Avalonia 11.3.17 already in `SWLOR.Toolset.csproj`. MIT adds no constraint — the toolset is already GPL-3.0 via Radoub. |
| 4 | **Do tier-1 diagnostics ever block save?** | **No.** Advisory only. Blocking save on our own parser's opinion is exactly the failure mode tier 2 exists to prevent. |
| 5 | **Ship `.ndb` debug symbols?** | **No** — not committed today, and it would add 200+ files to the repo. Easy to turn on later (`-g`). |
| 6 | **Scope check: `.dlg` editor?** | **Out of scope.** Conversations live in C#. Recorded so it is explicitly excluded rather than assumed. |

**Execution order:** start with the **WPS4.1 byte-identity spike** — vendor the compiler, recompile
one committed script, verify it matches byte-for-byte — *before* building anything else. It is the
one finding that would reshape Phase S4, and it is cheap to get. Everything else then proceeds in
plan order from WPS0.1.

---

## Scope

**In:** `.nss` editing; syntax highlighting; completion; signature help; go-to-definition;
find-references; outline; diagnostics; the Aurora-style function/constant browser; script-slot
pickers wired into the blueprint and area editors; reverse-reference lookup ("what uses this
script?"); compilation and stale-`.ncs` detection.

**Out:** C# editing (Visual Studio, per your direction — a seam is left, nothing is built);
`.dlg` editing; debugging/breakpoints; refactoring beyond rename; LSP; decompiling `.ncs`
(the 154 sourceless compiled scripts — mostly the `d1_card*` pazaak game — stay opaque and are
simply left alone).

---

## Architecture

The project's iron rule from `PLAN.md` — *"all logic lives in Domain"* — is load-bearing here,
because `SWLOR.Toolset.Tests` references **Domain only** and never constructs Avalonia controls.
Anything placed in the app project is untestable. So:

```
SWLOR.Toolset.Domain/Script/          ← headless, 100% of the language logic, fully unit-tested
  Syntax/     Lexer, Parser, SyntaxTree, SyntaxKind, Diagnostic, SourceText
  Symbols/    SymbolDatabase, FunctionSymbol, ConstantSymbol, StructSymbol, DocComment
  Engine/     NwScriptHeaderParser (nwscript-8193.37.nss), CategoryMap, SymbolCache
  Workspace/  IncludeGraph, ScriptWorkspaceIndex, ReferenceFinder
  Semantics/  Binder, SemanticModel, TypeChecker
  Services/   CompletionEngine, SignatureHelpEngine, OutlineBuilder, DefinitionResolver
  Compile/    IScriptCompiler, NwnScriptCompiler (process wrapper), CompilerDiagnosticParser
  ILanguageService  ← the seam; one implementation (NWScript) today

SWLOR.Toolset/Editors/                ← thin Avalonia glue only
  ScriptEditorViewModel.cs            Document, IEditorDocument, IDocumentStatusSource
  Views/ScriptEditorView.axaml        AvaloniaEdit TextEditor + completion/insight windows
  Script/  LexerColorizer, DiagnosticMarginRenderer, CompletionAdapter, FoldingStrategy
SWLOR.Toolset/Shell/Panels/
  ScriptReferenceViewModel.cs         the Aurora-style browser
```

Nothing in `Domain/Script/` references Avalonia. `CompletionEngine` returns plain
`CompletionItem` records; `CompletionAdapter` in the app maps them to AvaloniaEdit's
`ICompletionData`. That keeps completion *ranking and filtering* — the part that is easy to get
subtly wrong — under test.

### Undo

**Do not reuse `DocumentSession`/`UndoStack`.** Those model transactional edits over a
`JsonGffDocument` field tree; text is a different shape. AvaloniaEdit's `TextDocument.UndoStack`
already does the right thing, so `IEditorDocument.Undo/Redo` simply forwards to it. This is the one
place where deliberately *not* reusing existing infrastructure is correct.

### Byte fidelity

`PLAN.md`'s hardest-won constraint is **zero spurious git diff** on save. It applies here too, and is
easier: preserve each file's EOL style and final-newline state, never re-indent on save, never strip
trailing whitespace unless asked. The corpus is CRLF. Same discipline, simpler problem.

---

## UI

The editor is a document tab like any other — it inherits the shell's Save/Undo/Redo menu, hotkeys,
dirty markers, and close-with-unsaved prompt purely by implementing `IEditorDocument`, with no shell
changes. `ViewLocator` resolves `ScriptEditorViewModel` → `ScriptEditorView` by convention.

Drawn on the shell as it actually ships: menu, quick-access bar, `0.26 / 0.47 / 0.25` docks over a
`0.20` bottom dock, and the two-slot status bar. **Everything marked `[+]` is new; every other
element already exists.**

```
┌──────────────────────────────────────────────────────────────────────────────────────┐
│ File  Edit  View  Build  Tools  Help              Build ▸ Compile / Build All  [+]    │
├──────────────────────────────────────────────────────────────────────────────────────┤
│ [save][saveall] │ [undo][redo] │ [compile] [+] │ [shadow][fog][light]                 │
├───────────────┬──────────────────────────────────────────────┬───────────────────────┤
│MODULE CONTENTS│ ⟨ bank ⟩ ⟨ dmfi_activate • ⟩                  │ Palette │ Script Ref ⏺│
├───────────────┤──────────────────────────────────────────────┤───────────────────────┤
│ Areas  Dialogs│  17 │ #include "dmfi_init_inc"               │ ┌───────────────────┐ │
│ ┌───────────┐ │  18 │                                        │ │ GetNearest        │ │
│ │Scripts  87│ │  19 │ void dmw_CleanUp(object oMySpeaker)     │ └───────────────────┘ │
│ └───────────┘ │  20 │ {                                       │ ▾ Actions       118   │
│ [search][New] │  21 │    int nCount;                          │ ▾ Creature       94   │
│               │  22 │    DeleteLocalObject(oMySpeaker, "x");  │    GetNearestCreature │
│ ▾ DMFI     39 │  23 │    DeleteLocalObj(oMySpeaker, "y");     │    GetNearestObject   │
│  □ DMFI act • │     │    ~~~~~~~~~~~~~~                       │ ▾ Effects       147   │
│    dmfi_activ │     │    ⓧ undefined identifier                │ ▾ Constants   6,201   │
│  □ DMFI init  │  24 │ }                                       ├───────────────────────┤
│    dmfi_init_ ├──────────────────────────────────────────────┤ object GetNearest     │
│ ▸ Legacy AI 14│ OUTLINE                                       │ Creature(             │
│ ▸ Zep plac. 17│  ƒ dmw_CleanUp(object oMySpeaker)          19 │   int nFirstCriteria… │
│ ▸ Unsorted  17│  ƒ main()                                  52 │ [ Insert at cursor ]  │
├───────────────┴──────────────────────────────────────────────┴───────────────────────┤
│  Output │ Validation │ Problems 1  [+]                                                │
│   ⓧ dmfi_activate.nss(23,4)  undefined identifier 'DeleteLocalObj'          [editor]  │
├──────────────────────────────────────────────────────────────────────────────────────┤
│ ● dmfi_activate.nss — 1 error    Ln 23, Col 42 · .ncs 3 days older than source        │
└──────────────────────────────────────────────────────────────────────────────────────┘
```

**Module Contents** is a three-tab panel (Areas / Dialogs / Scripts), each tab carrying its own
count, with only one section visible at a time — so **the Scripts tab is the editor's entry point**.
Rows keep the panel's existing shape: display name left, resref right-aligned in `MonoFont`. For
scripts those two often differ, which is exactly when the second column earns its width.

**Script Reference** tabs *beside* Palette in the right dock — the same arrangement Output and
Validation already use in the bottom dock — and activates automatically when a script document takes
focus. That placement is deliberate: `PaletteDock` currently holds one tool, and the Palette lists
the front **area's** tileset, so it has nothing to offer while a script is in front. The panel itself
is the Aurora-parity ask: a category tree over all 1,164 functions and 6,201 constants, incremental
search, description pane, and **Insert at cursor** — which writes a call skeleton with parameter
placeholders, not a bare name. Functions group by the `SWLOR.NWN.API/NWScript/*Functions.cs` category
map; constants group by their `FOO_*` family.

**There is no Script menu, and none is added.** The menu is `File · Edit · View · Build · Tools ·
Help`, and `Build` currently holds only Pack Module, so **Compile Script** and **Build All Scripts**
join it there. One Compile button goes in the quick-access bar's *left* group, which that toolbar's
own comment reserves for "what changes the module".

The **status bar keeps its real two-slot shape** — a status LED, `StatusText`, then `StatusDetail` —
rather than growing segments. `StatusText` names the document and its error count; `StatusDetail`
carries cursor position and the **stale-`.ncs` state**, the one piece of state a builder in this repo
most needs and cannot otherwise see. `IDocumentStatusSource` already feeds exactly this.

### Completion behaviour

Triggered on identifier characters, `.` (struct members), and `"` inside `#include`. Ranked by
context, which is where the design earns its keep:

- **In an argument position of a known function** → that parameter's constant family first
  (`CREATURE_TYPE_*`), then type-compatible locals, then everything else. This is the feature Aurora
  lacked and the header hands us for free.
- **After `#include "`** → resolvable script resrefs, `_inc` files ranked first.
- **After `.`** → members of the resolved struct type only.
- **Otherwise** → locals and parameters in scope, then file/include symbols, then engine functions,
  then constants. Locals outrank 6,201 engine constants, always.

Substring + subsequence matching (`gnc` → `GetNearestCreature`), because NWScript names are long.

---

## Work packages

Format, tiers, and definition-of-done follow `PLAN.md` exactly. Each package: **Tier**
(Low / Mid / Lead), **Deliverables**, **Acceptance**. DoD for every package is unchanged from
`PLAN.md`: solution builds, all existing + new tests green (**including the GFF round-trip corpus
suite, which must stay green forever**), no diff outside the named files, style matches.

Phases S1 and S2 are where the risk is; S0 is deliberately shippable on its own.

### Phase S0 — A working text editor (ship this alone if you want to stop early)

**WPS0.1 — Editor tab, no language smarts.** Tier: **Mid**.
Add `Avalonia.AvaloniaEdit` 11.3.0. `ScriptEditorViewModel : Document, IEditorDocument,
IDocumentStatusSource` + `ScriptEditorView.axaml`. Load/save `.nss` as UTF-8 preserving EOL and final
newline; dirty title marker; undo/redo forwarded to `TextDocument.UndoStack`; external-change
detection through the existing `ModuleFileWatcher` + `IEditorPromptService`; cursor line/col in the
status bar via `IDocumentStatusSource.StatusDetail`. Register an `Nss` branch in
`EditorService.TryOpenEditor` with an `_openScriptEditors` map keyed by path; extend
`ModuleExplorerViewModel.CanOpenSelectedType` to admit `Nss` (it currently admits `Area` only, and
its own comment notes a script "would only ever log 'No editor available yet'"). Set the editor's
font from the theme's existing `MonoFont` resource — **no font work is required**, see below.
*Acceptance:* open, edit, save, reopen every one of the 87 module scripts; `git status` clean after
an open-and-save with no edit (byte-fidelity gate); unsaved-close prompt works; shell Ctrl+S/Z/Y act
on the tab with no shell changes.

### Phase S1 — Language core (Domain, headless — gates everything after it)

**WPS1.1 — Lexer.** Tier: **Mid**.
`SourceText`, `Token`, `SyntaxKind`, `Lexer`. Handles `//` and `/* */`, strings with escapes,
int/float/hex literals, identifiers, all operators, and the preprocessor (`#include`, `#define`).
*Acceptance:* lex all 87 module scripts **and** the 13,870-line engine header with zero errors; and a
**round-trip gate in the project's own idiom** — concatenating every token's source span reproduces
the file **byte-for-byte**. That single assertion catches essentially every lexer bug.

**WPS1.2 — Parser.** Tier: **Lead**.
Error-tolerant recursive descent → `SyntaxTree` + positioned diagnostics. NWScript's grammar is
small: function declarations/definitions, `struct` declarations and member access, global and local
variables, `if/else/while/do/for/switch/case/break/continue/return`, and the eight engine structure
types (`effect, event, location, talent, itemproperty, sqlquery, cassowary, json`). Recovery must
keep producing a usable tree for a half-typed line — completion depends on parsing broken code.
*Acceptance:* parse all 88 files with **zero diagnostics** (they are known-good, so any diagnostic is
our bug — the direct analogue of `PLAN.md`'s corpus gate); a truncation suite that parses every file
cut at 200 random points without throwing.

**WPS1.3 — Engine symbol database.** Tier: **Low**.
`NwScriptHeaderParser` over `nwscript-8193.37.nss` → functions (name, return type, ordered parameters
with types and default values), constants (name, type, value), doc comments split into summary /
per-parameter (`// - name:`) / return-on-error, and the **constant-family hint** parsed out of
`FOO_*` mentions in parameter docs. `CategoryMap` from `SWLOR.NWN.API/NWScript/*Functions.cs`
filenames. Cached to `%LOCALAPPDATA%\SWLOR.Toolset\` keyed by file hash, alongside the existing
resource-index cache.
*Acceptance:* exactly **1,164 functions and 6,201 constants**; spot-checks on `Random`,
`GetNearestCreature` (8 params, 6 with defaults), `ApplyEffectToObject`; **≥150 parameters carry a
constant-family hint**; every function resolves to a category; cold parse < 500 ms, warm < 50 ms.

**WPS1.4 — Include graph + workspace index.** Tier: **Mid**.
Resolve `#include "x"` against `Module/nss/` then the engine header; transitive closure with cycle
detection and a depth cap of 16 (matching the compiler); reverse edges (dependents) for staleness;
project-wide symbol index. Invalidated incrementally by `ModuleFileWatcher`.
*Acceptance:* the real `dmfi_*` chain resolves fully; a synthetic cycle is reported, not hung;
dependents of `dmfi_init_inc` are exactly the files that include it transitively; full index of 87
files < 1 s.

**WPS1.5 — Binder, semantic model, diagnostics.** Tier: **Lead**.
Scope chains, identifier resolution, struct member resolution, type checking with NWScript's implicit
conversions. Diagnostics: undefined identifier, wrong argument count, argument type mismatch,
duplicate/unreachable/missing-return, unresolved include, assignment-in-condition.
**Conservative by construction** — anything ambiguous produces no diagnostic.
*Acceptance:* **zero diagnostics across all 87 known-good module scripts** (false positives are the
only failure mode that matters here); a seeded-error fixture suite, one file per rule; **< 50 ms** to
re-analyse a typical script so the idle pass never stutters typing.

### Phase S2 — Editor intelligence (app glue over S1)

**WPS2.1 — Highlighting, folding, brackets.** Tier: **Low**.
Colorize from our own lexer rather than a separate `.xshd` grammar, so highlighting can never drift
from the parser. Fold functions, blocks and `/* */`; bracket matching; comment toggle; auto-indent.
Colors from the existing `Styles/ToolsetTheme.axaml` palette.

**WPS2.2 — Completion.** Tier: **Mid**. The `CompletionEngine` behaviour described under *UI* above.
*Acceptance:* ranking is unit-tested **in Domain** (caret position + source → expected ordered items),
covering the argument-position, `#include`, struct-member and locals-outrank-constants cases.

**WPS2.3 — Signature help.** Tier: **Low**. Overload insight on `(` and `,`, bolding the active
parameter, showing its doc line and default.

**WPS2.4 — Squiggles and Problems.** Tier: **Low**. Underlines + margin markers; click-to-navigate;
each entry tagged `[editor]` or `[compiler]`. Adds a **Problems** tool to `OutputDock` alongside
Output and Validation (that dock already tabs two tools, so this is a one-line `VisibleDockables`
addition plus a locator entry — no layout surgery).

**WPS2.5 — Navigation.** Tier: **Mid**. Outline pane; go-to-definition (F12, across includes and into
the engine header opened read-only); find-all-references; workspace symbol search (Ctrl+T); rename
within a file.

### Phase S3 — Aurora parity + module integration

**WPS3.1 — Script Reference panel.** Tier: **Mid**. The categorized browser, search, description
pane, and insert-at-cursor with parameter placeholders. *This is the explicit Aurora-parity
requirement.* Docks as a second tool in `PaletteDock` and auto-activates when a script document
becomes active — reusing the `ActiveDocumentChanged` hook `ToolsetDockFactory` already fires to tell
the Palette which area is in front.

**WPS3.2 — Script slot pickers.** Tier: **Low**. Redeem the `EditorKind.ScriptSlot` promise: a picker
dialog with search, plus **Open** and **Create** actions, on every script field in the blueprint and
area editors. Warn on a slot pointing at a nonexistent script — a real and currently invisible class
of bug across those 2,250 references.

**WPS3.3 — Reverse references.** Tier: **Mid**. "Used by" for the open script: every blueprint, area
and instance whose script slots name it, from the existing blueprint catalog. Aurora could not do
this, and with 2,250 references it is the difference between editing a legacy script confidently and
guessing.

### Phase S4 — Compilation

**WPS4.1 — Compiler wrapper.** Tier: **Mid**. Vendor `nwn_script_comp` into `tools/SWLOR.CLI/`;
`IScriptCompiler` + process wrapper (async, cancellable, output streamed to the Output panel, exact
resman/include flags **verified empirically, not assumed**); parse compiler output into the same
`Diagnostic` type as tier 1; stage `nwscript-8193.37.nss` as `nwscript.nss`.

> **This package's gate has already been run as a spike — it PASSED.** See the `WPS4.1-spike` entry
> in `WORKLOG.md` for full detail. Headlines: 68 compiled / 0 errors / **65 of 68 byte-identical**;
> the 3 exceptions each differ by exactly one byte, caused by the float literal `1.9` being emitted
> one ULP high (the *committed* artifact has the correctly-rounded value). Only `0.9` and `1.9` are
> affected across a `0.1`–`9.9` sweep. Vendoring requires **two** files —
> `nwn_script_comp.exe` + `libnwnscriptcomp.dll`.
>
> **An NWN install is required to compile 16 of the scripts** — `nw_i0_generic` and friends include
> 14 base-game headers (`x0_i0_*`, `x2_inc_*`, `x3_inc_*`) that exist only in the install's KEY/BIF,
> not in `Module/nss`. `--no-keys` + the staged header compiles 55 of 87. The *editor* still needs no
> install; *compilation* of those scripts does, and the UI must say so plainly rather than failing
> opaquely.

*Acceptance (amended by the spike):* all entry-point scripts compile in `-s` mode with zero errors,
and a real compile reproduces every committed `.ncs` **byte-for-byte except for the documented
float-literal ULP exceptions** — exactly `dmfi_execute`, `dmfi_plychat_exe`, `dmfi_x_emote`. Any
fourth divergent file is a regression, not an exception.

**WPS4.2 — Compile on save, Build All, staleness.** Tier: **Mid**. Compile-on-save per decision #2;
**Compile Script** and **Build All Scripts** added to the existing `Build` menu beside Pack Module,
plus one Compile button in the quick-access bar's left group (the group its own comment reserves for
"what changes the module"); a `StaleCompiledScriptRule` in the existing validation framework using
WPS1.4's reverse edges (`.ncs` older than its `.nss` *or* than any transitive include); stale scripts
marked in the Scripts tab — the marker **replaces the row's `□` glyph** rather than adding a column,
since that slot is already the row's status position — and surfaced in `StatusDetail` and before
Pack. *Acceptance:* editing `dmfi_init_inc` marks every transitive dependent stale; Build All
produces a clean tree; Pack warns when stale artifacts would ship.

---

## Sequencing and what ships when

| After | You can |
|---|---|
| **S0** | Edit and save module scripts inside the toolset. Genuinely useful alone. |
| **S1+S2** | Real IDE behaviour: highlighting, completion, signature help, diagnostics, navigation. |
| **S3** | Aurora parity, plus script-slot and reverse-reference integration Aurora never had. |
| **S4** | Compilation — the point at which editing a script actually changes the game. |

S4 is last in dependency order but is the package that makes the feature honest. If the plan gets cut
short, **cut S3 before S4**.

**WPS4.1's byte-identical `.ncs` gate is pulled forward as a standalone spike and runs first**
(decided 2026-07-26). A surprise there — committed artifacts produced by a different compiler or a
different optimisation level — is the one finding that would reshape everything after it, and it
costs almost nothing to learn. If the gate fails, stop and re-plan S4 rather than proceeding to
WPS0.1; if it passes, the rest of the phase runs in plan order.

## Risks

1. ~~**The vendored compiler may not reproduce the committed `.ncs` byte-for-byte.**~~ **RESOLVED
   2026-07-26 by the WPS4.1 spike — 65 of 68 byte-identical.** The 3 exceptions are a one-ULP float
   rounding difference on the literal `1.9`, functionally irrelevant, and are now a named exception
   set. Residual risk is small and known: a full recompile produces a 3-file, 3-byte diff, and the
   16 base-AI-derived scripts need an NWN install to build at all.
2. **Parser false positives** are the fastest way to make the editor annoying enough to abandon. The
   "zero diagnostics on 87 known-good files" gate is the specific defence, and the tier-1/tier-2
   split means being wrong is quiet rather than loud.
3. **AvaloniaEdit ↔ Avalonia version pinning.** 11.3.0 matches the current 11.3.17 pin; the toolset
   already learned this lesson dropping `Radoub.UI` to move Avalonia independently.
4. **Idle-pass latency on large files.** `nw_i0_generic` is the stress case. The < 50 ms budget in
   WPS1.5 is the guard; incremental re-lexing is the escape hatch if it is missed.
5. **Legacy scripts may not be clean.** If any of the 87 fail WPS1.2's zero-diagnostic gate, the file
   is wrong (or our grammar is) — either way it is worth knowing, and it is exactly the kind of
   finding `PLAN.md`'s corpus gates were designed to force early.

## Critical files

**Language core:** `SWLOR.NWN.API/NWN/nwscript-8193.37.nss` (the symbol database) ·
`SWLOR.NWN.API/NWScript/*Functions.cs` (categories) ·
`Domain/GameData/GameCode/SourceIdScanner.cs` (precedent for scanning C# source).

**Why compilation matters:** `SWLOR.CLI/ModulePacker.cs` (copies scripts, never compiles) ·
`tools/SWLOR.CLI/` (where the compiler binary would land, beside `nwn_gff.exe`/`nwn_erf.exe`) ·
`Domain/Validation/` (staleness rule).

**Shell surfaces this plugs into** — read these before touching UI, they are what rev 2 corrected
against: `Shell/MainWindow.axaml` (menu, quick-access bar, two-slot status bar) ·
`Shell/ToolsetDockFactory.cs` (dock proportions; `PaletteDock` holds one tool; `OutputDock` tabs two;
Properties deliberately absent) · `Shell/Views/ModuleExplorerView.axaml` +
`Shell/Panels/ExplorerTabViewModel.cs` (the three-tab panel and row shape) ·
`Styles/ToolsetTheme.axaml` (tokens, `MonoFont`, the `title`/`name`/`resref`/`count` classes) ·
`Editors/EditorService.cs` · `Editors/IEditorDocument.cs` ·
`Domain/Workspace/ResourceType.cs` · `Domain/Documents/ModuleResourceTemplateFactory.cs`.
