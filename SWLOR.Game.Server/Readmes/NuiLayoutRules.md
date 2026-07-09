# NUI Layout Rules for SWLOR GUI Windows

NWN:EE's NUI layouts are solved client-side by a Cassowary constraint solver. When a
layout is unsolvable the client shows `NuiSetLayout failed: The constraint can not be
satisfied` in an "Error layoutupdate" window — with **no indication of which element or
window caused it** — and the window renders empty or collapsed. These rules exist so
that authoring mistakes are caught (or at least flagged with a widget path) at server
boot instead.

Enforcement lives in:
- `Service/GuiService/GuiLayoutValidator.cs` — advisory findings printed as
  `[NUI layout warning]` console lines during `Gui.LoadWindowTemplates()` at boot.
- `Service/GuiService/Component/GuiTable.cs` (`GuiTableBuilder.Build`) — hard throw for
  fixed zero-width table columns.

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
   behavior — and hard sizes that cannot coexist fail the whole layout update.
4. **List template cells** are `[element, width, variable]` triplets. `width > 0` +
   `variable: false` = hard fixed column; `width: 0` + `variable: true` = fill column.
   Every shipping table uses one of those two combinations.

## Authoring rules

### R1 — Never declare a fixed table column without a positive width (enforced: throws)
In `AddTable`, a column is only variable when you pass `isVariable: true` **or** it is
the last declared column. Any other column resolves to fixed and must have `width > 0`.
`GuiTableBuilder.Build` throws at boot naming the offending column.

### R2 — Bound a list, or make it the last row of its column (advisory warning)
Inside a **named partial**, a `GuiList`/`AddTable` list should either:
- carry an explicit height (on the list, its row, or a wrapping `AddGroup`),
- sit inside a **scrollable** group (`Scrollbars` other than `None`) — scroll regions
  decouple their content from parent constraints, or
- be the terminal content of the partial — i.e. on the path from the partial root to
  the list, every row that belongs to a column is that column's **last** row.

Every table in `CharacterSheetDefinition`'s tab partials is terminal; `MarketBuy`'s
table-with-pagination-below shape lives in the window **root**, which is
geometry-bound. `HoloComDefinition`'s Messages tab wraps its table in a scrollable
group so a pagination row can follow it.

Known counterexamples that ship and work (`DMPlayerExamine` NotesView,
`AppearanceEditor`'s part/color lists) have unbounded non-terminal lists in partials —
so this rule is a **warning, not a hard error**, until the engine's exact failure
condition is pinned down. When a window hits a client-side layout error, check its
`[NUI layout warning]` lines first.

### R2b — Keep a partial's fixed-height content below any plausible host viewport
A named partial lands in a `scrollbars: None` host group whose viewport is whatever
the window has left over. Rows with explicit heights (and explicit-height groups)
stack up as REQUIRED constraints: if their sum exceeds the host viewport, the solve
fails — and the viewport can be much smaller than you designed for, because window
geometry is persisted per player and restored on login (`Player.WindowGeometries`).
Keep partials compressible: put tall content inside **unsized scrollable groups**
rather than fixed-height blocks. (`Gui.CreatePlayerWindows` also discards persisted
sizes under 100px, which otherwise permanently poison a window that ever rendered
broken.)

### R2c — Never give a row an explicit height equal to its buttons' height (CONFIRMED; advisory warning)
The engine's cross-axis constraint is REQUIRED-strength: `row_height >= child_height +
child margins`. Buttons (and toggle buttons) carry a nonzero **default margin**, so a
row with `SetHeight(32f)` containing a button with `SetHeight(32f)` is mathematically
unsolvable and fails the whole layout update. This was the confirmed root cause of the
HoloCom window's persistent failures — an audit found zero shipping windows with this
shape, while HoloCom had it in every partial *and* (via a debug row) the window root.

Fix: **don't set a height on rows that contain fixed-height buttons** — the row
derives its height from children + margins (this is what every shipping window does).
Exception: the `tabbar` widget (`AddToggles`) has no default margin, which is why
CharacterSheet's `tabRow.SetHeight(28)` + `AddToggles().SetHeight(28)` works. Do not
generalize from it to buttons.

### R3 — Watch a property only after it has been Set
`WatchOnClient(model => model.X)` serializes the property's **current** value. If the
property has never been assigned, its backing value is `null` and
`GuiPropertyConverter.ToJson` throws a `NullReferenceException` mid-`Initialize`,
which also aborts the rest of the window's setup. Always assign first:

```csharp
SelectedTabId = MessagesTabId;                    // Set first
WatchOnClient(model => model.TabToggleValue);     // then watch
```

### R4 — Keep toggle widgets and tab-content orchestration on separate properties
Bind the `AddToggles` widget to a plain value property synchronized through
`GuiToggleGroupSync`, and drive partial swaps from a separate selected-tab property via
`GuiTabGroup` — exactly as `CharacterSheetViewModel` does with
`TopTabId`/`BottomTabId` vs `SelectedTabId`. Binding the swap-triggering property
directly to the widget re-enters the swap logic on every client echo.

## Diagnosing a client-side layout error

1. Check boot output for `[NUI layout warning]` lines for that window.
2. Dump the real wire JSON: temporarily log `JsonDump(constructedWindow.Window)` and
   each `constructedWindow.PartialViews` entry inside `Gui.LoadWindowTemplates`
   (via `Log.Write(LogGroup.Server, ...)` so it lands in the Server log file) — then
   diff the failing window's JSON against a working window (`CharacterSheet`) rather
   than reasoning from the C# builders.
3. If geometry is suspect, temporarily log the rect in `Gui.TogglePlayerWindow`'s
   open branch, `Gui.SaveWindowGeometry`, and `GuiViewModelBase.UpdatePropertyFromClient`
   (for `Geometry`) — this exposes degenerate client pushes and persistence poisoning.
4. Bisect the failing partial in-game: define temporary partials that each isolate one
   construct, plus a temporary row of buttons that `SwapNestedPartialView` each one
   into the content slot. One rebuild identifies the culprit construct.
