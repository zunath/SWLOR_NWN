# Pre-Build Authoring Checklist

Run every sweep; each must answer YES before building.

## Structure
- [ ] `GuiWindowType` enum entry added (900 range for debug windows), no duplicate value.
- [ ] Definition class implements `IGuiWindowDefinition` and lives in
      `SWLOR.Game.Server/Feature/GuiDefinition/`.
- [ ] ViewModel derives `GuiViewModelBase<TDerived, GuiPayloadBase>` and lives in
      `.../GuiDefinition/ViewModel/`.
- [ ] The root layout is ONE `window.AddStandardLayout(...)` call — no hand-rolled
      `window.AddColumn` root (rule R5).
- [ ] Every tab partial is a fixed-width borderless `Scrollbars(None)` panel.
- [ ] Partial names and element ids are `const string`s on the ViewModel, referenced
      from the definition (never string literals in two places).

## Rules sweeps
- [ ] R3: every property referenced by any `Bind*` expression is assigned in
      `Initialize` BEFORE any `WatchOnClient` call.
- [ ] Watched properties: every value the CLIENT can change (text edits, checkboxes,
      combos, sliders, toggles, color pickers) has a `WatchOnClient` call.
- [ ] R2c: no `row.SetHeight(...)` on any row containing a fixed-height button,
      image button, toggle button, checkbox, textedit, combo, slider, or progress
      bar. (Tab rows with `AddToggles` are the sanctioned exception.)
- [ ] R4: the toggles-bound tab property only calls `HandleClientChange`; the swap
      logic lives in a separate `SelectTab` method.
- [ ] R6: if the window calls `ShowModal` or `ShowInputModal` anywhere AND has tabs,
      `OnModalClosedRestore` is overridden.
- [ ] R1: every non-last `AddTable` column passes a width > 0.
- [ ] Lists: every cell of one list binds a `GuiBindingList` property, all kept the
      same length; `BindRowCount` is called; per-row click handlers use
      `NuiGetEventArrayIndex()`.
- [ ] Combo option lists (`GuiBindingList<GuiComboEntry>`) are only ever replaced
      wholesale, never mutated in place.

## API honesty
- [ ] Every `Add*/Set*/Bind*` call used exists in
      `SWLOR.Game.Server/Service/GuiService/Component/` — verified by grep, not memory.
- [ ] Event handlers are `public Action Name() => () => { ... };` (they RETURN the action).
- [ ] All data access points are `// TODO: data plumbing` stubs with placeholder values.

## Open path
- [ ] Debug chat command added to `DebuggingChatCommand.cs`
      (Admin + `AvailableToAllOnTestEnvironment`, `Gui.TogglePlayerWindow`).
