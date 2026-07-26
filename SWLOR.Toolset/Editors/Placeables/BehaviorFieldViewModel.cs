using System.Globalization;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Placeables;

namespace SWLOR.Toolset.Editors.Placeables
{
    /// <summary>
    /// One typed control over a behavior's local variable. Reads and writes the VarTable through
    /// the editor's transaction pipeline, so a behavior field undoes exactly like a GFF field.
    /// </summary>
    /// <remarks>
    /// Clearing a value removes the variable rather than storing an empty string. An unset local and
    /// a local set to "" mean the same thing to the game, and leaving the row behind makes a
    /// placeable look configured when it is not.
    /// </remarks>
    public partial class BehaviorFieldViewModel : ObservableObject
    {
        private const int GalleryPageSize = 48;
        private readonly EditorFieldContext _context;
        private readonly PlaceableBehaviorField _field;
        private readonly BehaviorValueSourceProvider _sources;
        private List<BehaviorChoiceOption> _galleryMatches = new();
        private int _galleryPublished;
        private bool _hasStoredValue;

        [ObservableProperty]
        private string _text = string.Empty;

        [ObservableProperty]
        private long _number;

        [ObservableProperty]
        private bool _flag;

        [ObservableProperty]
        private BehaviorChoiceOption? _selectedOption;

        [ObservableProperty]
        private BehaviorValueStatus _status;

        [ObservableProperty]
        private string? _statusText;

        [ObservableProperty]
        private string _galleryQuery = string.Empty;

        [ObservableProperty]
        private string _choiceSearchText = string.Empty;

        public BehaviorFieldViewModel(
            PlaceableBehaviorField field,
            EditorFieldContext context,
            BehaviorValueSourceProvider sources)
        {
            _field = field;
            _context = context;
            _sources = sources;
            Options = sources.GetOptions(field.Source);

            RefreshFromDocument();
            RebuildGallery();
        }

        public string Label => _field.Label;
        public string VariableName => _field.VariableName;
        public string? Description => _field.Description;
        public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
        public bool IsRequired => _field.IsRequired;
        public decimal Minimum => _field.Minimum ?? int.MinValue;
        public decimal Maximum => _field.Maximum ?? int.MaxValue;

        public IReadOnlyList<BehaviorChoiceOption> Options { get; }
        public ObservableCollection<BehaviorGalleryTileViewModel> GalleryTiles { get; } = new();

        public bool IsToggle => _field.Kind == PlaceableFieldKind.Toggle;
        public bool IsInteger => _field.Kind == PlaceableFieldKind.Integer ||
                                 (_field.Kind == PlaceableFieldKind.Choice &&
                                  _field.VarType == VarTable.TypeInt &&
                                  Options.Count == 0);
        public bool IsGalleryChoice =>
            _field.Kind == PlaceableFieldKind.Choice &&
            _field.Source is PlaceableValueSource.PlaceableBlueprints
                or PlaceableValueSource.CreatureBlueprints
                or PlaceableValueSource.VisualEffects &&
            Options.Count > 0;

        /// <summary>
        /// A name-valued choice (loot table, quest, tag). Rendered as a suggestion box rather than a
        /// combo: the tag source alone offers five figures of options, and an unknown stored value
        /// has to remain visible and editable instead of showing blank.
        /// </summary>
        public bool IsNameChoice => _field.Kind == PlaceableFieldKind.Choice &&
                                    _field.VarType == VarTable.TypeString &&
                                    !IsGalleryChoice &&
                                    Options.Count > 0;

        /// <summary>
        /// Key items are numerous enough to need a searchable selector. The text filters choices,
        /// while only selecting a real option writes its numeric id to the placeable.
        /// </summary>
        public bool IsSearchableIdChoice => _field.Kind == PlaceableFieldKind.Choice &&
                                            _field.Source == PlaceableValueSource.KeyItems &&
                                            _field.VarType == VarTable.TypeInt &&
                                            Options.Count > 0;

        /// <summary>A short id-valued choice (skill or market region), rendered as a combo box.</summary>
        public bool IsIdChoice => _field.Kind == PlaceableFieldKind.Choice &&
                                  _field.VarType == VarTable.TypeInt &&
                                  !IsGalleryChoice &&
                                  !IsSearchableIdChoice &&
                                  Options.Count > 0;

        /// <summary>Free text, and the fallback whenever a choice source produced no options.</summary>
        public bool IsText => !IsToggle && !IsInteger && !IsNameChoice && !IsSearchableIdChoice &&
                              !IsIdChoice && !IsGalleryChoice;
        public string SelectedDisplay => SelectedOption?.Display ??
                                         (string.IsNullOrWhiteSpace(Text) ? "Not selected" : Text);
        public string? SelectedDetails => SelectedOption?.Details;
        public bool HasSelectedDetails => !string.IsNullOrWhiteSpace(SelectedDetails);
        public bool CanLoadMore => _galleryPublished < _galleryMatches.Count;
        public bool CanClearChoice => _field.Kind == PlaceableFieldKind.Choice &&
                                      !IsRequired &&
                                      _hasStoredValue;
        public string GallerySearchWatermark =>
            _field.Source == PlaceableValueSource.VisualEffects
                ? "Search by name, resref, group, color, or location"
                : "Search by name or resref";
        public string GallerySummary => _galleryMatches.Count == 0
            ? "No matches"
            : _galleryPublished >= _galleryMatches.Count
                ? $"{_galleryMatches.Count} choice{(_galleryMatches.Count == 1 ? string.Empty : "s")}"
                : $"{_galleryPublished} of {_galleryMatches.Count} choices";

        public void RefreshFromDocument()
        {
            var wasRefreshing = _context.IsRefreshing;
            _context.IsRefreshing = true;

            try
            {
                var table = new VarTable(_context.Document.Root);
                var entry = table.FirstOrDefault(candidate =>
                    string.Equals(candidate.Name, _field.VariableName, StringComparison.Ordinal));
                _hasStoredValue = entry != null;

                if (_field.VarType == VarTable.TypeInt)
                {
                    var value = entry?.IntValue ?? 0;
                    Number = value;
                    Flag = value != 0;
                    Text = _hasStoredValue ? value.ToString(CultureInfo.InvariantCulture) : string.Empty;
                    SelectedOption = Options.FirstOrDefault(option =>
                        string.Equals(option.Value, Text, StringComparison.Ordinal));
                }
                else
                {
                    Text = entry?.StringValue ?? string.Empty;
                    SelectedOption = Options.FirstOrDefault(option =>
                        string.Equals(option.Value, Text, StringComparison.OrdinalIgnoreCase));
                }

                ChoiceSearchText = SelectedOption?.Display ?? string.Empty;
                UpdateSelectedChoice(SelectedOption);
                UpdateStatus();
                OnPropertyChanged(nameof(CanClearChoice));
            }
            finally
            {
                _context.IsRefreshing = wasRefreshing;
            }
        }

        partial void OnTextChanged(string value)
        {
            if (_context.IsRefreshing || _field.VarType != VarTable.TypeString)
                return;

            Write(table =>
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    table.Remove(_field.VariableName);
                    _hasStoredValue = false;
                }
                else
                {
                    table.SetString(_field.VariableName, value);
                    _hasStoredValue = true;
                }
            });
        }

        partial void OnNumberChanged(long value)
        {
            if (_context.IsRefreshing || !IsInteger)
                return;

            var clamped = Math.Clamp(
                value,
                _field.Minimum ?? int.MinValue,
                _field.Maximum ?? int.MaxValue);
            if (clamped != value)
            {
                Number = clamped;
                return;
            }

            Write(table =>
            {
                table.SetInt(_field.VariableName, (int)value);
                _hasStoredValue = true;
            });
        }

        partial void OnFlagChanged(bool value)
        {
            if (_context.IsRefreshing || !IsToggle)
                return;

            Write(table =>
            {
                if (value)
                    table.SetInt(_field.VariableName, 1);
                else
                    table.Remove(_field.VariableName);
            });
        }

        partial void OnSelectedOptionChanged(BehaviorChoiceOption? value)
        {
            UpdateSelectedChoice(value);
            if (_context.IsRefreshing || value == null)
                return;

            if (IsSearchableIdChoice)
                ChoiceSearchText = value.Display;

            if (_field.VarType == VarTable.TypeInt)
            {
                if (int.TryParse(value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                {
                    Write(table =>
                    {
                        table.SetInt(_field.VariableName, parsed);
                        _hasStoredValue = true;
                    });
                }

                return;
            }

            Text = value.Value;
        }

        partial void OnGalleryQueryChanged(string value) => RebuildGallery();

        [RelayCommand]
        private void PickChoice(BehaviorGalleryTileViewModel? tile)
        {
            if (tile == null)
                return;

            SelectedOption = tile.Choice;
        }

        [RelayCommand]
        private void LoadMoreGallery()
        {
            PublishGalleryPage();
        }

        [RelayCommand]
        private void ClearChoice()
        {
            if (!CanClearChoice)
                return;

            var applied = _context.RunEdit(
                $"Clear {Label}",
                () => new VarTable(_context.Document.Root).Remove(_field.VariableName));
            if (!applied)
            {
                RefreshFromDocument();
                return;
            }

            var wasRefreshing = _context.IsRefreshing;
            _context.IsRefreshing = true;
            try
            {
                _hasStoredValue = false;
                SelectedOption = null;
                Text = string.Empty;
                Number = 0;
                ChoiceSearchText = string.Empty;
            }
            finally
            {
                _context.IsRefreshing = wasRefreshing;
            }

            UpdateSelectedChoice(null);
            UpdateStatus();
            OnPropertyChanged(nameof(CanClearChoice));
        }

        private void Write(Action<VarTable> mutation)
        {
            var applied = _context.RunEdit($"Change {Label}",
                () => mutation(new VarTable(_context.Document.Root)));

            if (applied)
            {
                UpdateStatus();
                OnPropertyChanged(nameof(CanClearChoice));
            }
            else
                RefreshFromDocument();
        }

        private void UpdateStatus()
        {
            var stored = _field.VarType == VarTable.TypeInt
                ? (_hasStoredValue ? (SelectedOption?.Value ?? Number.ToString(CultureInfo.InvariantCulture)) : string.Empty)
                : Text;

            if (string.IsNullOrWhiteSpace(stored))
            {
                Status = IsRequired ? BehaviorValueStatus.Missing : BehaviorValueStatus.None;
                StatusText = IsRequired ? "required" : null;
                return;
            }

            if (_field.Source == PlaceableValueSource.None)
            {
                Status = BehaviorValueStatus.None;
                StatusText = null;
                return;
            }

            if (_sources.IsKnown(_field.Source, stored))
            {
                Status = BehaviorValueStatus.Resolves;
                StatusText = null;
                return;
            }

            Status = BehaviorValueStatus.Dangling;
            StatusText = _field.Source switch
            {
                PlaceableValueSource.ObjectTags => "no object in the module carries this tag",
                PlaceableValueSource.LootTables => "no loot table is declared with this name",
                PlaceableValueSource.Dialogs => "no conversation class with this name",
                PlaceableValueSource.Quests => "no quest with this id",
                PlaceableValueSource.SpawnTables => "no spawn table with this id",
                PlaceableValueSource.PlaceableBlueprints => "no placeable blueprint with this resref",
                PlaceableValueSource.CreatureBlueprints => "no creature blueprint with this resref",
                _ => "not a known value"
            };
        }

        private void RebuildGallery()
        {
            if (!IsGalleryChoice)
                return;

            var query = GalleryQuery.Trim();
            _galleryMatches = Options
                .Where(option => query.Length == 0 ||
                                 option.Display.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                 option.Value.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                 (option.Group?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                 (option.Details?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

            GalleryTiles.Clear();
            _galleryPublished = 0;
            PublishGalleryPage();
        }

        private void PublishGalleryPage()
        {
            foreach (var option in _galleryMatches
                         .Skip(_galleryPublished)
                         .Take(GalleryPageSize))
            {
                var tile = new BehaviorGalleryTileViewModel(option, _field.Source, _sources)
                {
                    IsSelected = IsSelectedOption(option)
                };
                GalleryTiles.Add(tile);
                tile.EnsurePreview();
            }

            _galleryPublished = GalleryTiles.Count;
            OnPropertyChanged(nameof(CanLoadMore));
            OnPropertyChanged(nameof(GallerySummary));
        }

        private void UpdateSelectedChoice(BehaviorChoiceOption? value)
        {
            foreach (var tile in GalleryTiles)
                tile.IsSelected = value != null && IsSelectedOption(tile.Choice);

            OnPropertyChanged(nameof(SelectedDisplay));
            OnPropertyChanged(nameof(SelectedDetails));
            OnPropertyChanged(nameof(HasSelectedDetails));
        }

        private bool IsSelectedOption(BehaviorChoiceOption option) =>
            SelectedOption != null &&
            string.Equals(option.Value, SelectedOption.Value, StringComparison.OrdinalIgnoreCase);
    }
}
