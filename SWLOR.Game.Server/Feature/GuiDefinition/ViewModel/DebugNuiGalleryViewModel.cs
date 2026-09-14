using System;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class DebugNuiGalleryViewModel : GuiViewModelBase<DebugNuiGalleryViewModel, GuiPayloadBase>
    {
        public const int ButtonsTabId = 0;
        public const int TextTabId = 1;
        public const int SelectionTabId = 2;
        public const int SlidersTabId = 3;
        public const int ListsTabId = 4;
        public const int GroupsTabId = 5;
        public const int DrawingTabId = 6;
        public const int ChartsTabId = 7;
        public const int BindingsTabId = 8;
        public const int ModalsTabId = 9;
        public const int HazardsTabId = 10;

        public const string TabContentElement = "gallery_tab_content";
        public const string HazardSlotElement = "gallery_hazard_slot";

        public const string ButtonsTabPartial = "GALLERY_TAB_BUTTONS";
        public const string TextTabPartial = "GALLERY_TAB_TEXT";
        public const string SelectionTabPartial = "GALLERY_TAB_SELECTION";
        public const string SlidersTabPartial = "GALLERY_TAB_SLIDERS";
        public const string ListsTabPartial = "GALLERY_TAB_LISTS";
        public const string GroupsTabPartial = "GALLERY_TAB_GROUPS";
        public const string DrawingTabPartial = "GALLERY_TAB_DRAWING";
        public const string ChartsTabPartial = "GALLERY_TAB_CHARTS";
        public const string BindingsTabPartial = "GALLERY_TAB_BINDINGS";
        public const string ModalsTabPartial = "GALLERY_TAB_MODALS";
        public const string HazardsTabPartial = "GALLERY_TAB_HAZARDS";

        public const string HazardSafePartial = "GALLERY_HAZARD_SAFE";
        public const string HazardButtonRowPartial = "GALLERY_HAZARD_BUTTON_ROW";
        public const string HazardListNonTerminalPartial = "GALLERY_HAZARD_LIST_NONTERMINAL";
        public const string HazardTallStackPartial = "GALLERY_HAZARD_TALL_STACK";
        public const string HazardZeroCellPartial = "GALLERY_HAZARD_ZERO_CELL";
        public const string HazardWidthConflictPartial = "GALLERY_HAZARD_WIDTH_CONFLICT";

        public const string ProbeRowCheckboxPartial = "GALLERY_PROBE_ROW_CHECKBOX";
        public const string ProbeRowTextEditPartial = "GALLERY_PROBE_ROW_TEXTEDIT";
        public const string ProbeRowComboPartial = "GALLERY_PROBE_ROW_COMBO";
        public const string ProbeRowSliderPartial = "GALLERY_PROBE_ROW_SLIDER";
        public const string ProbeRowOptionsPartial = "GALLERY_PROBE_ROW_OPTIONS";
        public const string ProbeRowProgressPartial = "GALLERY_PROBE_ROW_PROGRESS";

        public const string ProbeColWidthPartial = "GALLERY_PROBE_COL_WIDTH";
        public const string ProbeAspectPartial = "GALLERY_PROBE_ASPECT";
        public const string ProbeZeroDimPartial = "GALLERY_PROBE_ZERO_DIM";
        public const string ProbeNegDimPartial = "GALLERY_PROBE_NEG_DIM";
        public const string ProbeNestedHostPartial = "GALLERY_PROBE_NESTED_HOST";
        public const string ProbeNestedSlotElement = "gallery_probe_nested_slot";

        private const int MaxLogEntries = 100;

        private static readonly GuiTabGroup<DebugNuiGalleryViewModel, GuiPayloadBase> Tabs =
            new GuiTabGroup<DebugNuiGalleryViewModel, GuiPayloadBase>()
                .AddTab(ButtonsTabId, ButtonsTabPartial)
                .AddTab(TextTabId, TextTabPartial)
                .AddTab(SelectionTabId, SelectionTabPartial)
                .AddTab(SlidersTabId, SlidersTabPartial)
                .AddTab(ListsTabId, ListsTabPartial)
                .AddTab(GroupsTabId, GroupsTabPartial)
                .AddTab(DrawingTabId, DrawingTabPartial)
                .AddTab(ChartsTabId, ChartsTabPartial)
                .AddTab(BindingsTabId, BindingsTabPartial)
                .AddTab(ModalsTabId, ModalsTabPartial)
                .AddTab(HazardsTabId, HazardsTabPartial);

        private static readonly GuiToggleGroupSync RowAToggles = new(ButtonsTabId, TextTabId, SelectionTabId, SlidersTabId);
        private static readonly GuiToggleGroupSync RowBToggles = new(ListsTabId, GroupsTabId, DrawingTabId, ChartsTabId);
        private static readonly GuiToggleGroupSync RowCToggles = new(BindingsTabId, ModalsTabId, HazardsTabId);

        private static readonly GuiColor[] ColorCycle =
        {
            GuiColor.White,
            GuiColor.Red,
            GuiColor.Green,
            GuiColor.Cyan,
            GuiColor.Grey
        };

        private static readonly string[] ImageCycle =
        {
            "arrow_up",
            "arrow_right",
            "arrow_down",
            "arrow_left"
        };

        private bool _isInitializing;
        private int _eventCount;
        private int _clickCount;
        private int _comboSetIndex;
        private int _colorIndex;
        private int _imageIndex;
        private int _drawStep;
        private int _chartRollIndex;
        private int _listRowIndex;

        public int SelectedTabId
        {
            get => Get<int>();
            set => Set(value);
        }

        public int RowATabValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                RowAToggles.HandleClientChange(value, SelectTab);
            }
        }

        public int RowBTabValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                RowBToggles.HandleClientChange(value, SelectTab);
            }
        }

        public int RowCTabValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                RowCToggles.HandleClientChange(value, SelectTab);
            }
        }

        public GuiBindingList<string> EventLog { get => Get<GuiBindingList<string>>(); set => Set(value); }

        // Buttons tab
        public string ClickCountText { get => Get<string>(); set => Set(value); }
        public string DynamicButtonText { get => Get<string>(); set => Set(value); }
        public string SampleToggleText { get => Get<string>(); set => Set(value); }

        public bool SampleToggle
        {
            get => Get<bool>();
            set
            {
                Set(value);
                SampleToggleText = value ? "Toggled ON" : "Toggled OFF";
            }
        }

        // Text & Input tab
        public string LongText { get => Get<string>(); set => Set(value); }

        public string TextEditValue
        {
            get => Get<string>();
            set
            {
                Set(value);
                MirrorLabelText = $"Mirror: {value}";
                LogEvent($"TextEditValue synced: '{value}'");
            }
        }

        public string MultilineValue
        {
            get => Get<string>();
            set
            {
                Set(value);
                LogEvent($"MultilineValue synced ({(value ?? string.Empty).Length} chars)");
            }
        }

        public string MirrorLabelText { get => Get<string>(); set => Set(value); }

        // Selection tab
        public bool IsChecked
        {
            get => Get<bool>();
            set
            {
                Set(value);
                CheckboxMirrorText = value ? "Checkbox is checked" : "Checkbox is unchecked";
                LogEvent($"Checkbox changed: {value}");
            }
        }

        public string CheckboxMirrorText { get => Get<string>(); set => Set(value); }

        public int StaticComboSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                LogEvent($"Static combo selection: {value}");
            }
        }

        public int DynamicComboSelection
        {
            get => Get<int>();
            set
            {
                Set(value);
                LogEvent($"Dynamic combo selection: {value}");
            }
        }

        public GuiBindingList<GuiComboEntry> DynamicComboOptions { get => Get<GuiBindingList<GuiComboEntry>>(); set => Set(value); }

        public int OptionsHorizontalValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                LogEvent($"Horizontal options selection: {value}");
            }
        }

        public int OptionsVerticalValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                LogEvent($"Vertical options selection: {value}");
            }
        }

        public GuiColor PickedColor
        {
            get => Get<GuiColor>();
            set
            {
                Set(value);
                LogEvent($"Color picked: R{value.R} G{value.G} B{value.B}");
            }
        }

        // Sliders tab
        public int SliderIntValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                ProgressValue = value / 10f;
                LogEvent($"SliderInt value: {value} (drives progress bar)");
            }
        }

        public float SliderFloatValue
        {
            get => Get<float>();
            set
            {
                Set(value);
                LogEvent($"SliderFloat value: {value:F2}");
            }
        }

        public float SliderFloatMax { get => Get<float>(); set => Set(value); }
        public float ProgressValue { get => Get<float>(); set => Set(value); }

        // Lists & Tables tab
        public GuiBindingList<string> ListNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> ListDescriptions { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<float> ListProgress { get => Get<GuiBindingList<float>>(); set => Set(value); }
        public GuiBindingList<string> ListResrefs { get => Get<GuiBindingList<string>>(); set => Set(value); }

        public GuiBindingList<string> TableColA { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> TableColB { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> TableColC { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> TableTooltips { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> Table2Names { get => Get<GuiBindingList<string>>(); set => Set(value); }

        // Images & Drawing tab
        public string CycledImageResref { get => Get<string>(); set => Set(value); }
        public GuiColor DrawColor { get => Get<GuiColor>(); set => Set(value); }
        public GuiRectangle DrawCircleBounds { get => Get<GuiRectangle>(); set => Set(value); }

        // Charts tab
        public GuiBindingList<float> ChartData { get => Get<GuiBindingList<float>>(); set => Set(value); }
        public string ChartLegend { get => Get<string>(); set => Set(value); }
        public GuiColor ChartColor { get => Get<GuiColor>(); set => Set(value); }

        // Bindings & Watches tab
        public bool IsSampleEnabled { get => Get<bool>(); set => Set(value); }
        public bool IsSampleVisible { get => Get<bool>(); set => Set(value); }
        public GuiColor SampleColor { get => Get<GuiColor>(); set => Set(value); }
        public string SampleTooltip { get => Get<string>(); set => Set(value); }
        public string FloodCounterText { get => Get<string>(); set => Set(value); }

        public string WatchDemoValue
        {
            get => Get<string>();
            set
            {
                Set(value);
                LogEvent($"WatchDemoValue synced: '{value}'");
            }
        }

        // Hazards tab. Deliberately never assigned anywhere - hazard H6 watches it
        // to demonstrate the R3 failure (WatchOnClient on a never-Set property).
        public string NeverSetProperty { get => Get<string>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _isInitializing = true;

            EventLog = new GuiBindingList<string>();

            // R3: every bound property is assigned here, before any WatchOnClient call
            // below. NeverSetProperty is the single deliberate exception (hazard H6).
            RowATabValue = 0;
            RowBTabValue = -1;
            RowCTabValue = -1;

            _clickCount = 0;
            ClickCountText = "Clicked 0 times";
            DynamicButtonText = "Bound text button (click to change)";
            SampleToggle = false;

            LongText = "This is a GuiText block with a border and auto scrollbars. " +
                       "It holds a long run of text so the scrollbar behavior can be verified. " +
                       "Line two of the text block.\nLine three of the text block.\n" +
                       "Line four.\nLine five.\nLine six.\nLine seven.\nLine eight.";
            TextEditValue = string.Empty;
            MultilineValue = "Multiline text edit.\nSecond line.";
            MirrorLabelText = "Mirror:";

            IsChecked = false;
            StaticComboSelection = 0;
            DynamicComboSelection = 0;
            DynamicComboOptions = BuildComboOptions();
            OptionsHorizontalValue = 0;
            OptionsVerticalValue = 0;
            PickedColor = GuiColor.White;

            SliderIntValue = 5;
            SliderFloatValue = 0.5f;
            SliderFloatMax = 1f;
            ProgressValue = 0.5f;

            ListNames = new GuiBindingList<string> { "Alpha", "Beta", "Gamma" };
            ListDescriptions = new GuiBindingList<string> { "First row", "Second row", "Third row" };
            ListProgress = new GuiBindingList<float> { 0.25f, 0.5f, 0.75f };
            ListResrefs = new GuiBindingList<string> { "arrow_up", "arrow_right", "arrow_down" };
            _listRowIndex = 3;

            TableColA = new GuiBindingList<string> { "A1", "A2", "A3", "A4" };
            TableColB = new GuiBindingList<string> { "B1", "B2", "B3", "B4" };
            TableColC = new GuiBindingList<string> { "Variable C1", "Variable C2", "Variable C3", "Variable C4" };
            TableTooltips = new GuiBindingList<string> { "Tooltip 1", "Tooltip 2", "Tooltip 3", "Tooltip 4" };
            Table2Names = new GuiBindingList<string> { "Component column row 1", "Component column row 2", "Component column row 3" };

            CycledImageResref = ImageCycle[0];
            _imageIndex = 0;
            DrawColor = GuiColor.Cyan;
            DrawCircleBounds = new GuiRectangle(10f, 10f, 40f, 40f);
            _drawStep = 0;

            ChartData = new GuiBindingList<float> { 1f, 3f, 2f, 5f, 4f };
            ChartLegend = "Bound bar data";
            ChartColor = GuiColor.FPColor;

            IsSampleEnabled = true;
            IsSampleVisible = true;
            SampleColor = GuiColor.White;
            SampleTooltip = "Initial tooltip";
            FloodCounterText = "No flood yet";
            WatchDemoValue = "watch me";

            WatchOnClient(model => model.RowATabValue);
            WatchOnClient(model => model.RowBTabValue);
            WatchOnClient(model => model.RowCTabValue);
            WatchOnClient(model => model.TextEditValue);
            WatchOnClient(model => model.MultilineValue);
            WatchOnClient(model => model.IsChecked);
            WatchOnClient(model => model.StaticComboSelection);
            WatchOnClient(model => model.DynamicComboSelection);
            WatchOnClient(model => model.OptionsHorizontalValue);
            WatchOnClient(model => model.OptionsVerticalValue);
            WatchOnClient(model => model.PickedColor);
            WatchOnClient(model => model.SliderIntValue);
            WatchOnClient(model => model.SliderFloatValue);
            WatchOnClient(model => model.WatchDemoValue);

            _isInitializing = false;

            SelectTab(ButtonsTabId);
            LogEvent("Window initialized");
        }

        private void SelectTab(int tabId)
        {
            SelectedTabId = tabId;
            RowAToggles.SyncTo(tabId, v => RowATabValue = v);
            RowBToggles.SyncTo(tabId, v => RowBTabValue = v);
            RowCToggles.SyncTo(tabId, v => RowCTabValue = v);
            Tabs.Select(this, TabContentElement, tabId);
            LogEvent($"Tab selected: {Tabs.GetPartialName(tabId)}");
        }

        private void LogEvent(string message)
        {
            if (_isInitializing)
                return;

            var log = EventLog;
            if (log == null)
                return;

            _eventCount++;
            log.Insert(0, $"[{_eventCount:D3}] {message}");

            while (log.Count > MaxLogEntries)
            {
                log.RemoveAt(log.Count - 1);
            }

            var separator = message.IndexOf(':');
            var eventName = separator >= 0 ? message[..separator] : message;
            Log.WriteStructured(
                LogGroup.Server,
                "[NUI gallery] Event {EventNumber}: {EventName}",
                _eventCount,
                eventName);
        }

        private static GuiBindingList<GuiComboEntry> BuildComboOptions(int setIndex = 0)
        {
            var options = new GuiBindingList<GuiComboEntry>();
            for (var index = 1; index <= 4; index++)
            {
                options.Add(new GuiComboEntry($"Set {setIndex} Option {index}", index - 1));
            }

            return options;
        }

        public Action OnWindowOpened() => () =>
        {
            LogEvent("Window opened event fired");
        };

        public override Action OnWindowClosed() => () =>
        {
            LogEvent("Window closed event fired");
        };

        public Action OnClickClearLog() => () =>
        {
            // Whole-object replacement on purpose: exercises the binding-list re-hook
            // path in GuiViewModelBase.Set in addition to the in-place mutations
            // LogEvent performs.
            EventLog = new GuiBindingList<string>();
            LogEvent("Event log cleared (list object replaced)");
        };

        // Buttons tab
        public Action OnClickSimpleButton() => () =>
        {
            _clickCount++;
            ClickCountText = $"Clicked {_clickCount} times";
            LogEvent($"Simple button clicked ({_clickCount})");
        };

        public Action OnClickImageButton() => () =>
        {
            LogEvent("Image button clicked");
        };

        public Action OnClickBoundTextButton() => () =>
        {
            DynamicButtonText = $"Text changed at click {++_clickCount}";
            LogEvent("Bound-text button clicked; its label was rewritten");
        };

        public Action OnClickSampleToggle() => () =>
        {
            SampleToggle = !SampleToggle;
            LogEvent($"Toggle button clicked; now {(SampleToggle ? "ON" : "OFF")}");
        };

        public Action OnMouseDownLabel() => () =>
        {
            LogEvent("Label mouse DOWN event fired");
        };

        public Action OnMouseUpLabel() => () =>
        {
            LogEvent("Label mouse UP event fired");
        };

        // Selection tab
        public Action OnClickReplaceComboOptions() => () =>
        {
            _comboSetIndex++;
            // GuiBindingList<GuiComboEntry> is not one of the list types hooked by
            // GuiViewModelBase.Set - the whole object must be replaced (the same
            // pattern the pagination combos ship with); in-place .Add would not push.
            DynamicComboOptions = BuildComboOptions(_comboSetIndex);
            DynamicComboSelection = 0;
            LogEvent($"Combo options replaced with set {_comboSetIndex}");
        };

        // Sliders tab
        public Action OnClickProgressAdd() => () =>
        {
            ProgressValue = Math.Min(1f, ProgressValue + 0.1f);
            LogEvent($"Progress bar increased to {ProgressValue:F1}");
        };

        public Action OnClickProgressReset() => () =>
        {
            ProgressValue = 0f;
            LogEvent("Progress bar reset to 0");
        };

        public Action OnClickRaiseSliderMax() => () =>
        {
            SliderFloatMax = SliderFloatMax >= 4f ? 1f : SliderFloatMax + 1f;
            LogEvent($"SliderFloat maximum changed to {SliderFloatMax:F0} while the window is open");
        };

        // Lists & Tables tab
        public Action OnClickListRowButton() => () =>
        {
            var index = NuiGetEventArrayIndex();
            LogEvent($"List row button clicked at index {index} ('{(index >= 0 && index < ListNames.Count ? ListNames[index] : "?")}')");
        };

        public Action OnClickTable2RowButton() => () =>
        {
            var index = NuiGetEventArrayIndex();
            LogEvent($"Table component-column button clicked at index {index}");
        };

        public Action OnClickListAddRow() => () =>
        {
            _listRowIndex++;
            ListNames.Add($"Row {_listRowIndex}");
            ListDescriptions.Add($"Added at #{_eventCount + 1}");
            ListProgress.Add((_listRowIndex % 10) / 10f);
            ListResrefs.Add(ImageCycle[_listRowIndex % ImageCycle.Length]);
            LogEvent($"List row added in place (now {ListNames.Count} rows)");
        };

        public Action OnClickListRemoveRow() => () =>
        {
            var rowCount = ListNames.Count;
            if (rowCount == 0)
            {
                LogEvent("List remove skipped; no rows left");
                return;
            }

            if (ListDescriptions.Count != rowCount ||
                ListProgress.Count != rowCount ||
                ListResrefs.Count != rowCount)
            {
                LogEvent("List remove skipped; bound column lengths differ");
                return;
            }

            var last = rowCount - 1;
            ListNames.RemoveAt(last);
            ListDescriptions.RemoveAt(last);
            ListProgress.RemoveAt(last);
            ListResrefs.RemoveAt(last);
            LogEvent($"List row removed in place (now {ListNames.Count} rows; exercises the row-shrink workaround)");
        };

        public Action OnClickListReplace() => () =>
        {
            ListNames = new GuiBindingList<string> { "Fresh A", "Fresh B" };
            ListDescriptions = new GuiBindingList<string> { "Replaced list", "Replaced list" };
            ListProgress = new GuiBindingList<float> { 0.9f, 0.1f };
            ListResrefs = new GuiBindingList<string> { "arrow_left", "arrow_right" };
            _listRowIndex = 2;
            LogEvent("All list objects replaced wholesale (re-hook path)");
        };

        // Images & Drawing tab
        public Action OnClickCycleImage() => () =>
        {
            _imageIndex = (_imageIndex + 1) % ImageCycle.Length;
            CycledImageResref = ImageCycle[_imageIndex];
            LogEvent($"Bound image resref cycled to '{CycledImageResref}'");
        };

        public Action OnClickAnimateDraw() => () =>
        {
            _drawStep = (_drawStep + 1) % 5;
            DrawCircleBounds = new GuiRectangle(10f + _drawStep * 15f, 10f, 30f + _drawStep * 5f, 30f + _drawStep * 5f);
            DrawColor = ColorCycle[_drawStep % ColorCycle.Length];
            LogEvent($"Draw list circle stepped (step {_drawStep}); bounds and color rebound");
        };

        // Charts tab
        public Action OnClickRandomizeChart() => () =>
        {
            _chartRollIndex++;
            var data = new GuiBindingList<float>();
            for (var point = 0; point < 5; point++)
            {
                data.Add(Service.Random.Next(1, 10));
            }

            ChartData = data;
            ChartLegend = $"Bound bar data (roll {_chartRollIndex})";
            ChartColor = ColorCycle[_chartRollIndex % ColorCycle.Length];
            LogEvent($"Chart data replaced (roll {_chartRollIndex})");
        };

        // Bindings & Watches tab
        public Action OnClickToggleVisible() => () =>
        {
            IsSampleVisible = !IsSampleVisible;
            LogEvent($"Specimen IsVisible set to {IsSampleVisible}");
        };

        public Action OnClickToggleEnabled() => () =>
        {
            IsSampleEnabled = !IsSampleEnabled;
            LogEvent($"Specimen IsEnabled set to {IsSampleEnabled}");
        };

        public Action OnClickCycleColor() => () =>
        {
            _colorIndex = (_colorIndex + 1) % ColorCycle.Length;
            SampleColor = ColorCycle[_colorIndex];
            LogEvent($"Specimen color cycled (index {_colorIndex})");
        };

        public Action OnClickCycleTooltip() => () =>
        {
            SampleTooltip = $"Tooltip updated at event {_eventCount + 1}";
            LogEvent("Specimen tooltip rebound; hover to verify");
        };

        public Action OnClickLogGeometry() => () =>
        {
            var geometry = Geometry;
            LogEvent($"Geometry: X{geometry.X:F0} Y{geometry.Y:F0} W{geometry.Width:F0} H{geometry.Height:F0}");
        };

        public Action OnClickRewatch() => () =>
        {
            // Re-issuing a watch on an already-watched, already-Set property must be
            // harmless (the framework does the same for Geometry after root layout).
            WatchOnClient(model => model.WatchDemoValue);
            LogEvent("Re-issued WatchOnClient(WatchDemoValue)");
        };

        public Action OnClickBindFlood() => () =>
        {
            for (var update = 1; update <= 20; update++)
            {
                FloodCounterText = $"Flood update {update} of 20";
            }

            LogEvent("Pushed 20 rapid bind updates to one property in a single event");
        };

        // Modals & Events tab
        // Closing a modal swaps %%WINDOW_MAIN%% back in, which resets the tab
        // content group to its empty template state - the current tab partial
        // must be re-applied afterwards (same as CharacterSheet's modal flow).
        protected override void OnModalClosedRestore() => Tabs.Select(this, TabContentElement, SelectedTabId);

        public Action OnClickShowModal() => () =>
        {
            LogEvent("Opening yes/no modal");
            ShowModal(
                "This is the stock confirmation modal. Choose either button.",
                () => LogEvent("Modal CONFIRMED"),
                () => LogEvent("Modal CANCELLED"));
        };

        public Action OnClickShowInputModal() => () =>
        {
            LogEvent("Opening input modal");
            ShowInputModal(
                "Type something and submit it.",
                "prefilled text",
                () => LogEvent($"Input modal submitted: '{ModalInputText}'"),
                () => LogEvent("Input modal cancelled"));
        };

        public Action OnClickReapplyTab() => () =>
        {
            LogEvent("Re-applying the current tab partial redundantly");
            Tabs.Select(this, TabContentElement, SelectedTabId);
        };

        // Hazards tab
        private bool HazardsAreAvailable
        {
            get
            {
                var environment = ApplicationSettings.Get().ServerEnvironment;
                return environment == ServerEnvironmentType.Development ||
                       environment == ServerEnvironmentType.Test;
            }
        }

        private void LoadHazard(string partialName, string expectedFailure)
        {
            if (!HazardsAreAvailable)
            {
                LogEvent("Hazards are disabled on this environment");
                return;
            }

            // Logged BEFORE the swap so the culprit is identifiable in the log
            // even if the window blanks out client-side.
            LogEvent($"LOADING HAZARD {partialName} (expect: {expectedFailure})");

            // The hazard slot is nested two partials deep (window root -> hazards tab
            // partial -> slot). SwapNestedPartialView's root-redraw pass would reset
            // the tab content and destroy the slot element, so apply directly and
            // re-apply once after the redraw nudge settles instead.
            ChangePartialView(HazardSlotElement, partialName);
            DelayCommand(0.0f, () =>
            {
                if (Gui.IsWindowOpen(Player, WindowType))
                    ChangePartialView(HazardSlotElement, partialName);
            });
        }

        public Action OnClickHazardButtonRow() => () =>
        {
            LoadHazard(HazardButtonRowPartial, "unsolvable layout - row height equals button height (R2c)");
        };

        public Action OnClickHazardListNonTerminal() => () =>
        {
            LoadHazard(HazardListNonTerminalPartial, "verified working - expect it to render");
        };

        public Action OnClickHazardTallStack() => () =>
        {
            LoadHazard(HazardTallStackPartial, "verified working - expect it to render");
        };

        public Action OnClickHazardZeroCell() => () =>
        {
            LoadHazard(HazardZeroCellPartial, "verified working - expect it to render");
        };

        public Action OnClickHazardWidthConflict() => () =>
        {
            LoadHazard(HazardWidthConflictPartial, "verified working - expect it to render");
        };

        public Action OnClickHazardWatchUnset() => () =>
        {
            LogEvent("LOADING HAZARD watch-unset-property (expect: descriptive InvalidOperationException; window unaffected)");

            // WatchOnClient fails fast on never-Set properties (rule R3). Before
            // that guard existed this silently created a null-valued property entry
            // that made every later reopen of the window throw, requiring a server
            // restart - the exact failure this exhibit originally uncovered.
            try
            {
                WatchOnClient(model => model.NeverSetProperty);
                LogEvent("UNEXPECTED: watching a never-Set property did not throw");
            }
            catch (InvalidOperationException ex)
            {
                LogEvent($"Confirmed R3 guard: {ex.Message}");
            }
        };

        public Action OnClickHazardReset() => () =>
        {
            if (!HazardsAreAvailable)
                return;

            LogEvent("Hazard slot reset to safe content");
            ChangePartialView(HazardSlotElement, HazardSafePartial);
        };

        public Action OnClickProbeRowCheckbox() => () =>
        {
            LoadHazard(ProbeRowCheckboxPartial, "confirmed - checkbox has default margins; blanks the window (R2c)");
        };

        public Action OnClickProbeRowTextEdit() => () =>
        {
            LoadHazard(ProbeRowTextEditPartial, "confirmed - textedit has default margins; blanks the window (R2c)");
        };

        public Action OnClickProbeRowCombo() => () =>
        {
            LoadHazard(ProbeRowComboPartial, "confirmed - combo has default margins; blanks the window (R2c)");
        };

        public Action OnClickProbeRowSlider() => () =>
        {
            LoadHazard(ProbeRowSliderPartial, "confirmed - slider has default margins; blanks the window (R2c)");
        };

        public Action OnClickProbeRowOptions() => () =>
        {
            LoadHazard(ProbeRowOptionsPartial, "verified working - options is margin-free like toggles");
        };

        public Action OnClickProbeRowProgress() => () =>
        {
            LoadHazard(ProbeRowProgressPartial, "confirmed - progress has default margins; blanks the window (R2c)");
        };

        public Action OnClickProbeColWidth() => () =>
        {
            LoadHazard(ProbeColWidthPartial, "verified working - expect it to render");
        };

        public Action OnClickProbeAspect() => () =>
        {
            LoadHazard(ProbeAspectPartial, "verified working - expect it to render");
        };

        public Action OnClickProbeZeroDim() => () =>
        {
            LoadHazard(ProbeZeroDimPartial, "verified working - expect it to render");
        };

        public Action OnClickProbeNegDim() => () =>
        {
            LoadHazard(ProbeNegDimPartial, "verified working - expect it to render");
        };

        public Action OnClickProbeListMismatch() => () =>
        {
            LogEvent("PROBE: setting ListNames to 5 rows but ListDescriptions to 2 - check the Lists & Tables tab; recover via Replace Lists");
            ListNames = new GuiBindingList<string> { "Mismatch 1", "Mismatch 2", "Mismatch 3", "Mismatch 4", "Mismatch 5" };
            ListDescriptions = new GuiBindingList<string> { "Only two", "descriptions" };
            ListProgress = new GuiBindingList<float> { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
            ListResrefs = new GuiBindingList<string> { "arrow_up", "arrow_up", "arrow_up", "arrow_up", "arrow_up" };
            _listRowIndex = 5;
        };

        public Action OnClickProbeEmptyData() => () =>
        {
            LogEvent("PROBE: emptying ChartData and DynamicComboOptions - check Charts and Selection tabs; recover via Randomize Data / Replace Options");
            ChartData = new GuiBindingList<float>();
            DynamicComboOptions = new GuiBindingList<GuiComboEntry>();
        };

        public Action OnClickProbeBadElementId() => () =>
        {
            LogEvent("PROBE: applying a partial to a nonexistent element id 'no_such_element_id'");
            try
            {
                ChangePartialView("no_such_element_id", HazardSafePartial);
                LogEvent("PROBE result: no server-side exception (check client for errors)");
            }
            catch (Exception ex)
            {
                LogEvent($"PROBE result: server-side {ex.GetType().Name}: {ex.Message}");
            }
        };

        public Action OnClickProbeNestedPartial() => () =>
        {
            LoadHazard(ProbeNestedHostPartial, "verified with caveat - renders, but the inner slot content is dropped by the parent re-apply");
            DelayCommand(0.1f, () =>
            {
                if (Gui.IsWindowOpen(Player, WindowType))
                {
                    LogEvent("PROBE: applying safe partial into the 3rd-level nested slot");
                    ChangePartialView(ProbeNestedSlotElement, HazardSafePartial);
                }
            });
        };
    }
}
