# Wireframe Element → Widget Mapping

For each visual element in the wireframe, use this table to pick the builder calls and
the ViewModel property that backs it. Copyable examples live in
`SWLOR.Game.Server/Feature/GuiDefinition/DebugNuiGalleryDefinition.cs` — the "Example"
column names the method to copy from. Every `Add*/Set*/Bind*` call below exists in
`SWLOR.Game.Server/Service/GuiService/Component/`; do not invent others.

| Wireframe element | Builder calls | VM property type | Gallery example |
|---|---|---|---|
| Window title bar | `.SetTitle("...")` (or `.BindTitle`) on `CreateWindow` chain | `string` if bound | `BuildWindow` |
| Tab strip | `layout.AddTabRow(row => { row.SetHeight(28f); row.AddToggles().AddOption("A").AddOption("B").BindSelectedValue(m => m.TabToggleValue).SetWidth(116f * optionCount).SetHeight(28f); })` inside `AddStandardLayout` | `int` (toggle value) + `int` SelectedTabId | `BuildWindow` |
| Push button | `row.AddButton().SetText("...").SetHeight(32f).SetWidth(140f).BindOnClicked(m => m.OnClickX())` | `Action OnClickX()` | `AddButtonsTab` |
| Icon/image button | `row.AddButtonImage().SetImageResref("arrow_up").SetHeight(32f).SetWidth(32f).BindOnClicked(...)` | `Action` | `AddButtonsTab` |
| Toggle/latch button | `row.AddToggleButton().SetText("...").BindIsToggled(m => m.IsOn).BindOnClicked(...)` | `bool` | `AddButtonsTab` |
| Checkbox | `row.AddCheckBox().SetText("...").BindIsChecked(m => m.IsChecked).SetHeight(24f).SetWidth(180f)` — watch the property | `bool` (watched) | `AddSelectionTab` |
| Single-line input | `row.AddTextEdit().SetPlaceholder("...").BindValue(m => m.Text).SetMaxLength(50).SetHeight(32f)` — watch the property | `string` (watched) | `AddTextTab` |
| Multiline input | same + `.SetIsMultiline(true).SetHasWordWrap(true).SetHeight(80f)` | `string` (watched) | `AddTextTab` |
| Dropdown (static options) | `row.AddComboBox().AddOption("Label", 0)...BindSelectedIndex(m => m.Selection).SetHeight(32f).SetWidth(250f)` — watch the index | `int` (watched) | `AddSelectionTab` |
| Dropdown (dynamic options) | same but `.BindOptions(m => m.Options)` | `GuiBindingList<GuiComboEntry>` (replace whole object to change) + `int` | `AddSelectionTab` |
| Radio group | `row.AddOptions().SetDirection(NuiDirection.Horizontal).AddOption("A")...BindSelectedValue(m => m.Choice).SetHeight(30f).SetWidth(...)` | `int` (watched) | `AddSelectionTab` |
| Slider | `row.AddSliderInt().BindValue(m => m.Value).SetMinimum(0).SetMaximum(10).SetStepSize(1).SetHeight(32f)` (or `AddSliderFloat`) | `int`/`float` (watched) | `AddSlidersTab` |
| Progress bar | `row.AddProgressBar().BindValue(m => m.Progress).SetHeight(24f)` — value 0.0-1.0 | `float` | `AddSlidersTab` |
| Static label | `row.AddLabel().SetText("...").SetHeight(20f).SetHorizontalAlign(NuiHorizontalAlign.Left)` | — | everywhere |
| Dynamic label | `row.AddLabel().BindText(m => m.LabelText)...` | `string` | `AddButtonsTab` |
| Wrapping text block | `row.AddText().BindText(m => m.Body).SetShowBorder(true).SetScrollbars(NuiScrollbars.Auto).SetHeight(100f)` | `string` | `AddTextTab` |
| Image / portrait | `row.AddImage().SetResref("...")` or `.BindResref(m => m.Portrait)` + `.SetAspect(NuiAspect.Fit).SetWidth(...).SetHeight(...)` | `string` if bound | `AddDrawingTab` |
| List of rows | `row.AddList(template => template.AddCell(cell => cell.AddLabel().BindText(m => m.Names)...))...BindRowCount(m => m.Names).SetRowHeight(28f)` — one `GuiBindingList` per cell, all same length | `GuiBindingList<string/float/...>` | `AddListsTab` |
| Per-row button in a list | inside a cell: `cell.SetIsVariable(false); cell.AddButton().SetText("...").SetWidth(44f).BindOnClicked(m => m.OnClickRow());` — handler reads `NuiGetEventArrayIndex()` | `Action` | `AddListsTab` |
| Multi-column table w/ headers | `col.AddTable(t => t.AddColumn("HEADER", 90f, m => m.ColA, ...).AddColumn("LAST", 0f, m => m.ColB).SetRowHeight(24f))` — fixed columns MUST have width > 0 (R1) | `GuiBindingList<string>` per column | `AddListsTab` |
| Table with button column | `.AddComponentColumn("", 60f, cell => cell.AddButton()...)` + `.BindRowCount(...)` | `Action` + lists | `AddListsTab` |
| Chart | `row.AddChart().AddSlot(s => s.SetType(NuiChartType.Lines).SetLegend("...").SetColor(r,g,b).AddDataPoint(...)).SetHeight(140f)` or `.BindData(m => m.Data)` | `GuiBindingList<float>` if bound | `AddChartsTab` |
| Color swatch/picker | `row.AddColorPicker().BindSelectedColor(m => m.Picked).SetHeight(150f).SetWidth(250f)` — watch it | `GuiColor` (watched) | `AddSelectionTab` |
| Horizontal centering | `row.AddSpacer();` before and after the element | — | `AddGroupsTab` |
| Bordered panel/frame | `row.AddGroup(g => { g.SetShowBorder(true); g.AddColumn(...); })` | — | `AddGroupsTab` |
| Confirm dialog | in a handler: `ShowModal("Prompt?", onConfirm, onCancel)` — window MUST override `OnModalClosedRestore` | `Action`s | `AddModalsTab` + VM |
| Text-entry dialog | `ShowInputModal("Prompt", initial, onConfirm)`; read `ModalInputText` in onConfirm | — | VM `OnClickShowInputModal` |
| Tooltip | `.SetTooltip("...")` or `.BindTooltip(m => m.Tip)` on any widget | `string` if bound | everywhere |
| Enabled/disabled state | `.BindIsEnabled(m => m.CanDoX)` + `.SetDisabledTooltip("why")` | `bool` | `AddBindingsTab` |
| Show/hide | `.BindIsVisible(m => m.ShowX)` | `bool` | `AddBindingsTab` |

Notes:
- **Every `Bind*` expression must be a BARE property reference: `m => m.Prop`.**
  String interpolation, concatenation, arithmetic, or method calls inside a bind
  expression (e.g. `m => $"{m.Count} / {m.Max}"`) COMPILE but throw
  `ArgumentException: refers to a method, not a property` at boot, killing window
  template loading. Computed display text gets its own `string` property (e.g.
  `SetCountText`) that the ViewModel updates whenever its inputs change.
- "watched" = the client edits the value, so `Initialize` must assign it FIRST and then
  call `WatchOnClient(m => m.Prop)` (rule R3 — the framework throws if you forget).
- Widget heights: buttons/inputs 32f, labels 20-24f, checkboxes 24f. Never set a
  height on a ROW containing these (rule R2c) — only tab rows with Toggles may pair
  row and widget heights.
