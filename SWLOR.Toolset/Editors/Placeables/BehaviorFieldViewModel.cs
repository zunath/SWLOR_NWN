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

        /// <summary>
        /// How many filtered options a searchable row publishes before it stops. Every published
        /// option is a control realized; a builder narrows the search rather than scrolling past the
        /// two hundredth result.
        /// </summary>
        public const int MaxSearchResults = 200;
        private readonly EditorFieldContext _context;
        private readonly PlaceableBehaviorField _field;
        private readonly BehaviorValueSourceProvider _sources;
        private List<BehaviorChoiceOption> _options;
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
            _options = sources.GetOptions(field.Source).ToList();

            RefreshFromDocument();
            RebuildSearchableOptions();
            RebuildGallery();
        }

        public string Label => _field.Label;
        public string VariableName => _field.VariableName;
        public string? Description => _field.Description;
        public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
        public bool IsRequired => _field.IsRequired;
        public decimal Minimum => _field.Minimum ?? int.MinValue;
        public decimal Maximum => _field.Maximum ?? int.MaxValue;

        public IReadOnlyList<BehaviorChoiceOption> Options => _options;
        public ObservableCollection<BehaviorChoiceOption> SearchableOptions { get; } = new();
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
                                    !IsSearchableTableChoice &&
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

        /// <summary>
        /// A server-declared table: spawn or loot. Both use a visible, searchable select list, so a
        /// builder can browse every valid option without knowing part of its name first, while
        /// selection still writes the exact declared id.
        /// </summary>
        /// <remarks>
        /// Loot tables used to be a drop-down on the grounds that they were a finite set. There are
        /// 490 of them, which is a set a builder scrolls rather than reads.
        /// </remarks>
        public bool IsSearchableTableChoice => _field.Kind == PlaceableFieldKind.Choice &&
                                               _field.Source is PlaceableValueSource.SpawnTables
                                                   or PlaceableValueSource.LootTables &&
                                               _field.VarType == VarTable.TypeString &&
                                               Options.Count > 0;

        public bool IsSearchableChoice => IsSearchableIdChoice || IsSearchableTableChoice;

        /// <summary>A short id-valued choice (skill or market region), rendered as a combo box.</summary>
        public bool IsIdChoice => _field.Kind == PlaceableFieldKind.Choice &&
                                  _field.VarType == VarTable.TypeInt &&
                                  !IsGalleryChoice &&
                                  !IsSearchableIdChoice &&
                                  Options.Count > 0;

        /// <summary>Free text, and the fallback whenever a choice source produced no options.</summary>
        public bool IsText => !IsToggle && !IsInteger && !IsNameChoice &&
                              !IsSearchableChoice && !IsIdChoice && !IsGalleryChoice;
        public string SelectedDisplay => SelectedOption?.Display ??
                                         (string.IsNullOrWhiteSpace(Text)
                                             ? _field.EmptyChoiceLabel
                                             : Text);
        public string? SelectedDetails => SelectedOption?.Details;
        public bool HasSelectedDetails => !string.IsNullOrWhiteSpace(SelectedDetails);
        public string ClearChoiceLabel => _field.ClearChoiceLabel;
        public bool CanLoadMore => _galleryPublished < _galleryMatches.Count;
        public bool CanClearChoice => _field.Kind == PlaceableFieldKind.Choice &&
                                      !IsRequired &&
                                      _hasStoredValue;
        public string GallerySearchWatermark =>
            _field.Source == PlaceableValueSource.VisualEffects
                ? "Search by name, ResRef, group, color, or location"
                : "Search by name or ResRef";
        public string GallerySummary => _galleryMatches.Count == 0
            ? "No matches"
            : _galleryPublished >= _galleryMatches.Count
                ? $"{_galleryMatches.Count} choice{(_galleryMatches.Count == 1 ? string.Empty : "s")}"
                : $"{_galleryPublished} of {_galleryMatches.Count} choices";
        public string ChoiceSearchWatermark => $"Search {SearchableChoiceNoun}s by name";

        /// <summary>What the searchable list is a list of, for its watermark and count line.</summary>
        private string SearchableChoiceNoun => _field.Source switch
        {
            PlaceableValueSource.SpawnTables => "spawn table",
            PlaceableValueSource.LootTables => "loot table",
            _ => "key item"
        };

        public string SearchableChoiceSummary
        {
            get
            {
                var noun = SearchableChoiceNoun;
                if (SearchableOptions.Count == 0)
                    return $"No matching {noun}s";

                return SearchableOptions.Count == Options.Count
                    ? $"{Options.Count} {noun}{(Options.Count == 1 ? string.Empty : "s")}"
                    : $"{SearchableOptions.Count} of {Options.Count} {noun}s";
            }
        }

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

                    // Keep a legacy or misspelled stored table visible in the selector so opening
                    // the editor never hides data. It remains marked dangling until replaced.
                    if (SelectedOption == null &&
                        IsSearchableTableChoice &&
                        !string.IsNullOrWhiteSpace(Text))
                    {
                        var missing = new BehaviorChoiceOption(Text, $"{Text} (missing)");
                        _options.Add(missing);
                        SelectedOption = missing;
                    }
                }

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
        partial void OnChoiceSearchTextChanged(string value) => RebuildSearchableOptions();

        [RelayCommand]
        private void PickChoice(BehaviorGalleryTileViewModel? tile)
        {
            if (tile == null)
                return;

            SelectedOption = tile.Choice;
        }

        [RelayCommand]
        private void PickSearchableChoice(BehaviorChoiceOption? option)
        {
            if (option != null)
                SelectedOption = option;
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

        /// <summary>
        /// Re-reads this field's options from the source and republishes everything derived from
        /// them.
        /// </summary>
        /// <remarks>
        /// The module-wide scans start when the first placeable opens, so a field built in that
        /// same moment cached an empty list — and an empty list makes the field fall back to plain
        /// free text with no suggestions and no resolution check. Nothing put the real options back
        /// when the scan landed, so Teleporter destinations and Quest Activator waypoint tags stayed
        /// bare until the tab was closed and reopened.
        /// </remarks>
        public void RefreshOptions()
        {
            var rebuilt = _sources.GetOptions(_field.Source).ToList();
            if (rebuilt.Count == 0 && _options.Count > 0)
                return;

            _options = rebuilt;

            OnPropertyChanged(nameof(Options));
            OnPropertyChanged(nameof(IsInteger));
            OnPropertyChanged(nameof(IsGalleryChoice));
            OnPropertyChanged(nameof(IsNameChoice));
            OnPropertyChanged(nameof(IsSearchableIdChoice));
            OnPropertyChanged(nameof(IsSearchableTableChoice));
            OnPropertyChanged(nameof(IsSearchableChoice));
            OnPropertyChanged(nameof(IsIdChoice));
            OnPropertyChanged(nameof(IsText));

            // Re-reads the stored value against the new options, which is what turns a value that
            // was reported as unresolvable back into a recognized selection.
            RefreshFromDocument();
            RebuildSearchableOptions();
            RebuildGallery();
        }

        private void RebuildSearchableOptions()
        {
            if (!IsSearchableChoice)
                return;

            var query = ChoiceSearchText.Trim();
            var matches = Options.Where(option =>
                query.Length == 0 ||
                option.Display.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (option.Details?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));

            SearchableOptions.Clear();
            foreach (var option in matches.Take(MaxSearchResults))
                SearchableOptions.Add(option);

            // A filter excludes a non-match on purpose; the cap excludes one by accident. A legacy
            // or misspelled table is appended past every real option, so truncating is exactly what
            // would hide it - and a value the editor will not show is one a builder cannot see.
            if (SearchableOptions.Count >= MaxSearchResults &&
                SelectedOption != null &&
                !SearchableOptions.Contains(SelectedOption))
            {
                SearchableOptions.Insert(0, SelectedOption);
            }

            OnPropertyChanged(nameof(SearchableChoiceSummary));
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
                // Not "required" - the label already says that, under the field's own name. Saying
                // it a second time beside the control puts the word on screen twice for one fact,
                // and leaves no room for the status line's real job below.
                Status = IsRequired ? BehaviorValueStatus.Missing : BehaviorValueStatus.None;
                StatusText = null;
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
                PlaceableValueSource.PlaceableBlueprints => "no placeable blueprint with this ResRef",
                PlaceableValueSource.CreatureBlueprints => "no creature blueprint with this ResRef",
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
