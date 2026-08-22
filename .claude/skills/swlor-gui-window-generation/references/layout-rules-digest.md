# Layout Rules Digest

Full text: `SWLOR.Game.Server/Readmes/NuiLayoutRules.md`. The client solves layouts
with a constraint solver; an unsolvable layout blanks the WHOLE window with a
context-free error. These rules prevent that. **Zero `[NUI layout warning]` boot
lines for your window is a hard gate — every warning is a confirmed defect.**

## The rules

- **R1 (throws):** in `AddTable`, every fixed column needs `width > 0`. Only the last
  column (or `isVariable: true`) may be 0f.
- **R2c (warns; the window-killer):** never `row.SetHeight(N)` when the row contains a
  same-height widget with default margins — that is buttons, image buttons, toggle
  buttons, checkboxes, text edits, combos, sliders, and progress bars (all confirmed
  in-game). Let such rows derive their height from children. ONLY `AddToggles` /
  `AddOptions`, or controls explicitly using `SetMargin(0f)` for a compact grid, may
  share a fixed height with their row.
- **R3 (throws):** assign every bound property in `Initialize` BEFORE calling
  `WatchOnClient` on it. The framework throws a descriptive exception if you forget.
- **R4 (doc):** the property bound to a toggles widget and the property that drives
  tab swaps must be SEPARATE (`GuiToggleGroupSync` wires them). Client echoes re-run
  property setters; putting swap logic in the widget-bound setter causes loops.
- **R5 (doc; helper-enforced):** the window root must be the standard shape — use
  `window.AddStandardLayout(...)`. Hand-rolled roots freeze the content region at a
  constant width. Tab partials are fixed-width (250-560f) borderless
  `Scrollbars(None)` panels; scrolling comes from the standard layout's host group.
- **R6 (doc; framework hook):** tabbed windows with modals MUST override
  `protected override void OnModalClosedRestore() => Tabs.Select(this, TabContentElement, SelectedTabId);`
  or the tab content vanishes when any modal closes.
- **R7 (doc):** element ids are not validated server-side (typos = client-only error);
  never nest partials more than 2 deep (window root → partial → one nested slot).

## Verified working — do NOT avoid these (all confirmed in-game)

- Unbounded lists in partials, even with rows after them.
- Tall stacks of fixed-height groups without scroll wrappers.
- Fixed list cells with no width; fixed children wider than their fixed parent
  (width conflicts clip — only cross-axis HEIGHT conflicts in rows kill layouts).
- Aspect + explicit width AND height on an image; invalid image resrefs (blank);
  zero or negative widget dimensions; empty bound chart/combo data;
  length-mismatched list bindings (render, but keep lengths equal anyway).
