using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

using SWLOR.ContentBuilder.Models;
using SWLOR.ContentBuilder.Rendering;
using SWLOR.ContentBuilder.Services;
namespace SWLOR.ContentBuilder.Windows
{
    /// <summary>
    /// Content Builder main window: pick a content theme / tileset profile / layout profile, tune
    /// layout knobs, preview the generated schematic instantly, queue compositions into a batch,
    /// and build a toolset-reviewable module via SWLOR.ProcgenReview with one click.
    ///
    /// All controls are constructed in code (BuildLeftPanel) rather than XAML, since the left panel
    /// is mostly repetitive labeled Slider/ComboBox rows; MainWindow.xaml only holds the structural
    /// skeleton (left panel host, preview image, status bar).
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int MaxSeed = int.MaxValue - 1000;

        private const string SchematicModeKey = "schematic";
        private const string MapGraphicsModeKey = "mapgraphics";

        private readonly DefinitionCatalog _catalog = new();
        private readonly ObservableCollection<BatchItem> _batch = new();
        private readonly System.Collections.Generic.HashSet<string> _overriddenKnobs = new();

        private bool _suppressEvents;
        private GenerationResult _lastResult;
        private DungeonLayoutProfile _currentLayoutProfile;

        private ComboBox _themeCombo;
        private ComboBox _tilesetCombo;
        private ComboBox _layoutCombo;
        private ComboBox _styleCombo;
        private Button _resetDefaultsButton;

        private Slider _widthSlider;
        private Slider _heightSlider;
        private TextBox _widthValueBox;
        private TextBox _heightValueBox;

        private Slider _minRoomsSlider;
        private Slider _maxRoomsSlider;
        private Slider _minRoomSizeSlider;
        private Slider _maxRoomSizeSlider;
        private Slider _corridorWidthSlider;
        private Slider _loopFactorSlider;
        private Slider _organicFillSlider;
        private Slider _accentDensitySlider;
        private Slider _featureDensitySlider;
        private Slider _elevationRegionsSlider;
        private TextBox _minRoomsValueBox;
        private TextBox _maxRoomsValueBox;
        private TextBox _minRoomSizeValueBox;
        private TextBox _maxRoomSizeValueBox;
        private TextBox _corridorWidthValueBox;
        private TextBox _loopFactorValueBox;
        private TextBox _organicFillValueBox;
        private TextBox _accentDensityValueBox;
        private TextBox _featureDensityValueBox;
        private TextBox _elevationRegionsValueBox;
        private CheckBox _accentCheckBox;

        private Slider _entrancesSlider;
        private Slider _exitsSlider;
        private TextBox _entrancesValueBox;
        private TextBox _exitsValueBox;
        private CheckBox _doorTransitionsCheckBox;

        private TextBox _seedTextBox;
        private Button _randomSeedButton;

        private Button _generateButton;
        private Button _addToBatchButton;

        private DataGrid _batchGrid;
        private Button _removeSelectedButton;
        private Button _clearBatchButton;
        private Button _buildModuleButton;

        private TextBox _logTextBox;

        public MainWindow()
        {
            InitializeComponent();

            BuildLeftPanel();
            PopulateCombos();
            SetUpPreviewToolbar();
            // Size renders off PreviewHost, never PreviewImage: a WPF Image with a null Source
            // measures/arranges to 0x0 regardless of its layout slot, so the Image's own
            // ActualWidth/SizeChanged never deliver a usable size for the FIRST render — sizing
            // off the Image left the preview permanently blank at startup.
            PreviewHost.SizeChanged += (_, _) => RenderPreview();

            ApplyThemeDefaults(resetDimensionsAndSeed: true);
            RegeneratePreview();
        }

        /// <summary>
        /// Wires the Schematic / Map graphics mode picker and the Map graphics-only room overlay
        /// toggle. Switching mode or toggling the overlay only re-draws the last generation result
        /// (RenderPreview) — it never re-runs generation.
        /// </summary>
        private void SetUpPreviewToolbar()
        {
            PreviewModeCombo.DisplayMemberPath = nameof(KeyedItem.DisplayName);
            PreviewModeCombo.Items.Add(new KeyedItem(SchematicModeKey, "Schematic"));
            PreviewModeCombo.Items.Add(new KeyedItem(MapGraphicsModeKey, "Map graphics"));
            PreviewModeCombo.SelectedIndex = 1; // default to Map graphics

            PreviewModeCombo.SelectionChanged += (_, _) =>
            {
                RoomOverlayCheckBox.IsEnabled = IsMapGraphicsMode;
                RenderPreview();
            };
            RoomOverlayCheckBox.Checked += (_, _) => RenderPreview();
            RoomOverlayCheckBox.Unchecked += (_, _) => RenderPreview();
            RoomOverlayCheckBox.IsEnabled = IsMapGraphicsMode;
        }

        private bool IsMapGraphicsMode =>
            (PreviewModeCombo.SelectedItem as KeyedItem)?.Key == MapGraphicsModeKey;

        // ------------------------------------------------------------------
        // Left panel construction
        // ------------------------------------------------------------------

        private void BuildLeftPanel()
        {
            var (_, composition) = AddGroup(LeftStack, "Composition");
            _themeCombo = AddComboRow(composition, "Theme");
            _tilesetCombo = AddComboRow(composition, "Tileset Profile");
            _layoutCombo = AddComboRow(composition, "Layout Profile");
            _resetDefaultsButton = new Button { Content = "Reset to theme defaults", Margin = new Thickness(0, 6, 0, 0) };
            composition.Children.Add(_resetDefaultsButton);

            var (_, dimensions) = AddGroup(LeftStack, "Dimensions");
            (_widthSlider, _widthValueBox) = AddSliderRow(dimensions, "Width", 8, 32, 16);
            (_heightSlider, _heightValueBox) = AddSliderRow(dimensions, "Height", 8, 32, 16);

            // Advanced: everything below is fine-grained layout tuning most users won't touch, so it
            // lives collapsed behind an Expander rather than always taking up left-panel space. The
            // Expander's own Content panel (not LeftStack) hosts the "Layout overrides" group so a
            // collapsed Expander reserves only its header row.
            var advancedContent = new StackPanel();
            var advancedExpander = new Expander
            {
                Header = "Advanced",
                IsExpanded = false,
                Margin = new Thickness(0, 0, 0, 8),
                Content = advancedContent
            };
            LeftStack.Children.Add(advancedExpander);

            var (_, overrides) = AddGroup(advancedContent, "Layout overrides");
            _styleCombo = AddComboRow(overrides, "Style");
            foreach (DungeonLayoutStyle style in Enum.GetValues(typeof(DungeonLayoutStyle)))
                _styleCombo.Items.Add(new KeyedItem(style.ToString(), style.ToString()));

            (_minRoomsSlider, _minRoomsValueBox) = AddSliderRow(overrides, "Min Rooms", 2, 12, 4);
            (_maxRoomsSlider, _maxRoomsValueBox) = AddSliderRow(overrides, "Max Rooms", 2, 16, 8);
            (_minRoomSizeSlider, _minRoomSizeValueBox) = AddSliderRow(overrides, "Min Room Size", 2, 10, 3);
            (_maxRoomSizeSlider, _maxRoomSizeValueBox) = AddSliderRow(overrides, "Max Room Size", 3, 12, 7);
            (_corridorWidthSlider, _corridorWidthValueBox) = AddSliderRow(overrides, "Corridor Width", 1, 3, 1);
            (_loopFactorSlider, _loopFactorValueBox) = AddSliderRow(overrides, "Loop Factor", 0, 100, 25, suffix: "%");
            (_organicFillSlider, _organicFillValueBox) = AddSliderRow(overrides, "Organic Fill", 30, 60, 45, suffix: "%");

            _accentCheckBox = AddCheckBoxRow(overrides, "Accent terrain");
            (_accentDensitySlider, _accentDensityValueBox) = AddSliderRow(overrides, "Accent Density", 1, 20, 5, suffix: "%");

            // Feature tile SET (treasure mounds, pillars, hot springs, ...) always comes from the
            // tileset profile - only the density is user-tunable here.
            (_featureDensitySlider, _featureDensityValueBox) = AddSliderRow(overrides, "Feature Density", 0, 15, 5, suffix: "%");

            // Elevation Regions: how many raised floor/wall patches LayoutElevationPainter attempts.
            // Best-effort and shape-gated against the real tileset (see DungeonTilesetProfile.
            // MaxElevationRegions/LayoutElevationPainter) -- a no-op on any tileset without verified
            // rim vocabulary, so this stays enabled/at its composed default for every profile rather
            // than being hidden; UpdateKnobConstraints disables it when the current tileset has none.
            (_elevationRegionsSlider, _elevationRegionsValueBox) = AddSliderRow(overrides, "Elevation Regions", 0, 3, 0);

            var (_, transitions) = AddGroup(LeftStack, "Transitions");
            (_entrancesSlider, _entrancesValueBox) = AddSliderRow(transitions, "Entrances", 1, 3, 1);
            (_exitsSlider, _exitsValueBox) = AddSliderRow(transitions, "Exits", 1, 3, 1);
            _doorTransitionsCheckBox = AddCheckBoxRow(transitions, "Door transitions (fallback: placeable)");
            _doorTransitionsCheckBox.IsChecked = true;

            var (_, seedGroup) = AddGroup(LeftStack, "Seed");
            var seedRow = CreateRow();
            var seedLabel = new TextBlock { Text = "Seed", VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(seedLabel, 0);
            seedRow.Children.Add(seedLabel);
            _seedTextBox = new TextBox { Text = NewRandomSeedText(), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 4, 0) };
            Grid.SetColumn(_seedTextBox, 1);
            seedRow.Children.Add(_seedTextBox);
            _randomSeedButton = new Button { Content = "Random", Padding = new Thickness(6, 0, 6, 0) };
            Grid.SetColumn(_randomSeedButton, 2);
            Grid.SetColumnSpan(_randomSeedButton, 1);
            seedRow.Children.Add(_randomSeedButton);
            seedGroup.Children.Add(seedRow);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 8) };
            _generateButton = new Button { Content = "Generate Preview", Margin = new Thickness(0, 0, 6, 0), Padding = new Thickness(8, 4, 8, 4) };
            _addToBatchButton = new Button { Content = "Add to Batch", Padding = new Thickness(8, 4, 8, 4) };
            buttonPanel.Children.Add(_generateButton);
            buttonPanel.Children.Add(_addToBatchButton);
            LeftStack.Children.Add(buttonPanel);

            var (_, batchGroup) = AddGroup(LeftStack, "Batch");
            _batchGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                CanUserReorderColumns = false,
                Height = 150,
                ItemsSource = _batch,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                SelectionMode = DataGridSelectionMode.Extended
            };
            _batchGrid.Columns.Add(new DataGridTextColumn { Header = "Theme", Binding = new Binding(nameof(BatchItem.ThemeDisplayName)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            _batchGrid.Columns.Add(new DataGridTextColumn { Header = "Tileset", Binding = new Binding(nameof(BatchItem.TilesetDisplayName)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            _batchGrid.Columns.Add(new DataGridTextColumn { Header = "Layout", Binding = new Binding(nameof(BatchItem.LayoutDisplayName)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            _batchGrid.Columns.Add(new DataGridTextColumn { Header = "Seed", Binding = new Binding(nameof(BatchItem.Seed)), Width = new DataGridLength(50) });
            _batchGrid.Columns.Add(new DataGridTextColumn { Header = "Size", Binding = new Binding(nameof(BatchItem.Size)), Width = new DataGridLength(50) });
            _batchGrid.Columns.Add(new DataGridTextColumn { Header = "Ent", Binding = new Binding(nameof(BatchItem.Entrances)), Width = new DataGridLength(36) });
            _batchGrid.Columns.Add(new DataGridTextColumn { Header = "Exit", Binding = new Binding(nameof(BatchItem.Exits)), Width = new DataGridLength(36) });
            _batchGrid.Columns.Add(new DataGridTextColumn { Header = "Doors", Binding = new Binding(nameof(BatchItem.DoorTransitions)), Width = new DataGridLength(48) });
            batchGroup.Children.Add(_batchGrid);

            var batchButtons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
            _removeSelectedButton = new Button { Content = "Remove Selected", Margin = new Thickness(0, 0, 6, 0), Padding = new Thickness(6, 2, 6, 2) };
            _clearBatchButton = new Button { Content = "Clear", Margin = new Thickness(0, 0, 6, 0), Padding = new Thickness(6, 2, 6, 2) };
            _buildModuleButton = new Button { Content = "Build Review Module", Padding = new Thickness(6, 2, 6, 2) };
            batchButtons.Children.Add(_removeSelectedButton);
            batchButtons.Children.Add(_clearBatchButton);
            batchButtons.Children.Add(_buildModuleButton);
            batchGroup.Children.Add(batchButtons);

            var (_, logGroup) = AddGroup(LeftStack, "Log");
            _logTextBox = new TextBox
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                AcceptsReturn = true,
                Height = 90,
                FontFamily = new FontFamily("Consolas")
            };
            logGroup.Children.Add(_logTextBox);

            WireEvents();
        }

        private static (GroupBox Box, StackPanel Content) AddGroup(Panel parent, string header)
        {
            var content = new StackPanel { Margin = new Thickness(6) };
            var box = new GroupBox { Header = header, Margin = new Thickness(0, 0, 0, 8), Content = content };
            parent.Children.Add(box);
            return (box, content);
        }

        private static Grid CreateRow()
        {
            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(118) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            return grid;
        }

        private static ComboBox AddComboRow(Panel parent, string label)
        {
            var row = CreateRow();

            var text = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(text, 0);
            row.Children.Add(text);

            var combo = new ComboBox
            {
                DisplayMemberPath = nameof(KeyedItem.DisplayName),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 2, 0, 2)
            };
            Grid.SetColumn(combo, 1);
            Grid.SetColumnSpan(combo, 2);
            row.Children.Add(combo);

            parent.Children.Add(row);
            return combo;
        }

        /// <summary>
        /// Builds a labeled Slider row whose value readout is a small editable TextBox instead of a
        /// plain TextBlock: SliderTextBoxSync.Attach wires the two together (slider drag/programmatic
        /// changes update the box; typing a number into the box and pressing Enter or tabbing away
        /// commits it back to the slider, clamped to Minimum/Maximum). <paramref name="suffix"/> (e.g.
        /// "%") is rendered as a small adjacent label, not inside the editable text, since the box only
        /// ever accepts/shows a plain integer.
        /// </summary>
        private static (Slider Slider, TextBox Box) AddSliderRow(Panel parent, string label, double min, double max, double value, string suffix = "")
        {
            var row = CreateRow();

            var text = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(text, 0);
            row.Children.Add(text);

            var slider = new Slider
            {
                Minimum = min,
                Maximum = max,
                Value = value,
                IsSnapToTickEnabled = true,
                TickFrequency = 1,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 4, 0)
            };
            Grid.SetColumn(slider, 1);
            row.Children.Add(slider);

            var valuePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            var valueBox = new TextBox
            {
                Width = 34,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            valuePanel.Children.Add(valueBox);
            if (!string.IsNullOrEmpty(suffix))
            {
                valuePanel.Children.Add(new TextBlock
                {
                    Text = suffix,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2, 0, 0, 0)
                });
            }
            Grid.SetColumn(valuePanel, 2);
            row.Children.Add(valuePanel);

            parent.Children.Add(row);

            SliderTextBoxSync.Attach(slider, valueBox);

            return (slider, valueBox);
        }

        private static CheckBox AddCheckBoxRow(Panel parent, string label)
        {
            var row = CreateRow();
            var checkBox = new CheckBox { Content = label, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(checkBox, 0);
            Grid.SetColumnSpan(checkBox, 3);
            row.Children.Add(checkBox);
            parent.Children.Add(row);
            return checkBox;
        }

        // ------------------------------------------------------------------
        // Event wiring
        // ------------------------------------------------------------------

        private void WireEvents()
        {
            _themeCombo.SelectionChanged += (_, _) => { if (_suppressEvents) return; OnThemeChanged(); };
            _tilesetCombo.SelectionChanged += (_, _) => { if (_suppressEvents) return; OnTilesetChanged(); };
            _layoutCombo.SelectionChanged += (_, _) => { if (_suppressEvents) return; OnLayoutProfileChanged(); };
            _resetDefaultsButton.Click += (_, _) => { ApplyThemeDefaults(resetDimensionsAndSeed: true); RegeneratePreview(); };

            _generateButton.Click += (_, _) => GeneratePreview();
            _addToBatchButton.Click += (_, _) => AddToBatch();
            _randomSeedButton.Click += (_, _) =>
            {
                _seedTextBox.Text = NewRandomSeedText();
            };

            _widthSlider.ValueChanged += (_, _) => { if (_suppressEvents) return; UpdateKnobConstraints(); RegeneratePreview(); };
            _heightSlider.ValueChanged += (_, _) => { if (_suppressEvents) return; UpdateKnobConstraints(); RegeneratePreview(); };

            _styleCombo.SelectionChanged += (_, _) =>
            {
                if (_suppressEvents) return;
                MarkOverride(nameof(_styleCombo));
                UpdateOrganicFillEnabled();
                UpdateKnobConstraints();
                RegeneratePreview();
            };

            // Min/Max Rooms and Min/Max Room Size are coupled pairs: each slider's own ValueChanged
            // immediately tightens the OTHER slider's Minimum/Maximum so Min>Max is unreachable by
            // dragging at any time, not just right after a profile/style/size change (see
            // WireCoupledSlider). UpdateKnobConstraints (profile load / style change / Width-Height
            // change) separately applies the style+size-derived room-size ceiling on top of this.
            WireCoupledSlider(_minRoomsSlider, _maxRoomsSlider, isMinSide: true);
            WireCoupledSlider(_maxRoomsSlider, _minRoomsSlider, isMinSide: false);
            WireCoupledSlider(_minRoomSizeSlider, _maxRoomSizeSlider, isMinSide: true);
            WireCoupledSlider(_maxRoomSizeSlider, _minRoomSizeSlider, isMinSide: false);

            WireKnobSlider(_corridorWidthSlider, nameof(_corridorWidthSlider));
            WireKnobSlider(_loopFactorSlider, nameof(_loopFactorSlider));
            WireKnobSlider(_organicFillSlider, nameof(_organicFillSlider));
            WireKnobSlider(_accentDensitySlider, nameof(_accentDensitySlider));
            WireKnobSlider(_featureDensitySlider, nameof(_featureDensitySlider));
            WireKnobSlider(_elevationRegionsSlider, nameof(_elevationRegionsSlider));
            WireKnobSlider(_entrancesSlider, nameof(_entrancesSlider));
            WireKnobSlider(_exitsSlider, nameof(_exitsSlider));

            _accentCheckBox.Checked += (_, _) => OnAccentCheckChanged();
            _accentCheckBox.Unchecked += (_, _) => OnAccentCheckChanged();

            _doorTransitionsCheckBox.Checked += (_, _) => { if (_suppressEvents) return; MarkOverride(nameof(_doorTransitionsCheckBox)); RegeneratePreview(); };
            _doorTransitionsCheckBox.Unchecked += (_, _) => { if (_suppressEvents) return; MarkOverride(nameof(_doorTransitionsCheckBox)); RegeneratePreview(); };

            _seedTextBox.PreviewTextInput += (_, e) => e.Handled = !e.Text.All(char.IsDigit);
            _seedTextBox.TextChanged += (_, _) => { if (_suppressEvents) return; RegeneratePreview(); };

            _removeSelectedButton.Click += (_, _) => RemoveSelectedBatchItems();
            _clearBatchButton.Click += (_, _) => _batch.Clear();
            _buildModuleButton.Click += async (_, _) => await BuildReviewModuleAsync();
        }

        /// <summary>
        /// Domain glue for an "override" slider whose box/slider sync is already wired by
        /// SliderTextBoxSync.Attach (see AddSliderRow): marks the knob as user-overridden (so a
        /// theme/profile reload won't clobber it) and regenerates the preview.
        /// </summary>
        private void WireKnobSlider(Slider slider, string key)
        {
            slider.ValueChanged += (_, _) =>
            {
                if (_suppressEvents) return;
                MarkOverride(key);
                RegeneratePreview();
            };
        }

        private void MarkOverride(string key) => _overriddenKnobs.Add(key);

        /// <summary>
        /// Wires one side of a Min/Max slider pair (Min Rooms/Max Rooms, Min Room Size/Max Room Size):
        /// dragging this slider immediately tightens the PARTNER's Minimum (if this is the min side) or
        /// Maximum (if this is the max side) to this slider's new value, so Min>Max can never be
        /// reached by dragging either slider at any time -- not only right after a profile/style/size
        /// change (UpdateKnobConstraints separately layers the style+size-derived room-size ceiling on
        /// top of this same coupling). The partner update runs under _suppressEvents so a coercion it
        /// triggers on the partner's Value never fires a second MarkOverride/RegeneratePreview -- the
        /// single call at the end of this handler is the only one for the user's actual drag.
        /// </summary>
        private void WireCoupledSlider(Slider slider, Slider partner, bool isMinSide)
        {
            slider.ValueChanged += (_, _) =>
            {
                if (_suppressEvents) return;
                MarkOverride(slider == _minRoomsSlider ? nameof(_minRoomsSlider)
                    : slider == _maxRoomsSlider ? nameof(_maxRoomsSlider)
                    : slider == _minRoomSizeSlider ? nameof(_minRoomSizeSlider)
                    : nameof(_maxRoomSizeSlider));

                var wasSuppressed = _suppressEvents;
                _suppressEvents = true;
                try
                {
                    if (isMinSide)
                        partner.Minimum = slider.Value;
                    else
                        partner.Maximum = slider.Value;
                }
                finally
                {
                    _suppressEvents = wasSuppressed;
                }

                RegeneratePreview();
            };
        }

        private void OnAccentCheckChanged()
        {
            _accentDensitySlider.IsEnabled = _accentCheckBox.IsChecked == true;
            if (_suppressEvents) return;
            MarkOverride(nameof(_accentCheckBox));
            RegeneratePreview();
        }

        // ------------------------------------------------------------------
        // Combo population / selection helpers
        // ------------------------------------------------------------------

        private void PopulateCombos()
        {
            foreach (var theme in _catalog.Themes)
                _themeCombo.Items.Add(new KeyedItem(theme.ThemeKey, theme.DisplayName));

            foreach (var profile in _catalog.TilesetProfiles.Values.OrderBy(p => p.DisplayName))
                _tilesetCombo.Items.Add(new KeyedItem(profile.Key, profile.DisplayName));

            RepopulateLayoutCombo();

            _suppressEvents = true;
            try
            {
                if (_themeCombo.Items.Count > 0) _themeCombo.SelectedIndex = 0;
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        /// <summary>
        /// Rebuilds the Layout Profile combo to only the profiles the currently selected tileset can
        /// actually realize. An Alley-corridor profile (City Streets) on a tileset without the Alley
        /// crosser vocabulary silently downgrades to Corridor tunnels — an identical result to the
        /// Corridor Complex profile — so offering it as a separate choice is misleading. A Tunnel-mode
        /// profile (Complex or Streets after its own Alley downgrade) on a tileset missing the Doorway
        /// or Corridor crosser it needs (e.g. Barrows/tbw01) can never resolve at all, so it is hidden
        /// outright rather than offered and left to fail generation.
        /// </summary>
        private void RepopulateLayoutCombo()
        {
            var previousKey = (_layoutCombo.SelectedItem as KeyedItem)?.Key;
            var tilesetProfile = SelectedTilesetProfile();

            _suppressEvents = true;
            try
            {
                _layoutCombo.Items.Clear();
                foreach (var profile in _catalog.LayoutProfiles.Values.OrderBy(p => p.DisplayName))
                {
                    if (TilesetSupportsLayoutProfile(tilesetProfile, profile))
                        _layoutCombo.Items.Add(new KeyedItem(profile.Key, profile.DisplayName));
                }

                if (previousKey != null)
                    SelectComboByKey(_layoutCombo, previousKey);
                else if (_layoutCombo.Items.Count > 0)
                    _layoutCombo.SelectedIndex = 0;
            }
            finally
            {
                _suppressEvents = false;
            }

            // If the filter dropped the previous selection, the combo fell back to another profile —
            // its knobs must load like a manual selection would have loaded them.
            var newKey = (_layoutCombo.SelectedItem as KeyedItem)?.Key;
            if (previousKey != null && newKey != previousKey)
            {
                _overriddenKnobs.Clear();
                LoadLayoutProfileKnobs(SelectedLayoutProfile());
            }
        }

        private bool TilesetSupportsLayoutProfile(DungeonTilesetProfile tilesetProfile, DungeonLayoutProfile layoutProfile)
        {
            if (tilesetProfile == null) return true;
            if (layoutProfile.Template.CorridorMode != CorridorMode.Tunnel) return true;

            try
            {
                var model = TilesetModelCache.Get(tilesetProfile.TilesetResref);

                // Shape-aware, not just crosser-name presence: mirrors MacroLayoutGenerator's own
                // downgrade check (TunnelVocabularyCheck.SupportsTunnels) so the dropdown never offers a
                // pairing the engine would itself downgrade away from Tunnel mode -- e.g. Illithid
                // Interior (tii01) declares both "Doorway" and "Corridor" but is missing a junction
                // shape (Corridor+Corridor+Doorway) Tunnel mode needs for reliable generation, and Ruins
                // (tdr01) declares "Alley" but has no side-open boundary tile carrying a lone Alley edge
                // at all.
                var openTerrain = string.IsNullOrEmpty(tilesetProfile.PrimaryOpenTerrain)
                    ? model.FloorTerrain
                    : tilesetProfile.PrimaryOpenTerrain;

                return TunnelVocabularyCheck.SupportsTunnels(
                    model, openTerrain, tilesetProfile.SecondaryOpenTerrain, model.DefaultTerrain,
                    layoutProfile.Template.CorridorCrosserType);
            }
            catch
            {
                // If the tileset model can't load here, don't hide options — generation itself
                // reports the real failure and the engine downgrade keeps it safe.
                return true;
            }
        }

        private static void SelectComboByKey(ComboBox combo, string key)
        {
            foreach (var obj in combo.Items)
            {
                if (obj is KeyedItem item && item.Key == key)
                {
                    combo.SelectedItem = obj;
                    return;
                }
            }

            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private DungeonDetail SelectedTheme()
        {
            var key = (_themeCombo.SelectedItem as KeyedItem)?.Key;
            return key == null ? null : _catalog.Themes.FirstOrDefault(t => t.ThemeKey == key);
        }

        private DungeonTilesetProfile SelectedTilesetProfile()
        {
            var key = (_tilesetCombo.SelectedItem as KeyedItem)?.Key;
            return key != null && _catalog.TilesetProfiles.TryGetValue(key, out var profile) ? profile : null;
        }

        private DungeonLayoutProfile SelectedLayoutProfile()
        {
            var key = (_layoutCombo.SelectedItem as KeyedItem)?.Key;
            return key != null && _catalog.LayoutProfiles.TryGetValue(key, out var profile) ? profile : null;
        }

        private DungeonLayoutStyle SelectedStyle()
        {
            var key = (_styleCombo.SelectedItem as KeyedItem)?.Key;
            return key != null && Enum.TryParse<DungeonLayoutStyle>(key, out var style) ? style : DungeonLayoutStyle.RoomsAndCorridors;
        }

        // ------------------------------------------------------------------
        // Theme / tileset / layout profile change handling
        // ------------------------------------------------------------------

        private void OnThemeChanged()
        {
            ApplyThemeDefaults(resetDimensionsAndSeed: false);
            RegeneratePreview();
        }

        private void OnTilesetChanged()
        {
            RepopulateLayoutCombo();
            UpdateAccentAvailability();
            UpdateFeatureAvailability();
            UpdateElevationAvailability();
            // Composed knob values (e.g. CorridorWidth's tileset-driven floor) depend on the selected
            // tileset, not just the layout profile -- reload so untouched sliders reflect the new
            // tileset's composition even when RepopulateLayoutCombo kept the same layout profile
            // selected (LoadLayoutProfileKnobs itself skips any knob the user has already overridden).
            LoadLayoutProfileKnobs(SelectedLayoutProfile());
            RegeneratePreview();
        }

        private void OnLayoutProfileChanged()
        {
            _overriddenKnobs.Clear();
            LoadLayoutProfileKnobs(SelectedLayoutProfile());
            RegeneratePreview();
        }

        /// <summary>
        /// Selects the theme's default tileset/layout profiles (Composition group) and, when
        /// requested, resets Width/Height/Seed too — used both by the explicit "Reset to theme
        /// defaults" button and by picking a new Theme.
        /// </summary>
        private void ApplyThemeDefaults(bool resetDimensionsAndSeed)
        {
            var theme = SelectedTheme();
            if (theme == null) return;

            _suppressEvents = true;
            try
            {
                SelectComboByKey(_tilesetCombo, theme.TilesetProfileKey);
                RepopulateLayoutCombo();
                SelectComboByKey(_layoutCombo, theme.LayoutProfileKey);

                if (resetDimensionsAndSeed)
                {
                    _widthSlider.Value = 16;
                    _heightSlider.Value = 16;
                    _seedTextBox.Text = NewRandomSeedText();
                }
            }
            finally
            {
                _suppressEvents = false;
            }

            _overriddenKnobs.Clear();
            UpdateAccentAvailability();
            UpdateFeatureAvailability();
            UpdateElevationAvailability();
            LoadLayoutProfileKnobs(SelectedLayoutProfile());
        }

        private void UpdateAccentAvailability()
        {
            var tileset = SelectedTilesetProfile();
            var supportsAccent = tileset != null && !string.IsNullOrEmpty(tileset.AccentTerrain);

            _accentCheckBox.IsEnabled = supportsAccent;
            if (!supportsAccent && _accentCheckBox.IsChecked == true)
            {
                _suppressEvents = true;
                try { _accentCheckBox.IsChecked = false; }
                finally { _suppressEvents = false; }
            }

            _accentDensitySlider.IsEnabled = supportsAccent && _accentCheckBox.IsChecked == true;
        }

        private void UpdateFeatureAvailability()
        {
            var tileset = SelectedTilesetProfile();
            _featureDensitySlider.IsEnabled = tileset != null && tileset.FeatureTiles.Count > 0;
        }

        private void UpdateElevationAvailability()
        {
            var tileset = SelectedTilesetProfile();
            _elevationRegionsSlider.IsEnabled = tileset != null && tileset.MaxElevationRegions > 0;
        }

        private void UpdateOrganicFillEnabled()
        {
            _organicFillSlider.IsEnabled = SelectedStyle() == DungeonLayoutStyle.OrganicCave;
        }

        /// <summary>
        /// Pre-fills the "Layout overrides" knobs from the profile's template, skipping any field
        /// the user has directly edited since the last profile load / theme-defaults reset.
        /// </summary>
        /// <summary>
        /// Raises the Width/Height slider minimums to the effective layout style's empirically
        /// measured size floor (see LayoutStyleSizeFloor) so sizes that cannot generate are simply
        /// not selectable. WPF sliders coerce Value up automatically when Minimum rises, and the
        /// numeric boxes clamp typed input to the slider range.
        /// </summary>
        private void UpdateSizeFloor()
        {
            var floor = LayoutStyleSizeFloor.For(SelectedStyle());
            _widthSlider.Minimum = floor;
            _heightSlider.Minimum = floor;
        }

        /// <summary>
        /// Room Size slider ceiling never exceeds this, even for styles whose room-size knobs are
        /// structurally unused (OrganicCave, where LayoutParameterConstraints.RoomSizeBounds returns
        /// an unbounded Max) -- keeps the slider's own track usable instead of stretching it out to
        /// int.MaxValue for a style where the value doesn't matter anyway.
        /// </summary>
        private const int RoomSizeSliderAbsoluteMax = 12;

        /// <summary>
        /// Recomputes every Advanced Settings slider bound so no combination the user can reach by
        /// dragging can fail generation, mirroring LayoutParameterConstraints -- the same engine-side
        /// authority MacroLayoutGenerator.Generate clamps through as a final safety net. Run whenever
        /// the layout profile loads, the Style combo changes, or Width/Height change (the coupled
        /// Min/Max pairs for Rooms and Room Size are additionally kept live on every drag by
        /// WireCoupledSlider, since a style/size change alone wouldn't re-tighten them against a
        /// mid-drag Min>Max attempt). Idempotent and safe to call repeatedly: wraps every bound change
        /// in _suppressEvents (preserving, not clobbering, whatever suppression state the caller was
        /// already in) so this never triggers its own regeneration -- the caller performs the single
        /// RegeneratePreview for the change that triggered it.
        /// </summary>
        private void UpdateKnobConstraints()
        {
            UpdateSizeFloor();

            var style = SelectedStyle();
            var width = (int)_widthSlider.Value;
            var height = (int)_heightSlider.Value;

            var (_, maxRoomSize) = LayoutParameterConstraints.RoomSizeBounds(style, width, height);
            var effectiveMaxRoomSize = Math.Min(maxRoomSize, RoomSizeSliderAbsoluteMax);
            var minFillPercent = Math.Round(LayoutParameterConstraints.MinSafeOpenFillTarget(width, height) * 100);

            var wasSuppressed = _suppressEvents;
            _suppressEvents = true;
            try
            {
                // Room size: cap Max first (coerces its Value down if it exceeded the new style+size
                // ceiling), then re-couple Min<=Max off the post-coercion Max value, then Max>=Min off
                // the post-coercion Min value -- this order handles cascading coercion correctly
                // regardless of what the sliders held before (e.g. a style switch that shrinks the
                // ceiling out from under a previously-valid Min/Max pair).
                _maxRoomSizeSlider.Maximum = effectiveMaxRoomSize;
                _minRoomSizeSlider.Maximum = _maxRoomSizeSlider.Value;
                _maxRoomSizeSlider.Minimum = _minRoomSizeSlider.Value;

                // Rooms: no style/size-derived ceiling, just the Min<=Max coupling.
                _minRoomsSlider.Maximum = _maxRoomsSlider.Value;
                _maxRoomsSlider.Minimum = _minRoomsSlider.Value;

                // OrganicCave's Organic Fill has a hard safe floor that rises steeply as the area
                // shrinks toward its size floor (see LayoutParameterConstraints.MinSafeOpenFillTarget).
                // Applied regardless of the current style: harmless when the slider is disabled
                // (UpdateOrganicFillEnabled), and keeps the bound already correct if the user switches
                // to OrganicCave afterward without touching Width/Height again.
                _organicFillSlider.Minimum = minFillPercent;
            }
            finally
            {
                _suppressEvents = wasSuppressed;
            }
        }

        private void LoadLayoutProfileKnobs(DungeonLayoutProfile profile)
        {
            _currentLayoutProfile = profile;
            if (profile == null) return;

            // Load from the COMPOSED parameters (DungeonComposition.BuildLayoutParameters), not the
            // raw profile Template, so sliders show the values that actually drive generation --
            // e.g. Facility's Corridor Width shows the composed 2 (zsf01's MinimumOpeningWidth floor),
            // not the Complex profile's raw Template value of 1. Falls back to the raw Template when
            // no tileset is selected yet (BuildLayoutParameters needs a tileset to compose against).
            var tilesetProfile = SelectedTilesetProfile();
            var composed = tilesetProfile != null
                ? new DungeonComposition { Tileset = tilesetProfile, Layout = profile }.BuildLayoutParameters()
                : profile.Template;

            _suppressEvents = true;
            try
            {
                if (!_overriddenKnobs.Contains(nameof(_styleCombo)))
                    SelectComboByKey(_styleCombo, composed.Style.ToString());

                if (!_overriddenKnobs.Contains(nameof(_minRoomsSlider)))
                    _minRoomsSlider.Value = Clamp(composed.MinRooms, _minRoomsSlider.Minimum, _minRoomsSlider.Maximum);
                if (!_overriddenKnobs.Contains(nameof(_maxRoomsSlider)))
                    _maxRoomsSlider.Value = Clamp(composed.MaxRooms, _maxRoomsSlider.Minimum, _maxRoomsSlider.Maximum);
                if (!_overriddenKnobs.Contains(nameof(_minRoomSizeSlider)))
                    _minRoomSizeSlider.Value = Clamp(composed.MinRoomCornerSize, _minRoomSizeSlider.Minimum, _minRoomSizeSlider.Maximum);
                if (!_overriddenKnobs.Contains(nameof(_maxRoomSizeSlider)))
                    _maxRoomSizeSlider.Value = Clamp(composed.MaxRoomCornerSize, _maxRoomSizeSlider.Minimum, _maxRoomSizeSlider.Maximum);
                if (!_overriddenKnobs.Contains(nameof(_corridorWidthSlider)))
                    _corridorWidthSlider.Value = Clamp(composed.CorridorWidth, _corridorWidthSlider.Minimum, _corridorWidthSlider.Maximum);
                if (!_overriddenKnobs.Contains(nameof(_loopFactorSlider)))
                    _loopFactorSlider.Value = Clamp(Math.Round(composed.LoopFactor * 100), _loopFactorSlider.Minimum, _loopFactorSlider.Maximum);
                if (!_overriddenKnobs.Contains(nameof(_organicFillSlider)))
                    _organicFillSlider.Value = Clamp(Math.Round(composed.OpenFillTarget * 100), _organicFillSlider.Minimum, _organicFillSlider.Maximum);
                if (!_overriddenKnobs.Contains(nameof(_entrancesSlider)))
                    _entrancesSlider.Value = Clamp(composed.EntranceCount, _entrancesSlider.Minimum, _entrancesSlider.Maximum);
                if (!_overriddenKnobs.Contains(nameof(_exitsSlider)))
                    _exitsSlider.Value = Clamp(composed.ExitCount, _exitsSlider.Minimum, _exitsSlider.Maximum);
                if (!_overriddenKnobs.Contains(nameof(_doorTransitionsCheckBox)))
                    _doorTransitionsCheckBox.IsChecked = composed.DoorTransitions;
                if (!_overriddenKnobs.Contains(nameof(_elevationRegionsSlider)))
                    _elevationRegionsSlider.Value = Clamp(composed.ElevationRegions, _elevationRegionsSlider.Minimum, _elevationRegionsSlider.Maximum);

                var supportsAccent = tilesetProfile != null && !string.IsNullOrEmpty(tilesetProfile.AccentTerrain);

                if (!_overriddenKnobs.Contains(nameof(_accentCheckBox)))
                    _accentCheckBox.IsChecked = supportsAccent && composed.AccentDensity > 0;
                if (!_overriddenKnobs.Contains(nameof(_accentDensitySlider)))
                {
                    var density = composed.AccentDensity > 0 ? Math.Round(composed.AccentDensity * 100) : 5;
                    _accentDensitySlider.Value = Clamp(density, _accentDensitySlider.Minimum, _accentDensitySlider.Maximum);
                }

                _accentCheckBox.IsEnabled = supportsAccent;
                _accentDensitySlider.IsEnabled = supportsAccent && _accentCheckBox.IsChecked == true;

                UpdateOrganicFillEnabled();
                UpdateKnobConstraints();
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));

        // ------------------------------------------------------------------
        // Generation / preview
        // ------------------------------------------------------------------

        private void RegeneratePreview()
        {
            if (_suppressEvents) return;
            GeneratePreview();
        }

        private void GeneratePreview()
        {
            var theme = SelectedTheme();
            var tilesetProfile = SelectedTilesetProfile();
            var layoutProfile = SelectedLayoutProfile();

            if (theme == null || tilesetProfile == null || layoutProfile == null || _currentLayoutProfile == null)
            {
                SetStatus("No composition selected.");
                return;
            }

            TilesetModel tilesetModel;
            try
            {
                tilesetModel = TilesetModelCache.Get(tilesetProfile.TilesetResref);
            }
            catch (Exception ex)
            {
                _lastResult = new GenerationResult { Success = false, FailureReason = $"Tileset load failed: {ex.Message}" };
                RenderPreview();
                SetStatus(_lastResult.FailureReason);
                return;
            }

            // Composition, not the raw layout Template, is the single source of truth: BuildLayoutParameters
            // (called inside GenerationEngine.Generate) stamps SecondaryOpenTerrain, the tileset's
            // CorridorWidth floor, ChannelTerrain, and FeatureTiles/SetPieces/ExitGroups -- exactly what
            // SWLOR.ProcgenReview composes with. Sliders were themselves loaded FROM these composed values
            // (see LoadLayoutProfileKnobs), so applying every current slider value on top is lossless for
            // knobs the user never touched and correct for the ones they did.
            var composition = new DungeonComposition { Content = theme, Tileset = tilesetProfile, Layout = layoutProfile };

            var overrides = new LayoutKnobOverrides
            {
                Style = SelectedStyle(),
                MinRooms = (int)_minRoomsSlider.Value,
                MaxRooms = (int)_maxRoomsSlider.Value,
                MinRoomCornerSize = (int)_minRoomSizeSlider.Value,
                MaxRoomCornerSize = (int)_maxRoomSizeSlider.Value,
                CorridorWidth = (int)_corridorWidthSlider.Value,
                LoopFactorPercent = (int)_loopFactorSlider.Value,
                OpenFillTargetPercent = (int)_organicFillSlider.Value,
                EntranceCount = (int)_entrancesSlider.Value,
                ExitCount = (int)_exitsSlider.Value,
                DoorTransitions = _doorTransitionsCheckBox.IsChecked == true,
                AccentEnabled = _accentCheckBox.IsChecked == true,
                AccentDensityPercent = (int)_accentDensitySlider.Value,
                FeatureDensityPercent = (int)_featureDensitySlider.Value,
                ElevationRegions = (int)_elevationRegionsSlider.Value
            };

            var width = (int)_widthSlider.Value;
            var height = (int)_heightSlider.Value;
            var seed = GetSeedValue();

            var result = GenerationEngine.Generate(composition, tilesetModel, width, height, seed, overrides);
            _lastResult = result;

            RenderPreview();

            if (!result.Success)
            {
                SetStatus($"Generation failed: {result.FailureReason}");
                return;
            }

            var openPercent = ComputeOpenPercent(result.Layout, result.Parameters.OpenTerrain);
            SetStatus($"rooms: {result.Layout.Rooms.Count} | open corners: {openPercent:0}% | attempt seed: {result.AttemptSeed}");
        }

        /// <summary>
        /// Redraws the last generation result (success schematic or failure text) into the preview
        /// image, sized to the preview host panel. Sizing must come from PreviewHost — an Image with
        /// a null Source measures to 0x0, so the Image's own ActualWidth is useless pre-first-render.
        /// </summary>
        private void RenderPreview()
        {
            if (_lastResult == null) return;

            var actualWidth = PreviewHost.ActualWidth - PreviewHost.BorderThickness.Left - PreviewHost.BorderThickness.Right;
            var actualHeight = PreviewHost.ActualHeight - PreviewHost.BorderThickness.Top - PreviewHost.BorderThickness.Bottom;
            if (actualWidth < 1 || actualHeight < 1) return;

            if (!_lastResult.Success)
            {
                PreviewImage.Source = SchematicRenderer.RenderMessage($"Generation failed:\n{_lastResult.FailureReason}", actualWidth, actualHeight);
                return;
            }

            if (IsMapGraphicsMode && _lastResult.Resolved != null)
            {
                PreviewImage.Source = MapGraphicsRenderer.Render(
                    _lastResult.Resolved,
                    _lastResult.Tileset,
                    RoomOverlayCheckBox.IsChecked == true,
                    actualWidth,
                    actualHeight,
                    out var stats);

                ReportMapGraphicsStats(stats);
            }
            else
            {
                PreviewImage.Source = SchematicRenderer.Render(_lastResult.Layout, _lastResult.Parameters, actualWidth, actualHeight);
            }
        }

        /// <summary>
        /// Surfaces where minimap tile art came from (or that it's missing) in the status bar,
        /// without clobbering the rooms/open-percent summary GeneratePreview already set — Map
        /// graphics mode appends its own line instead of overwriting.
        /// </summary>
        private void ReportMapGraphicsStats(MapRenderStats stats)
        {
            if (stats == null) return;

            var message = stats.Misses > 0
                ? $"map art: {stats.LooseHits} loose, {stats.ArchiveHits} base-game, {stats.Misses} tiles without minimap art"
                : $"map art: {stats.LooseHits} loose, {stats.ArchiveHits} base-game";

            if (!string.IsNullOrEmpty(stats.BaseGameArchiveStatus))
                message += $" (base game archive unavailable: {stats.BaseGameArchiveStatus})";

            AppendLog(message);
        }

        private static double ComputeOpenPercent(MacroLayout layout, string openTerrain)
        {
            var total = (layout.Corners.Width + 1) * (layout.Corners.Height + 1);
            if (total == 0) return 0;

            var open = 0;
            for (var x = 0; x <= layout.Corners.Width; x++)
            for (var y = 0; y <= layout.Corners.Height; y++)
                if (layout.Corners.Labels[x, y] == openTerrain) open++;

            return 100.0 * open / total;
        }

        /// <summary>Updates the status bar and mirrors the message into the log so app state is inspectable after the fact.</summary>
        private void SetStatus(string text)
        {
            StatusTextBlock.Text = text;
            AppendLog(text);
        }

        private static string NewRandomSeedText() =>
            System.Random.Shared.Next(0, MaxSeed + 1).ToString(CultureInfo.InvariantCulture);

        private int GetSeedValue()
        {
            if (int.TryParse(_seedTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                return Math.Clamp(value, 0, MaxSeed);
            return 4242;
        }

        // ------------------------------------------------------------------
        // Batch
        // ------------------------------------------------------------------

        private void AddToBatch()
        {
            var theme = SelectedTheme();
            var tilesetProfile = SelectedTilesetProfile();
            var layoutProfile = SelectedLayoutProfile();
            if (theme == null || tilesetProfile == null || layoutProfile == null) return;

            var width = (int)_widthSlider.Value;
            var height = (int)_heightSlider.Value;
            if (width != height)
            {
                AppendLog($"Width ({width}) and Height ({height}) differ; the review module builder " +
                          $"only supports square areas, so {width} will be used for both when building.");
            }

            // Force a fresh preview generation so _lastResult.Parameters exactly mirrors the current
            // UI state (composition + every Advanced-settings knob) before snapshotting it into the
            // batch item -- this is what guarantees the review module reproduces this exact preview
            // rather than silently dropping any override, as the old theme:tileset:layout:seed:size
            // string spec did.
            GeneratePreview();
            if (_lastResult is not { Success: true })
            {
                AppendLog("Cannot add to batch: the current composition failed to generate.");
                return;
            }

            var item = new BatchItem
            {
                ThemeKey = theme.ThemeKey,
                ThemeDisplayName = theme.DisplayName,
                TilesetProfileKey = tilesetProfile.Key == theme.TilesetProfileKey ? string.Empty : tilesetProfile.Key,
                TilesetDisplayName = tilesetProfile.DisplayName,
                LayoutProfileKey = layoutProfile.Key == theme.LayoutProfileKey ? string.Empty : layoutProfile.Key,
                LayoutDisplayName = layoutProfile.DisplayName,
                Seed = GetSeedValue(),
                Size = width,
                Entrances = (int)_entrancesSlider.Value,
                Exits = (int)_exitsSlider.Value,
                DoorTransitions = _doorTransitionsCheckBox.IsChecked == true,
                Parameters = _lastResult.Parameters.Clone()
            };

            _batch.Add(item);
        }

        private void RemoveSelectedBatchItems()
        {
            foreach (var item in _batchGrid.SelectedItems.Cast<BatchItem>().ToList())
                _batch.Remove(item);
        }

        // ------------------------------------------------------------------
        // Build Review Module
        // ------------------------------------------------------------------

        private async Task BuildReviewModuleAsync()
        {
            if (_batch.Count == 0)
            {
                AppendLog("Batch is empty; nothing to build.");
                return;
            }

            _buildModuleButton.IsEnabled = false;
            string areasFilePath = null;
            try
            {
                // Ships the full effective MacroLayoutParameters for every batch entry (see
                // BatchItem.Parameters / AreaBatchFile) instead of the lossy theme:tileset:layout:
                // seed:size string spec, so every Advanced-settings override survives into the built
                // module. --areas-file entries are consumed verbatim by ProcgenReview.
                var entries = _batch.Select(b => new AreaBatchFileEntry
                {
                    ThemeKey = b.ThemeKey,
                    TilesetKey = b.TilesetProfileKey,
                    LayoutKey = b.LayoutProfileKey,
                    Seed = b.Seed,
                    Size = b.Size,
                    Parameters = b.Parameters
                }).ToList();

                areasFilePath = Path.Combine(Path.GetTempPath(), $"swlor_contentbuilder_areas_{Guid.NewGuid():N}.json");
                File.WriteAllText(areasFilePath, AreaBatchFile.Serialize(entries));

                var outPath = RepoPaths.ReviewModuleOutputPath;
                var projectPath = RepoPaths.ProcgenReviewProjectPath;

                AppendLog($"Building review module with {_batch.Count} area(s)...");

                var arguments = $"run --project \"{projectPath}\" -- --areas-file \"{areasFilePath}\" --out \"{outPath}\"";
                var exitCode = await RunProcessAsync("dotnet", arguments);

                AppendLog(exitCode == 0
                    ? $"Build succeeded: {outPath}"
                    : $"Build failed (exit code {exitCode}).");
            }
            catch (Exception ex)
            {
                AppendLog($"Build failed: {ex.Message}");
            }
            finally
            {
                _buildModuleButton.IsEnabled = true;
                if (areasFilePath != null)
                {
                    try { File.Delete(areasFilePath); }
                    catch { /* best-effort scratch-file cleanup */ }
                }
            }
        }

        private Task<int> RunProcessAsync(string fileName, string arguments)
        {
            var tcs = new TaskCompletionSource<int>();

            var psi = new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = RepoPaths.Root
            };

            var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, e) => { if (e.Data != null) AppendLog(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) AppendLog(e.Data); };
            process.Exited += (_, _) =>
            {
                tcs.TrySetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return tcs.Task;
        }

        private void AppendLog(string line)
        {
            if (!_logTextBox.Dispatcher.CheckAccess())
            {
                _logTextBox.Dispatcher.Invoke(() => AppendLog(line));
                return;
            }

            _logTextBox.AppendText(line + Environment.NewLine);
            _logTextBox.ScrollToEnd();
        }
    }
}
