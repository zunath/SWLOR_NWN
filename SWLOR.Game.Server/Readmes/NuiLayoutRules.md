# NUI Layout Rules for SWLOR GUI Windows

NWN:EE's NUI layouts are solved client-side by a Cassowary constraint solver. When a
layout is unsolvable the client shows `NuiSetLayout failed: The constraint can not be
satisfied` in an "Error layoutupdate" window — with **no indication of which element or
window caused it** — and the window renders empty or collapsed. These rules exist so
that authoring mistakes are caught (or at least flagged with a widget path) at server
boot instead.

Every rule below states its enforcement level: **throws** (build/boot fails or the
call raises a descriptive exception), **warns** (a structured `[NUI layout warning]` Server-log entry
at boot), or **doc-only** (not statically detectable — follow the prescription).

Enforcement lives in:
- `Service/GuiService/GuiLayoutValidator.cs` — invoked from `GuiWindowBuilder.Build()`
  for every window at boot; findings are written as structured `[NUI layout warning]` entries. The
  validator flags **only shapes confirmed to fail in-game** via the DebugNuiGallery
  hazard harness (`/nuigallery`), so **zero warning lines for your window is a hard
  authoring gate — every warning is a real defect.** The bar for adding a new rule is
  a confirmed-failing hazard/probe partial in the gallery first.
- `Service/GuiService/Component/GuiTable.cs` (`GuiTableBuilder.Build`) — hard throw for
  fixed zero-width table columns (R1).
- `Service/GuiService/GuiViewModelBase.cs` (`WatchOnClient`) — hard throw with a
  descriptive message when watching a never-assigned property (R3).

All "verified in-game" stamps below come from the July 2026 DebugNuiGallery
verification sessions; the gallery keeps every confirmed-failing and verified-working
shape loadable as regression exhibits on its Hazards tab.

## How the solver sees the wrapper components

Verified against `Core/Beamdog/Nui.cs` and the engine's layout source:

1. **`group` and `list` never advertise a size to their parent.** (`Nui.Group`'s own doc
   comment: "Will not advise parent of size, so you need to let it fill a span
   (col/row) as if it was an element.") `Nui.List` emits `row_template`/`row_count`/
   `row_height` but no `width`/`height` of its own. The solver derives their extent
   entirely from context: an explicit `SetWidth`/`SetHeight`, or the space left over in
   their span.
2. **The window root is externally bounded; nested partials are not.** The root layout
   (`%%WINDOW_MAIN%%`) is swapped into the special `_window_` group, which is hard-bound
   to the window's `Geometry` rect. A named partial applied via `NuiSetGroupLayout` to
   an ordinary element (an `AddPartialView` group) has no such bound of its own.
3. **Rows/columns position children with REQUIRED-strength constraints** (sequential
   placement, `span_size >= child_size + margins` on the cross axis) while "children
   fill the span" is only a MEDIUM-strength suggestion. Hard sizes always win over fill
   behavior — and hard sizes that cannot coexist *within one row's cross axis* fail the
   whole layout update.
4. **List template cells** are `[element, width, variable]` triplets. `width > 0` +
   `variable: false` = hard fixed column; `width: 0` + `variable: true` = fill column.
   A fixed cell (`variable: false`) with no width of its own is also solvable when its
   inner element declares a positive width (e.g. a button with `SetWidth(32f)`) — and
   in-game testing showed even a fixed cell with no width anywhere renders (see
   "Verified-working shapes" below).

## Authoring rules

### R1 — Never declare a fixed table column without a positive width (throws)
In `AddTable`, a column is only variable when you pass `isVariable: true` **or** it is
the last declared column. Any other column resolves to fixed and must have `width > 0`.
`GuiTableBuilder.Build` throws at boot naming the offending column.

### R2c — Never give a row an explicit height equal to its margined children's height (warns; CONFIRMED)
The engine's cross-axis constraint is REQUIRED-strength: `row_height >= child_height +
child margins`, and **most widget families carry a nonzero default margin**. A row with
`SetHeight(32f)` containing such a widget with `SetHeight(32f)` is mathematically
unsolvable and blanks the whole window.

Margin map, verified in-game 2026-07 via the gallery's margin-map probes:

| Widget family | Default margin? | Fixed-height row + same-height child |
|---|---|---|
| Button / ButtonImage / ToggleButton | YES | **FAILS** (H1, original root cause) |
| CheckBox | YES | **FAILS** (P1) |
| TextEdit | YES | **FAILS** (P2) |
| ComboBox | YES | **FAILS** (P3) |
| SliderInt / SliderFloat | YES (float inferred: same engine widget) | **FAILS** (P4) |
| ProgressBar | YES | **FAILS** (P6) |
| Options | no | renders (P5) |
| Toggles (tabbar) | no | renders (CharacterSheet tab rows) |
| Label | untested; fixed-height labels in unheighted rows ship everywhere | — |

Fix: **don't set a height on rows that contain fixed-height margined widgets** — let
the row derive its height from children + margins. The only sanctioned equal-height
pairings are `Toggles`/`Options` (the margin-free families), or controls that
deliberately call `SetMargin(0f)` for a tightly packed grid such as Slicing tiles.
The validator reads the explicit margin and keeps those layouts warning-free.

### R3 — Assign a property before watching it (throws)
`WatchOnClient(model => model.X)` serializes the property's **current** value.
`GuiViewModelBase.WatchOnClient` now throws a descriptive `InvalidOperationException`
if the property was never assigned. Always assign first:

```csharp
TabToggleValue = 0;                               // Set first
WatchOnClient(model => model.TabToggleValue);     // then watch that property
```

History: before this guard existed, watching an unset property NRE'd mid-`Initialize`
AND left a poisoned null-valued entry that made **every subsequent reopen of the
window throw** for that player until a server restart (confirmed via gallery hazard
H6). `Bind` also now skips null-valued entries defensively during its reopen rebind.

### R4 — Keep toggle widgets and tab-content orchestration on separate properties (doc-only)
Bind the `AddToggles` widget to a plain value property synchronized through
`GuiToggleGroupSync`, and drive partial swaps from a separate selected-tab property via
`GuiTabGroup`. Binding the swap-triggering property directly to the widget re-enters
the swap logic on every client echo (client pushes re-run property setters — only the
echo push is suppressed, not the setter body).

### R5 — Use the standard root layout shape (doc-only; helper-enforced)
Only ONE root layout shape has been proven to track window geometry correctly as the
client resizes a window (CharacterSheet's shape):

```text
root column
  └── single row ("main row")
        ├── variable-width CONTENT column
        │     ├── optional fixed-height borderless Auto-scroll group holding the
        │     │   tab-bar rows (28f rows of Toggles — the margin-free pairing)
        │     └── borderless Auto-scroll group hosting AddPartialView(contentElement)
        └── side column(s) — e.g. an event log or actions rail
```

`GuiWindowBuilder` normalizes every authored window into the shared outer root shape.
For a window with tabs, swappable content, or rails, call
`GuiStandardLayout.AddStandardLayout(...)`
(`Service/GuiService/Component/GuiStandardLayout.cs`) instead of hand-rolling that
inner structure. Deviating — most notably putting tab rows as siblings of the body
row — froze the content region at a constant width regardless of window resizing.
The all-window corpus test builds every definition through this normalizer and rejects
unexpected validator findings.

Side rails may be fixed-width (`fixedWidth: 280f`) or variable (`fixedWidth: null`);
both verified working in-game 2026-07 (the gallery ships a variable-width event log).

Tab partials themselves should be **fixed-width borderless `Scrollbars(None)` panels**
(CharacterSheet uses 250–460f; the gallery uses 560f). Scrolling comes from the
content column's Auto-scroll host group, which keeps every partial compressible when
a player's persisted window geometry is small. `Gui.CreatePlayerWindows` discards
only non-positive persisted dimensions; legitimate compact HUD windows are as small
as 72x52 and must retain their saved positions.

### R6 — Re-apply the current tab partial after any modal closes (framework hook)
Closing `ShowModal`/`ShowInputModal` swaps `%%WINDOW_MAIN%%` back into the root, which
**wipes any partial applied to a nested element** — the selected tab's content
disappears. Tabbed windows must override the base-class hook:

```csharp
protected override void OnModalClosedRestore() => Tabs.Select(this, TabContentElement, SelectedTabId);
```

The hook fires after every modal close (confirm and cancel, both modal kinds).

### R7 — Partial-view element rules (doc-only; verified 2026-07)
- **Element ids are not validated server-side.** `ChangePartialView` onto a
  nonexistent element id produces a client-side `NuiSetLayout failed: element id not
  found` error and the window must be closed/reopened (gallery P13a). Keep element-id
  constants in the ViewModel and reference them from the definition.
- **Do not nest partials more than 2 deep** (window root → partial → nested slot is
  the proven maximum, used by every tabbed window). At 3 deep the innermost content
  renders and is then dropped by the parent's re-apply pass (gallery P13b).
- After a partial swap makes the whole window layout fail (e.g. loading an R2c shape),
  subsequent `NuiSetGroupLayout` calls report `element id not found` because the
  client discarded the layout — close and reopen the window.

## Verified-working shapes (do not fear these)

All verified in-game 2026-07 via DebugNuiGallery; each remains loadable on the
Hazards tab as a regression exhibit. The validator deliberately does **not** warn on
any of these:

- **Unbounded non-terminal list in a named partial** (W1; also ships in
  DMPlayerExamine NotesView, AppearanceEditor's part/color lists, Settings' Chat view).
- **Tall fixed-height stacks with no scroll wrapper** (W2: 12 × 120f groups) — the
  old "R2b" fear; keep partials compressible anyway per R5's scroll-host prescription.
- **Fixed list template cell with no width anywhere** (W3).
- **Fixed children wider than their fixed parent** (W4: two 150f buttons in a 200f
  group; P7: 150f button in a 100f group) — width conflicts clip/overflow rather than
  failing the solve. Only *cross-axis height* conflicts in rows (R2c) kill layouts.
- **Aspect + explicit width AND height on one image** (P8), **invalid image resref**
  (P8 — renders as blank), **zero dimensions** (P9), **negative dimensions** (P10).
- **Length-mismatched binding lists across one list's cells** (P11) and **empty bound
  chart/combo data** (P12) — render without failure.

## Diagnosing a client-side layout error

1. Check the Server log for `[NUI layout warning]` entries for that window. On dev/test the
   ONLY expected warnings are the gallery's six R2c canaries (enumerated in
   `DebugNuiGalleryDefinition.cs`'s header); anything else is a real defect.
2. Read the built-in wire-JSON dump: every window's root and partial JSON is logged as
   `[NUI JSON]` lines to the Server log group (`debugserver/app_logs/Server/`) on
   non-production boots, or anywhere with `SWLOR_NUI_DUMP_JSON=1`. Diff the failing
   window against a working one (`CharacterSheet`) rather than reasoning from the C#
   builders. Note: element ids for event-bound widgets are fresh GUIDs each boot —
   normalize them before diffing across boots.
3. If geometry is suspect, temporarily log the rect in `Gui.TogglePlayerWindow`'s
   open branch, `Gui.SaveWindowGeometry`, and `GuiViewModelBase.UpdatePropertyFromClient`
   (for `Geometry`) — this exposes degenerate client pushes and persistence poisoning.
4. Reproduce the suspect construct in DebugNuiGallery's hazard slot: add a probe
   partial + button (see the existing `DefineFixedRowProbe` pattern), verify in-game,
   then record the outcome here and — if it fails — promote it to a validator rule.
