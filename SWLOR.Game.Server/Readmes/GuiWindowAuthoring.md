# GUI Window Authoring Guide

How to build a new NUI window in SWLOR, end to end. Companion docs:
`NuiLayoutRules.md` (the layout rules R1–R7 referenced throughout) and the living
widget reference: the DebugNuiGallery window (`/nuigallery` in-game;
`Feature/GuiDefinition/DebugNuiGalleryDefinition.cs`), which renders every widget,
binding path, and known-good/known-bad layout shape.

## 1. End-to-end checklist

A window is three classes plus one enum entry. Registration is automatic
(reflection) — the enum entry is the only registry edit.

1. **Enum entry** — add to `Service/GuiService/GuiWindowType.cs`. Player-facing
   windows use the next sequential id; debug windows use the 900 range
   (`DebugEnmity = 900`, `DebugNuiGallery = 901`). Duplicate ids throw at boot.
2. **Definition** — `Feature/GuiDefinition/<Name>Definition.cs`, implements
   `IGuiWindowDefinition`; builds the layout with `GuiWindowBuilder<TViewModel>`.
3. **ViewModel** — `Feature/GuiDefinition/ViewModel/<Name>ViewModel.cs`, derives
   `GuiViewModelBase<TDerived, TPayload>` (use `GuiPayloadBase` directly when no
   open-time payload is needed).
4. **Open path** — `Gui.TogglePlayerWindow(player, GuiWindowType.X)`. For a debug
   window, add a chat command to
   `Feature/ChatCommandDefinition/DebuggingChatCommand.cs` following `NuiGallery()`:

```csharp
private void NuiGallery()
{
    _builder.Create("nuigallery")
        .Description("Opens the NUI control gallery debug window")
        .Permissions(AuthorizationLevel.Admin)
        .AvailableToAllOnTestEnvironment()
        .Action((user, target, location, args) =>
        {
            Gui.TogglePlayerWindow(user, GuiWindowType.DebugNuiGallery);
        });
}
```

## 2. ViewModel lifecycle (what actually happens, in order)

`GuiViewModelBase.Bind(...)` runs every time the window opens
(`Service/GuiService/GuiViewModelBase.cs`):

1. Geometry defaults to the definition's `SetInitialGeometry` if unset (per-player
   geometry is persisted across sessions in `Player.WindowGeometries`).
2. **Rebind loop**: every property value stored from a previous open is pushed to
   the client again (null-valued entries are skipped defensively).
3. `Geometry` is watched; `ModalInputText` is set then watched (the input modal only
   reports typed text while watched — rule R3's canonical example).
4. `%%WINDOW_MAIN%%` is swapped into the root.
5. The Geometry watch is re-issued one tick later (the client can drop it during the
   initial root layout).
6. **`Initialize(payload)` runs** — your override. Assign every bound property FIRST,
   then issue `WatchOnClient` calls, then select the default tab. `WatchOnClient`
   throws a descriptive exception if the property was never assigned (R3).

Client → server flow: a watched bind change invokes `UpdatePropertyFromClient`, which
sets the property **through its public setter via reflection**. `SkipNotify` only
suppresses the echo push back to the client — **your setter body still runs**. This is
why setters with side effects (logging, driving other properties) fire on every client
sync, and why tab-swap logic must not live in a widget-bound property setter (R4).

Properties use the `Get`/`Set` dictionary pattern:

```csharp
public string StatusText { get => Get<string>(); set => Set(value); }
```

Assigning a property pushes it to the client immediately. `GuiBindingList<T>` values
also push on in-place mutation (`Add`/`RemoveAt`/indexer) — see §5 for the exception.

## 3. Canonical skeleton (copy this shape)

A minimal two-tab window with a side rail, using the proven components:
`GuiStandardLayout` (rule R5), `GuiTabGroup`/`GuiToggleGroupSync` (rule R4), and
`OnModalClosedRestore` (rule R6). The skill assets
(`.codex/skills/swlor-gui-window-generation/assets/`) carry this same pair as
fill-in templates.

**Definition:**

```csharp
public class ExampleDefinition : IGuiWindowDefinition
{
    private readonly GuiWindowBuilder<ExampleViewModel> _builder = new();

    private const float TabRowHeight = 28f;      // tabbar rows: the ONE legal
    private const float TabPanelHeight = 48f;    //   fixed-height-row pairing (R2c)
    private const float ContentPanelWidth = 560f; // fixed-width tab panels (R5)
    private const float SideRailWidth = 240f;

    public GuiConstructedWindow BuildWindow()
    {
        var window = _builder.CreateWindow(GuiWindowType.Example)
            .SetInitialGeometry(0, 0, 900f, 560f)
            .SetTitle("Example")
            .SetIsResizable(true)
            .SetIsCollapsible(true)
            .BindOnClosed(model => model.OnWindowClosed())
            // One partial per tab; names live on the ViewModel as consts.
            .DefinePartialView(ExampleViewModel.FirstTabPartial, AddFirstTab)
            .DefinePartialView(ExampleViewModel.SecondTabPartial, AddSecondTab);

        // R5: never hand-roll the root. This emits the only shape proven to
        // track window geometry (content column + side rails in ONE root row).
        window.AddStandardLayout(layout =>
        {
            layout.SetTabPanelHeight(TabPanelHeight);
            layout.AddTabRow(row =>
            {
                row.SetHeight(TabRowHeight);
                row.AddToggles()                     // toggles are margin-free,
                    .AddOption("First")              //  so row+widget may share a
                    .AddOption("Second")             //  fixed height (R2c exception)
                    .BindSelectedValue(model => model.TabToggleValue)
                    .SetWidth(232f)
                    .SetHeight(TabRowHeight);
            });
            layout.SetContentPartialElement(ExampleViewModel.TabContentElement);
            layout.AddSideColumn(AddSideRail, SideRailWidth);
        });

        return _builder.Build();
    }

    // Each tab: a fixed-width borderless panel (R5). Scrolling comes from the
    // standard layout's content host group.
    private static void AddFirstTab(GuiGroup<ExampleViewModel> host)
    {
        host.AddColumn(col =>
        {
            col.AddRow(row =>
            {
                row.AddGroup(panel =>
                {
                    panel.SetShowBorder(false);
                    panel.SetScrollbars(NuiScrollbars.None);
                    panel.AddColumn(content =>
                    {
                        content.AddRow(r => r.AddLabel()
                            .BindText(model => model.StatusText)
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left));
                        content.AddRow(r =>
                        {
                            // No row height: rows with buttons derive their
                            // height from children + margins (R2c).
                            r.AddButton()
                                .SetText("Do Something")
                                .SetHeight(32f)
                                .SetWidth(160f)
                                .BindOnClicked(model => model.OnClickDoSomething());
                            r.AddSpacer();
                        });
                    });
                })
                    .SetWidth(ContentPanelWidth);
            });
        });
    }

    private static void AddSecondTab(GuiGroup<ExampleViewModel> host) { /* same shape */ }

    private static void AddSideRail(GuiColumn<ExampleViewModel> col) { /* labels, image, etc. */ }
}
```

**ViewModel:**

```csharp
public class ExampleViewModel : GuiViewModelBase<ExampleViewModel, GuiPayloadBase>
{
    private const int FirstTabId = 0;
    private const int SecondTabId = 1;
    public const string TabContentElement = "example_tab_content";
    public const string FirstTabPartial = "EXAMPLE_TAB_FIRST";
    public const string SecondTabPartial = "EXAMPLE_TAB_SECOND";

    // R4: tab registration + toggle sync are static, shared by all instances.
    private static readonly GuiTabGroup<ExampleViewModel, GuiPayloadBase> Tabs =
        new GuiTabGroup<ExampleViewModel, GuiPayloadBase>()
            .AddTab(FirstTabId, FirstTabPartial)
            .AddTab(SecondTabId, SecondTabPartial, m => m.RefreshSecondTab());

    private static readonly GuiToggleGroupSync TabToggles = new(FirstTabId, SecondTabId);

    public int SelectedTabId { get => Get<int>(); set => Set(value); }

    // R4: the widget-bound value property only forwards GENUINE user clicks.
    public int TabToggleValue
    {
        get => Get<int>();
        set
        {
            Set(value);
            TabToggles.HandleClientChange(value, SelectTab);
        }
    }

    public string StatusText { get => Get<string>(); set => Set(value); }

    protected override void Initialize(GuiPayloadBase initialPayload)
    {
        // R3: assign every bound property BEFORE any WatchOnClient call.
        StatusText = "Ready.";            // TODO: data plumbing
        TabToggleValue = 0;

        WatchOnClient(model => model.TabToggleValue);

        SelectTab(FirstTabId);
    }

    public override Action OnWindowClosed() => () => { };

    private void SelectTab(int tabId)
    {
        SelectedTabId = tabId;
        TabToggles.SyncTo(tabId, v => TabToggleValue = v);
        Tabs.Select(this, TabContentElement, tabId);
    }

    // R6: modal close wipes the nested tab partial; the base hook restores it.
    protected override void OnModalClosedRestore() => Tabs.Select(this, TabContentElement, SelectedTabId);

    private void RefreshSecondTab()
    {
        // TODO: data plumbing
    }

    public Action OnClickDoSomething() => () =>
    {
        // Click handlers RETURN an Action (invoked by the event pipeline).
        StatusText = "Clicked.";          // TODO: data plumbing
    };
}
```

## 4. Binding-type table

| C# property type | Binds to (builder call) |
|---|---|
| `string` | `BindText` (label/button/checkbox/text), `BindTooltip`, `BindValue` (textedit), `BindResref`/`BindImageResref` (image/image button), `BindPlaceholder`, `BindLegend` (chart) |
| `int` | `BindSelectedValue` (toggles/options), `BindSelectedIndex` (combo), `BindValue`/`BindMinimum`/`BindMaximum`/`BindStepSize` (slider int) |
| `float` | `BindValue` (progress 0–1, slider float), slider float min/max/step |
| `bool` | `BindIsChecked` (checkbox), `BindIsToggled` (toggle button), `BindIsEnabled`, `BindIsVisible`, `BindIsEncouraged` |
| `GuiColor` | `BindColor` (any widget foreground), `BindSelectedColor` (color picker), chart slot color, draw-list item color |
| `GuiRectangle` | `Geometry` (automatic), `BindRegion` (image), `BindBounds` (draw-list items) |
| `GuiBindingList<string/int/bool/float/GuiRectangle/GuiVector2/GuiColor>` | list template cells + `BindRowCount`, chart `BindData` (`float`) |
| `GuiBindingList<GuiComboEntry>` | `BindOptions` (combo) — **NOT change-hooked**: see §5 |

Methods bound to events (`BindOnClicked`, `BindOnMouseDown/Up`, `BindOnOpened/Closed`)
must be `public Action MethodName() => () => { ... };` — they return the action the
event pipeline invokes.

**Bind expressions must be bare property references** (`m => m.Prop`). Interpolation,
concatenation, or method calls inside the lambda (e.g. `m => $"{m.Count} / {m.Max}"`)
compile but throw `ArgumentException` in `GuiHelper.GetPropertyName` at boot, aborting
window template loading. Computed display text gets its own string property that the
ViewModel recomputes when its inputs change.

## 5. Lists and tables

**Row-templated lists** (`AddList`): each cell's widget binds a
`GuiBindingList<T>` property; the row index is implicit. Always call
`BindRowCount(model => model.SomeList)` — it wires the row count AND per-row
visibility workarounds. Keep every cell's list the same length (mismatches render
but show stale/blank cells — gallery P11).

```csharp
row.AddList(template =>
{
    template.AddCell(cell => cell.AddLabel().BindText(model => model.Names)
        .SetHorizontalAlign(NuiHorizontalAlign.Left));
    template.AddCell(cell =>
    {
        cell.SetIsVariable(false);            // element-sized fixed cell:
        cell.AddButton().SetText("Pick")      //  the button's width sizes it
            .SetWidth(44f)
            .BindOnClicked(model => model.OnClickRow());
    });
})
    .BindRowCount(model => model.Names)
    .SetRowHeight(28f);
```

Per-row clicks recover their row via `NuiGetEventArrayIndex()` inside the handler.

**Mutation:** in-place `Add`/`RemoveAt`/indexer on a `GuiBindingList<T>` pushes
automatically. Replacing the whole list object also works and re-hooks events.
**Exception:** `GuiBindingList<GuiComboEntry>` is not one of the hooked types — always
replace the whole object (see the gallery's `OnClickReplaceComboOptions`).

**Tables** (`AddTable` extension on `GuiColumn`, from
`Service/GuiService/Component/GuiTable.cs`): declarative header + list. Fixed columns
need `width > 0` (R1 — throws otherwise); only the last column defaults to variable.
`AddComponentColumn` hosts buttons/widgets; `SetShowHeader(false)` + explicit
`BindRowCount` for headerless lists. Pair with `GuiTableSource<TViewModel,TRow>` on
the VM side to refresh all column lists from one row-DTO list.

## 6. Partials, tabs, and modals

- `DefinePartialView(name, builder)` declares a swappable layout; it renders only
  when applied to an element via `ChangePartialView(elementId, partialName)` (direct)
  or `SwapNestedPartialView(...)` (root-redraw-safe path used by `GuiTabGroup`).
- Tabs: register in a static `GuiTabGroup`, sync toggle rows with
  `GuiToggleGroupSync`, drive swaps from `SelectTab` — exactly as in §3. Never bind
  the swap-driving property to the widget (R4).
- **Windows without tabs still use `AddStandardLayout`** (R5 applies regardless):
  zero `AddTabRow` calls, ONE partial holding the entire body (stacked sections as
  rows inside it), applied at the end of `Initialize` via
  `ChangePartialView(TabContentElement, MainContentPartial)` — and re-applied in
  `OnModalClosedRestore` if the window shows modals (R6).
- Element ids are NOT validated server-side; a typo produces a client-only error and
  the window must be reopened (R7). Keep ids as ViewModel consts.
- Maximum nesting: window root → partial → one nested slot. Three-deep content gets
  dropped by parent re-applies (R7).
- Modals: `ShowModal(prompt, onConfirm, onCancel)` / `ShowInputModal(...)` (read
  `ModalInputText` in the confirm action). Any tabbed window MUST override
  `OnModalClosedRestore` (R6) or its tab content vanishes when the modal closes.

## 7. Validation gate (all four, in order)

1. `dotnet build SWLOR.Game.Server.Tests/SWLOR.Game.Server.Tests.csproj -p:RunPostBuildEvent=Never`
   followed by `dotnet test SWLOR.Game.Server.Tests/SWLOR.Game.Server.Tests.csproj
   --no-build --filter "FullyQualifiedName~GuiLayoutValidationTests"` — the corpus test
   builds every GUI definition without the live engine and rejects unexpected findings.
2. Boot the dev server: **zero `[NUI layout warning]` Server-log entries mentioning your window.**
   The only expected warnings anywhere are DebugNuiGallery's six R2c canaries.
3. Optional: pull your window's `[NUI JSON]` lines from
   `debugserver/app_logs/Server/` and sanity-check the structure (root row → content
   column + rails). Available on non-production boots or with `SWLOR_NUI_DUMP_JSON=1`.
4. Open in-game via your chat command: walk every tab, resize the window (content
   must track), exercise every modal path (tab content must survive).
