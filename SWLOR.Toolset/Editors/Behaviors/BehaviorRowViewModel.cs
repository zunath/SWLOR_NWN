using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Editors.Behaviors
{
    /// <summary>
    /// One label-and-value row in a behavior editor: reads its value out of the object, writes it
    /// back through the owning session's transaction, and exposes the kind flags a single template
    /// switches on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately one class rather than one per field kind, and one class rather than one per
    /// editor. Every row shares the same geometry and the same read/write pipeline; only the control
    /// in the value cell changes. The trigger, waypoint, door, and sound editors each used to carry
    /// their own copy of this logic, which is how they came to disagree about whether a required
    /// choice row counts as filled in.
    /// </para>
    /// <para>
    /// Editors that need more than the shared shape override the four write hooks and
    /// <see cref="ReadValue"/>; everything inside a hook runs inside the same transaction as the
    /// write, so a derived mutation is part of the same undo step.
    /// </para>
    /// </remarks>
    public partial class BehaviorRowViewModel : ObservableObject, IDisposable
    {
        /// <summary>Gallery tiles published per page, and per scroll once the builder reaches the end.</summary>
        public const int GalleryPageSize = 48;

        /// <summary>How long typing pauses before the gallery re-filters.</summary>
        private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(250);

        /// <summary>How many filtered options a searchable row publishes before it stops.</summary>
        /// <remarks>
        /// The tag source alone offers five figures of options, and every published row is a control
        /// realized. A builder narrows the search rather than scrolling past the two hundredth
        /// result, so the cap costs nothing and keeps a keystroke from realizing thousands of rows.
        /// </remarks>
        public const int MaxSearchResults = 200;

        /// <summary>How many search matches are published on first open and per explicit load.</summary>
        /// <remarks>
        /// Search rows live inside larger editor forms. Publishing even the full capped set for
        /// every field at once still realizes hundreds of buttons while changing behaviors. A
        /// page keeps the initial interaction bounded; the virtualized list and search box handle
        /// browsing, and builders can request another page when they actually need it.
        /// </remarks>
        public const int SearchPageSize = 50;

        private readonly Action? _valueChanged;
        private readonly ChoicePreviewService? _previews;
        private readonly Func<BehaviorChoice, string?>? _previewAudio;
        private readonly Func<string, int, int, Task<IReadOnlyList<BehaviorChoice>>>? _choicePageLoader;
        private readonly bool _forceInlineSearch;
        private Func<IReadOnlyList<BehaviorChoice>>? _choiceLoader;
        private Func<Task<IReadOnlyList<BehaviorChoice>>>? _asyncChoiceLoader;
        private List<BehaviorChoiceViewModel> _searchMatches = new();
        private List<BehaviorChoiceViewModel> _galleryMatches = new();
        private CancellationTokenSource? _searchDebounce;
        private int _searchPublished;
        private int _galleryPublished;
        private int _choicePageOffset;
        private int _choicePageRequestGeneration;
        private int _galleryRebuildGeneration;
        private bool _choicePagesActivated;
        private bool _choicePagesExhausted;
        private bool _choicePageLoading;
        private bool _reusePagedChoicesOnNextRebuild;
        private bool _galleryBuilt;
        private bool _galleryControlsBuilt;
        private bool _suppressAutomaticGalleryRebuild;
        private bool _disposed;
        private bool _loading;
        private BehaviorChoiceViewModel? _markedChoice;

        public BehaviorFieldDefinition Definition { get; }

        protected BehaviorValueStore Store { get; }

        protected Func<string, Action, bool> RunEditFunc { get; }

        /// <summary>True while <see cref="Reload"/> is assigning; suppresses write-back.</summary>
        protected bool IsLoading => _loading;

        public string Label => Definition.Label;

        public bool IsRequired => Definition.IsRequired;

        public bool IsReadOnly => Definition.IsReadOnly;

        public string? Note => Definition.Note;

        public virtual bool HasNote => !string.IsNullOrEmpty(Definition.Note);

        /// <summary>Characters the box accepts; 0 lets Avalonia treat it as unlimited.</summary>
        public int MaxLength => Definition.MaxLength;

        /// <summary>
        /// "12/32", shown for as long as the row has a limit rather than only near it. A cap a
        /// builder cannot see until they hit it reads as the box breaking.
        /// </summary>
        public string? Counter => MaxLength > 0 ? $"{Text.Length}/{MaxLength}" : null;

        public bool HasCounter => MaxLength > 0;

        private IReadOnlyList<BehaviorChoiceViewModel> _choices =
            Array.Empty<BehaviorChoiceViewModel>();

        /// <summary>The choice models, resolved only when a deferred picker is opened.</summary>
        public IReadOnlyList<BehaviorChoiceViewModel> Choices
        {
            get => _choices;
            private set
            {
                if (!SetProperty(ref _choices, value))
                    return;

                _galleryRebuildGeneration++;
                _galleryControlsBuilt = false;
                GalleryFilters.Clear();
                GallerySortOptions.Clear();
                _selectedGallerySort = null;
                NotifyChoicePresentationChanged();
            }
        }

        /// <summary>Whether this row has paid the cost of resolving and wrapping its option set.</summary>
        public bool AreChoicesLoaded =>
            _choiceLoader == null &&
            _asyncChoiceLoader == null &&
            (_choicePageLoader == null || _choicePagesActivated && !_choicePageLoading);

        /// <summary>True while a repository-backed gallery is resolving its next bounded page.</summary>
        public bool IsGalleryLoading => _choicePageLoading;

        /// <summary>The filtered slice of <see cref="Choices"/> a searchable row shows.</summary>
        public ObservableCollection<BehaviorChoiceViewModel> FilteredChoices { get; } = new();

        /// <summary>The published page of gallery tiles, for a choice row whose options have artwork.</summary>
        public ObservableCollection<BehaviorChoiceViewModel> GalleryChoices { get; } = new();

        /// <summary>
        /// Facet controls discovered from the current visual choices. The gallery owns this once;
        /// individual editors only describe their choices.
        /// </summary>
        public ObservableCollection<GalleryFilterViewModel> GalleryFilters { get; } = new();

        public ObservableCollection<GallerySortOption> GallerySortOptions { get; } = new();

        public bool HasGalleryFilters => GalleryFilters.Count > 0;

        public bool IsText => Definition.Kind is BehaviorFieldKind.Text or BehaviorFieldKind.Script;
        public bool IsLocalizedText => Definition.Kind == BehaviorFieldKind.LocalizedText;
        public bool IsParagraph => Definition.Kind == BehaviorFieldKind.Paragraph;
        public bool IsNumber => Definition.Kind is BehaviorFieldKind.Integer or BehaviorFieldKind.Float;

        /// <summary>
        /// The lowest value the spinner will offer, from the field's own floor when it declares one
        /// and otherwise from what its GFF storage type can hold.
        /// </summary>
        /// <remarks>
        /// Without this the spinner ran into negative numbers that the unsigned storage types cannot
        /// hold, so the edit was rejected at the GFF layer and the only sign of it was a line in the
        /// output log - the box showed -1 and the file kept its old value.
        /// </remarks>
        public decimal NumberMinimum =>
            Definition.Minimum ?? (IsUnsigned ? 0m : decimal.MinValue);

        public decimal NumberMaximum =>
            Definition.Maximum ?? Definition.FieldType switch
            {
                GffFieldType.Byte => byte.MaxValue,
                GffFieldType.Char => sbyte.MaxValue,
                GffFieldType.Word => ushort.MaxValue,
                GffFieldType.Short => short.MaxValue,
                GffFieldType.Dword => uint.MaxValue,
                GffFieldType.Int => int.MaxValue,
                _ => decimal.MaxValue
            };

        private bool IsUnsigned => Definition.FieldType
            is GffFieldType.Byte or GffFieldType.Word or GffFieldType.Dword or GffFieldType.Dword64;

        /// <summary>A read-only number (Total Cost) is a fact to display, not a control - no spinner chrome.</summary>
        public bool IsEditableNumber => IsNumber && !IsReadOnly;
        public bool IsReadOnlyNumber => IsNumber && IsReadOnly;
        public bool IsFloat => Definition.Kind == BehaviorFieldKind.Float;
        public bool IsCheck => Definition.Kind == BehaviorFieldKind.Check;
        public bool IsChoice => Definition.Kind == BehaviorFieldKind.Choice;
        public bool IsTagReference => Definition.Kind == BehaviorFieldKind.TagReference;
        public bool IsStatement => Definition.Kind == BehaviorFieldKind.Statement;

        /// <summary>Every kind that shows a single-line text box: text, scripts, names and tags.</summary>
        public bool IsTextEntry => IsText || IsTagReference || IsLocalizedText;

        /// <summary>
        /// True when the choices carry pictures, which the row shows as a searchable grid of them on
        /// the page rather than as a list of names. The load screens, the door appearances, the
        /// portraits, and the waypoint markers all arrive this way.
        /// </summary>
        public bool IsGallery => IsChoice &&
                                 (_choicePageLoader != null ||
                                  AreChoicesLoaded && Choices.Any(choice => choice.HasArtwork));

        /// <summary>
        /// A gallery whose whole set fits on the page, shown there rather than behind a button. A
        /// picture picker exists because the difference between its options is visible and not
        /// sayable, so hiding it leaves the row showing exactly the names it was meant to replace.
        /// </summary>
        public bool IsInlineGallery =>
            IsGallery && (Definition.IsInlineGallery || Choices.Count <= InlineGalleryLimit);

        /// <summary>
        /// A gallery too large to sit on the page — the portraits, which run to four figures. Its
        /// preview opens the grid: the picture is the control, so there is nothing else to aim at.
        /// </summary>
        public bool IsPopupGallery => IsGallery && !IsInlineGallery;

        /// <summary>
        /// Option count past which a gallery moves off the page. Set where a grid stops being a part
        /// of the form and starts being the form: a few rows of tiles a builder can take in at once,
        /// against a set they can only search.
        /// </summary>
        protected virtual int InlineGalleryLimit => 120;

        /// <summary>Tile sizing hooks for editors that give the shared gallery a full work pane.</summary>
        public virtual double GalleryTileWidth => 104;
        public virtual double GalleryThumbnailHeight => 78;
        public virtual double GalleryViewportHeight => 330;

        /// <summary>
        /// A choice row the builder searches rather than scrolls. Declared per field, and forced on
        /// once a set is large enough that a drop-down stops being usable. A gallery is already a
        /// browsable picker, so it never becomes a search list as well.
        /// </summary>
        public virtual bool IsSearchableChoice =>
            IsChoice && !IsGallery && !Definition.IsInlineGallery &&
            (Definition.IsSearchable || Definition.IsInlineSearch ||
             AreChoicesLoaded && Choices.Count > SearchableChoiceThreshold);

        /// <summary>
        /// A searchable list that remains visible in the form. Palette category fields declare
        /// this presentation in their definitions; an owning editor may also opt a visible work
        /// pane into it so builders can browse without an intermediate Choose action.
        /// </summary>
        public bool IsInlineSearchChoice =>
            IsSearchableChoice && (Definition.IsInlineSearch || _forceInlineSearch);

        /// <summary>A plain drop-down: every choice row that is neither searchable nor a gallery.</summary>
        public virtual bool IsPlainChoice =>
            IsChoice &&
            !IsGallery &&
            !Definition.IsInlineGallery &&
            !IsSearchableChoice;

        /// <summary>
        /// Choice count past which a row becomes searchable whether or not it asked to be. Set at the
        /// point a drop-down stops being something a builder can read: two screens of options.
        /// </summary>
        protected virtual int SearchableChoiceThreshold => 40;

        /// <summary>
        /// Whether an unset integer choice falls back to the first option. True everywhere the stored
        /// zero is a real member of the set (a palette category, an animation state); false where an
        /// absent field means "not chosen" and showing the first option would misreport it.
        /// </summary>
        protected virtual bool SelectsFirstChoiceWhenUnset => true;

        /// <summary>
        /// Whether this row carries a value at all — what the footer's "still needs" list reads.
        /// </summary>
        public virtual bool HasValue =>
            IsChoice ? Choice != null || !AreChoicesLoaded && HasStoredChoiceValue() :
            IsTextEntry || IsParagraph ? !string.IsNullOrWhiteSpace(Text) :
            true;

        /// <summary>The inverse of <see cref="HasValue"/>, for callers that read the empty case.</summary>
        public bool IsEmpty => !HasValue;

        /// <summary>Number of options matched by the current search, for the row's count line.</summary>
        public string SearchSummary
        {
            get
            {
                if (!AreChoicesLoaded)
                    return string.Empty;
                if (!IsSearchExpanded)
                    return $"{Choices.Count} option{(Choices.Count == 1 ? string.Empty : "s")}";
                if (_searchMatches.Count == 0)
                    return "No matching options";
                if (ChoiceSearchText.Trim().Length > 0 && FilteredChoices.Count >= _searchMatches.Count)
                    return $"{_searchMatches.Count} of {Choices.Count} options";

                if (FilteredChoices.Count >= _searchMatches.Count)
                {
                    var suffix = _searchMatches.Count == 1 ? string.Empty : "s";
                    return $"{_searchMatches.Count} option{suffix}";
                }

                return $"{FilteredChoices.Count} shown of {_searchMatches.Count} options";
            }
        }

        public bool CanLoadMoreSearchResults =>
            IsSearchExpanded &&
            _searchPublished < Math.Min(_searchMatches.Count, MaxSearchResults);

        /// <summary>Watermark for a searchable row's filter box, named after what it searches.</summary>
        public string SearchWatermark => $"Search {Label.ToLowerInvariant()}";

        /// <summary>
        /// What the picker says is chosen. A property rather than a <c>Choice.Display</c> binding
        /// because a row whose stored value matches nothing has no Choice at all, and binding
        /// through the null logs an error on every render.
        /// </summary>
        public virtual string SelectedChoiceDisplay => Choice?.Display ?? StoredChoiceDisplay();

        /// <summary>
        /// Optional stable identifier shown beneath the selected friendly name. Most choices do not
        /// need one; resource-backed pickers can expose it without folding it into the display text.
        /// </summary>
        public virtual string? SelectedChoiceIdentifier => Choice?.Identifier;

        public bool HasSelectedChoiceIdentifier =>
            !string.IsNullOrWhiteSpace(SelectedChoiceIdentifier);

        /// <summary>
        /// Whether this choice row can remove its stored value. Most engine fields always carry a
        /// value; linked-resource pickers such as creature equipment opt in and reuse the same
        /// progressive chooser rather than building a second search-list control.
        /// </summary>
        public virtual bool CanClearChoice => false;

        /// <summary>The clear action's builder-facing label when a linked choice can be absent.</summary>
        public virtual string ClearChoiceLabel => "Clear";

        /// <summary>How much of the gallery is on screen, for its count line.</summary>
        public string GallerySummary
        {
            get
            {
                if (_choicePageLoader != null)
                {
                    if (_choicePageLoading && GalleryChoices.Count == 0)
                        return "Loading choices...";
                    if (GalleryChoices.Count == 0)
                        return "No choices match";

                    var suffix = GalleryChoices.Count == 1 ? string.Empty : "s";
                    return _choicePagesExhausted
                        ? $"{GalleryChoices.Count} choice{suffix}"
                        : $"{GalleryChoices.Count} choice{suffix} loaded · scroll for more";
                }

                if (_galleryMatches.Count == 0)
                    return "No choices match";

                return _galleryPublished >= _galleryMatches.Count
                    ? $"{_galleryMatches.Count} choice{(_galleryMatches.Count == 1 ? string.Empty : "s")}"
                    : $"{_galleryPublished} of {_galleryMatches.Count} choices";
            }
        }

        public bool CanLoadMoreGallery => _choicePageLoader != null
            ? _choicePagesActivated && !_choicePageLoading && !_choicePagesExhausted
            : _galleryPublished < _galleryMatches.Count;

        [ObservableProperty]
        private string _text = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NumberDisplay))]
        private decimal _number;

        /// <summary>
        /// How a numeric row renders and steps. An integral GFF field is a whole number - showing
        /// (and stepping) it as "2.0" invites a fractional edit the store cannot hold; only a real
        /// Float field gets a decimal point.
        /// </summary>
        public string NumberFormat => IsFloat ? "0.###" : "0";

        public decimal NumberIncrement => IsFloat ? 0.1m : 1m;

        /// <summary>The read-only rendering of a number (Total Cost), formatted like its editable twin.</summary>
        public string NumberDisplay => Number.ToString(NumberFormat, System.Globalization.CultureInfo.CurrentCulture);

        [ObservableProperty]
        private bool _isChecked;

        [ObservableProperty]
        private BehaviorChoiceViewModel? _choice;

        [ObservableProperty]
        private string _choiceSearchText = string.Empty;

        /// <summary>Search results stay collapsed until the builder opens this particular field.</summary>
        [ObservableProperty]
        private bool _isSearchExpanded;

        /// <summary>
        /// What a read-only Statement row prints: the stored value, so a builder can see what the
        /// behavior wrote without being offered a box that would fight it.
        /// </summary>
        [ObservableProperty]
        private string _statementText = string.Empty;

        /// <summary>Live feedback beside the value: where a tag resolved, or why it did not.</summary>
        [ObservableProperty]
        private string? _status;

        [ObservableProperty]
        private bool _isStatusGood = true;

        [ObservableProperty]
        private bool _isVisible = true;

        [ObservableProperty]
        private string _galleryQuery = string.Empty;

        private GallerySortOption? _selectedGallerySort;

        public GallerySortOption? SelectedGallerySort
        {
            get => _selectedGallerySort;
            set
            {
                if (!SetProperty(ref _selectedGallerySort, value) || !_galleryBuilt)
                    return;

                RebuildGallery();
            }
        }

        /// <summary>The chosen option's picture, shown large enough to actually judge.</summary>
        [ObservableProperty]
        private Bitmap? _selectedPreview;

        /// <summary>
        /// Whether a popup gallery is showing. Bound rather than left to the flyout so that picking
        /// an option can close it — a picker that stays open after you have chosen makes you dismiss
        /// it yourself to see what you did.
        /// </summary>
        [ObservableProperty]
        private bool _isGalleryOpen;

        public BehaviorRowViewModel(
            BehaviorFieldDefinition definition,
            BehaviorValueStore store,
            Func<string, Action, bool> runEdit,
            IReadOnlyList<BehaviorChoice>? choices = null,
            Action? valueChanged = null,
            ChoicePreviewService? previews = null,
            Func<BehaviorChoice, string?>? previewAudio = null,
            Func<IReadOnlyList<BehaviorChoice>>? choiceLoader = null,
            Func<Task<IReadOnlyList<BehaviorChoice>>>? asyncChoiceLoader = null,
            Func<string, int, int, Task<IReadOnlyList<BehaviorChoice>>>? choicePageLoader = null,
            bool forceInlineSearch = false)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Store = store ?? throw new ArgumentNullException(nameof(store));
            RunEditFunc = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            _valueChanged = valueChanged;
            _previews = previews;
            _previewAudio = previewAudio;
            var choiceSourceCount = (choices != null ? 1 : 0) +
                                    (choiceLoader != null ? 1 : 0) +
                                    (asyncChoiceLoader != null ? 1 : 0) +
                                    (choicePageLoader != null ? 1 : 0);
            if (choiceSourceCount > 1)
                throw new ArgumentException(
                    "Provide eager choices, one deferred choice loader, or a paged choice loader, not more than one.");

            _choiceLoader = choiceLoader;
            _asyncChoiceLoader = asyncChoiceLoader;
            _choicePageLoader = choicePageLoader;
            _forceInlineSearch = forceInlineSearch;
            // Wrapping the choices costs nothing: no picture is decoded or rendered until a tile
            // that shows one exists, and then only for the page that has been published. Building
            // the rows used to decode every load screen - around thirty megabytes of DDS - before
            // the tab could draw, which is what made switching to Area Transition stall.
            if (choiceLoader == null && asyncChoiceLoader == null && choicePageLoader == null)
                Choices = BehaviorChoiceViewModel.From(choices ?? definition.Choices);
        }

        /// <summary>
        /// Reads this row's value out of the document. Called by the constructor of the concrete row
        /// — not by this one, so a subclass finishes initializing its own state first.
        /// </summary>
        public void Reload()
        {
            if (Definition.IsInlineSearch)
                LoadSynchronousDeferredChoices();

            if (IsInlineSearchChoice)
                IsSearchExpanded = true;

            _loading = true;
            try
            {
                ReadValue();
            }
            finally
            {
                _loading = false;
            }

            if (IsSearchableChoice && IsSearchExpanded)
            {
                ChoiceSearchText = string.Empty;
                RebuildFilteredChoices();
            }
            else
            {
                FilteredChoices.Clear();
                _searchMatches.Clear();
                _searchPublished = 0;
            }

            // An inline grid is part of the row rather than something opened, so it is built with the
            // row. Only the published page costs anything: the tiles beyond it are not realized and
            // their pictures are not requested until the builder scrolls to them. A popup gallery
            // still waits to be opened - four figures of portraits is not a page's worth of anything.
            if (IsInlineGallery && !_suppressAutomaticGalleryRebuild)
                RebuildGallery();

            RefreshStatus();
            NotifyValueShapeChanged();
        }

        /// <summary>Re-reads the stored value into the row's observable properties.</summary>
        protected virtual void ReadValue()
        {
            switch (Definition.Kind)
            {
                case BehaviorFieldKind.Statement:
                    StatementText = Store.GetInteger(Definition.Storage, Definition.Name)
                        ?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
                    break;
                case BehaviorFieldKind.Check:
                    IsChecked = Store.GetInteger(Definition.Storage, Definition.Name) == 1;
                    break;
                case BehaviorFieldKind.Integer:
                    Number = Store.GetInteger(Definition.Storage, Definition.Name) ?? 0;
                    break;
                case BehaviorFieldKind.Float:
                    Number = (decimal)(Store.GetFloat(Definition.Storage, Definition.Name) ?? 0);
                    break;
                case BehaviorFieldKind.Choice when Definition.FieldType is
                    GffFieldType.CExoString or GffFieldType.ResRef:
                    var storedText = Store.GetString(Definition.Storage, Definition.Name);
                    Choice = Choices.FirstOrDefault(option =>
                        string.Equals(option.StringValue, storedText, StringComparison.Ordinal));
                    break;
                case BehaviorFieldKind.Choice:
                    var stored = Store.GetInteger(Definition.Storage, Definition.Name);
                    Choice = Choices.FirstOrDefault(option => option.Value == (stored ?? 0))
                             ?? (SelectsFirstChoiceWhenUnset ? Choices.FirstOrDefault() : null);
                    break;
                case BehaviorFieldKind.LocalizedText:
                    Text = Store.GetLocalizedText(Definition.Name);
                    break;
                case BehaviorFieldKind.Paragraph when Definition.FieldType == GffFieldType.CExoLocString:
                    Text = Store.GetLocalizedText(Definition.Name);
                    break;
                default:
                    Text = Store.GetString(Definition.Storage, Definition.Name);
                    break;
            }
        }

        /// <summary>Writes a text edit. Runs inside the row's transaction.</summary>
        protected virtual void WriteText(string value)
        {
            if (Definition.Kind == BehaviorFieldKind.LocalizedText ||
                Definition.Kind == BehaviorFieldKind.Paragraph &&
                Definition.FieldType == GffFieldType.CExoLocString)
            {
                Store.SetLocalizedText(Definition.Name, value);
                return;
            }

            Store.SetString(Definition.Storage, Definition.Name, Definition.FieldType, value);
        }

        /// <summary>Writes a numeric edit. Runs inside the row's transaction.</summary>
        protected virtual void WriteNumber(decimal value)
        {
            if (IsFloat)
                Store.SetFloat(Definition.Storage, Definition.Name, (double)value);
            else
                Store.SetInteger(Definition.Storage, Definition.Name, Definition.FieldType, (long)value);
        }

        /// <summary>Writes a checkbox edit. Runs inside the row's transaction.</summary>
        protected virtual void WriteCheck(bool value) =>
            Store.SetInteger(Definition.Storage, Definition.Name, Definition.FieldType, value ? 1 : 0);

        /// <summary>Writes a choice edit. Runs inside the row's transaction.</summary>
        protected virtual void WriteChoice(BehaviorChoiceViewModel value)
        {
            if (value.Choice.IsStringValue)
                Store.SetString(
                    Definition.Storage, Definition.Name, Definition.FieldType, value.StringValue!);
            else
                Store.SetInteger(
                    Definition.Storage, Definition.Name, Definition.FieldType, value.Value);
        }

        /// <summary>Recomputes the row's status line. Subclasses that have one override this.</summary>
        public virtual void RefreshStatus()
        {
        }

        /// <summary>Runs after a successful edit lands, on the UI thread.</summary>
        protected virtual void OnApplied() => _valueChanged?.Invoke();

        [RelayCommand]
        private void PickChoice(BehaviorChoiceViewModel? option)
        {
            if (option != null)
                Choice = option;

            IsGalleryOpen = false;
            if (!IsInlineSearchChoice)
                IsSearchExpanded = false;
        }

        [RelayCommand]
        private void ClearSearch()
        {
            if (ChoiceSearchText.Length == 0)
                RebuildFilteredChoices();
            else
                ChoiceSearchText = string.Empty;
        }

        /// <summary>
        /// Resolves this one option set and publishes its first page. Merely displaying the editor
        /// or changing behaviors never calls a deferred loader.
        /// </summary>
        [RelayCommand]
        private async Task OpenSearch()
        {
            await EnsureChoicesLoadedAsync().ConfigureAwait(true);

            // Loading can reveal that this is an artwork picker. Open that reusable surface rather
            // than also constructing a text-result list for the same choices.
            if (IsPopupGallery)
            {
                await OpenGallery().ConfigureAwait(true);
                return;
            }

            if (IsInlineGallery)
            {
                // The choices may have kicked off a presentation refresh while the
                // catalog was loading. Await our own refresh so callers do not
                // continue before the first visible page has been published.
                await RebuildGalleryAsync().ConfigureAwait(true);
                return;
            }

            if (!IsSearchableChoice)
                return;

            IsSearchExpanded = true;
            if (ChoiceSearchText.Length == 0)
                RebuildFilteredChoices();
            else
                ChoiceSearchText = string.Empty;
        }

        [RelayCommand]
        private void CloseSearch() => IsSearchExpanded = false;

        [RelayCommand]
        private void LoadMoreSearchResults() => PublishSearchPage();

        [RelayCommand]
        protected virtual void ClearChoice()
        {
        }

        [RelayCommand]
        private void PreviewAudio(BehaviorChoiceViewModel? option)
        {
            if (option?.CanPreviewAudio != true || _previewAudio == null)
                return;
            Status = _previewAudio(option.Choice);
            IsStatusGood = string.IsNullOrWhiteSpace(Status);
        }

        /// <summary>
        /// Opens a popup gallery, building it on the first open. Until then the row has paid for
        /// exactly one picture — the one it is showing.
        /// </summary>
        [RelayCommand]
        private async Task OpenGallery()
        {
            await EnsureChoicesLoadedAsync().ConfigureAwait(true);
            if (!IsPopupGallery)
                return;

            IsGalleryOpen = true;
            if (_galleryBuilt)
                return;

            _galleryBuilt = true;
            await RebuildGalleryAsync().ConfigureAwait(true);
        }

        [RelayCommand]
        private async Task LoadMoreGallery() =>
            await PublishGalleryPageAsync().ConfigureAwait(true);

        partial void OnGalleryQueryChanged(string value)
        {
            if (!_galleryBuilt)
                return;

            _searchDebounce?.Cancel();
            _searchDebounce?.Dispose();
            _searchDebounce = null;

            // Clearing the box is a search being abandoned, not one being typed. Waiting out the
            // debounce for it leaves the old results sitting there looking like the filter stuck.
            if (string.IsNullOrWhiteSpace(value))
            {
                RebuildGallery();
                return;
            }

            var pending = new CancellationTokenSource();
            _searchDebounce = pending;
            Task.Delay(SearchDebounce, pending.Token).ContinueWith(
                task =>
                {
                    if (!task.IsCanceled)
                        Dispatcher.UIThread.Post(RebuildGallery);
                },
                TaskScheduler.Default);
        }

        private void RebuildGallery() => _ = RebuildGalleryAsync();

        private async Task RebuildGalleryAsync()
        {
            if (_disposed)
                return;

            var rebuildGeneration = ++_galleryRebuildGeneration;
            _galleryBuilt = true;

            // A repository-backed gallery owns only the pages it has published. Search starts a
            // fresh page at the source; scrolling asks for the next page. This is the same bounded
            // item flow used by the merchant editor, generalized here so other blueprint pickers
            // do not need to duplicate it.
            if (_choicePageLoader != null)
            {
                if (!_reusePagedChoicesOnNextRebuild &&
                    !await LoadChoicePageAsync(reset: true).ConfigureAwait(true))
                {
                    return;
                }
                _reusePagedChoicesOnNextRebuild = false;

                if (_disposed)
                    return;

                EnsureGalleryControls();
                _galleryPublished = 0;
                GalleryChoices.Clear();
                PublishLoadedGalleryChoices();
                return;
            }

            var words = (GalleryQuery ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var choices = Choices;
            var selectedFilters = GalleryFilters
                .Where(filter => filter.SelectedOption.ValueKey != null)
                .Select(filter => new GalleryFilterSelection(
                    filter.GroupKey,
                    filter.SelectedOption.ValueKey!))
                .ToArray();
            var sortMode = SelectedGallerySort?.Mode ?? GallerySortMode.Default;
            var buildControls = !_galleryControlsBuilt;

            // Catalog galleries can contain several thousand choices. Facet discovery, text
            // matching, and sorting are pure work, so keep them off Avalonia's UI thread and only
            // publish the first bounded page after the latest request completes. The generation
            // check prevents a slower, stale search from replacing newer input.
            var result = await Task.Run(() => new GalleryBuildResult(
                    buildControls ? BuildGalleryControls(choices) : null,
                    BuildGalleryMatches(choices, words, selectedFilters, sortMode)))
                .ConfigureAwait(true);

            if (_disposed || rebuildGeneration != _galleryRebuildGeneration)
                return;

            if (result.Controls != null)
                ApplyGalleryControls(result.Controls);

            _galleryMatches = result.Matches;
            _galleryPublished = 0;
            GalleryChoices.Clear();
            PublishLoadedGalleryChoices();
        }

        private static List<BehaviorChoiceViewModel> BuildGalleryMatches(
            IReadOnlyList<BehaviorChoiceViewModel> choices,
            IReadOnlyList<string> words,
            IReadOnlyList<GalleryFilterSelection> selectedFilters,
            GallerySortMode sortMode)
        {
            IEnumerable<BehaviorChoiceViewModel> matches = choices
                .Where(candidate => words.All(word =>
                    candidate.Display.Contains(word, StringComparison.OrdinalIgnoreCase) ||
                    (candidate.Detail?.Contains(word, StringComparison.OrdinalIgnoreCase) ?? false)));

            foreach (var filter in selectedFilters)
            {
                matches = matches.Where(candidate => candidate.Choice.GalleryFacets.Any(facet =>
                    string.Equals(facet.GroupKey, filter.GroupKey, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(facet.ValueKey, filter.ValueKey, StringComparison.OrdinalIgnoreCase)));
            }

            matches = sortMode switch
            {
                GallerySortMode.NameAscending => matches
                    .OrderBy(candidate => candidate.Display, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(candidate => candidate.Value),
                GallerySortMode.NameDescending => matches
                    .OrderByDescending(candidate => candidate.Display, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(candidate => candidate.Value),
                GallerySortMode.IdAscending => matches.OrderBy(candidate => candidate.Value),
                GallerySortMode.IdDescending => matches.OrderByDescending(candidate => candidate.Value),
                _ => matches
            };

            return matches.ToList();
        }

        private void EnsureGalleryControls()
        {
            if (_galleryControlsBuilt)
                return;

            ApplyGalleryControls(BuildGalleryControls(Choices));
        }

        private static GalleryControlSet BuildGalleryControls(
            IReadOnlyList<BehaviorChoiceViewModel> choices)
        {
            var filters = new List<GalleryFilterDefinition>();
            var facets = choices.SelectMany(choice => choice.Choice.GalleryFacets).ToList();
            foreach (var group in facets.GroupBy(
                         facet => facet.GroupKey,
                         StringComparer.OrdinalIgnoreCase))
            {
                var values = group
                    .GroupBy(facet => facet.ValueKey, StringComparer.OrdinalIgnoreCase)
                    .Select(valueGroup => valueGroup
                        .OrderBy(facet => facet.Order)
                        .ThenBy(facet => facet.Display, StringComparer.OrdinalIgnoreCase)
                        .First())
                    .OrderBy(facet => facet.Order)
                    .ThenBy(facet => facet.Display, StringComparer.OrdinalIgnoreCase)
                    .Select(facet => new GalleryFilterOption(facet.ValueKey, facet.Display))
                    .ToList();

                if (values.Count <= 1)
                    continue;

                values.Insert(0, new GalleryFilterOption(null, "All"));
                filters.Add(new GalleryFilterDefinition(
                    group.Key,
                    group.First().GroupLabel,
                    values));
            }

            return new GalleryControlSet(
                filters,
                choices.All(choice => !choice.Choice.IsStringValue));
        }

        private void ApplyGalleryControls(GalleryControlSet controls)
        {
            _galleryControlsBuilt = true;
            GalleryFilters.Clear();
            GallerySortOptions.Clear();

            foreach (var filter in controls.Filters)
            {
                GalleryFilters.Add(new GalleryFilterViewModel(
                    filter.GroupKey,
                    filter.GroupLabel,
                    filter.Options,
                    RebuildGallery));
            }

            GallerySortOptions.Add(new GallerySortOption(GallerySortMode.Default, "Default order"));
            GallerySortOptions.Add(new GallerySortOption(GallerySortMode.NameAscending, "Name A-Z"));
            GallerySortOptions.Add(new GallerySortOption(GallerySortMode.NameDescending, "Name Z-A"));
            if (controls.SupportsIdSort)
            {
                GallerySortOptions.Add(new GallerySortOption(GallerySortMode.IdAscending, "ID low-high"));
                GallerySortOptions.Add(new GallerySortOption(GallerySortMode.IdDescending, "ID high-low"));
            }

            _selectedGallerySort = GallerySortOptions[0];
            OnPropertyChanged(nameof(SelectedGallerySort));
            OnPropertyChanged(nameof(HasGalleryFilters));
        }

        private sealed record GalleryFilterSelection(string GroupKey, string ValueKey);

        private sealed record GalleryFilterDefinition(
            string GroupKey,
            string GroupLabel,
            IReadOnlyList<GalleryFilterOption> Options);

        private sealed record GalleryControlSet(
            IReadOnlyList<GalleryFilterDefinition> Filters,
            bool SupportsIdSort);

        private sealed record GalleryBuildResult(
            GalleryControlSet? Controls,
            List<BehaviorChoiceViewModel> Matches);

        private async Task PublishGalleryPageAsync()
        {
            if (_disposed || !_galleryBuilt)
                return;

            if (_choicePageLoader != null)
            {
                if (!await LoadChoicePageAsync(reset: false).ConfigureAwait(true))
                    return;
                PublishLoadedGalleryChoices();
                return;
            }

            PublishLoadedGalleryChoices();
        }

        private void PublishLoadedGalleryChoices()
        {
            if (_disposed || !_galleryBuilt)
                return;

            var end = Math.Min(_galleryPublished + GalleryPageSize, _galleryMatches.Count);
            for (var index = _galleryPublished; index < end; index++)
            {
                var choice = _galleryMatches[index];
                choice.IsSelected = ReferenceEquals(choice, Choice);
                GalleryChoices.Add(choice);
                RequestThumbnail(choice);
            }

            _galleryPublished = end;
            OnPropertyChanged(nameof(GallerySummary));
            OnPropertyChanged(nameof(CanLoadMoreGallery));
        }

        private async Task<bool> LoadChoicePageAsync(bool reset)
        {
            if (_choicePageLoader == null || !_choicePagesActivated)
                return false;

            var offset = reset ? 0 : _choicePageOffset;
            if (!reset && (_choicePagesExhausted || _choicePageLoading))
                return false;

            var query = (GalleryQuery ?? string.Empty).Trim();
            var requestGeneration = reset
                ? ++_choicePageRequestGeneration
                : _choicePageRequestGeneration;
            _choicePageLoading = true;
            NotifyChoicePageStateChanged();
            try
            {
                var sourcePage = await _choicePageLoader(
                    query,
                    offset,
                    GalleryPageSize + 1).ConfigureAwait(true);
                if (_disposed || requestGeneration != _choicePageRequestGeneration ||
                    !string.Equals(query, (GalleryQuery ?? string.Empty).Trim(), StringComparison.Ordinal))
                {
                    return false;
                }

                var visiblePage = sourcePage.Take(GalleryPageSize).ToList();
                var wrapped = BehaviorChoiceViewModel.From(visiblePage);

                if (reset)
                {
                    Choices = wrapped;
                    _galleryMatches = wrapped.ToList();
                    _galleryPublished = 0;
                    _choicePageOffset = wrapped.Count;
                }
                else if (wrapped.Count > 0)
                {
                    _choices = Choices.Concat(wrapped).ToList();
                    _galleryMatches.AddRange(wrapped);
                    _choicePageOffset += wrapped.Count;
                }

                _choicePagesExhausted = sourcePage.Count <= GalleryPageSize || wrapped.Count == 0;
                return true;
            }
            finally
            {
                if (requestGeneration == _choicePageRequestGeneration)
                {
                    _choicePageLoading = false;
                    NotifyChoicePageStateChanged();
                }
            }
        }

        private void NotifyChoicePageStateChanged()
        {
            OnPropertyChanged(nameof(IsGalleryLoading));
            OnPropertyChanged(nameof(AreChoicesLoaded));
            OnPropertyChanged(nameof(IsGallery));
            OnPropertyChanged(nameof(IsInlineGallery));
            OnPropertyChanged(nameof(IsPopupGallery));
            OnPropertyChanged(nameof(IsSearchableChoice));
            OnPropertyChanged(nameof(IsInlineSearchChoice));
            OnPropertyChanged(nameof(IsPlainChoice));
            OnPropertyChanged(nameof(GallerySummary));
            OnPropertyChanged(nameof(CanLoadMoreGallery));
        }

        private void RequestThumbnail(BehaviorChoiceViewModel choice)
        {
            if (_previews == null || !choice.HasArtwork || choice.Thumbnail != null)
                return;

            if (_previews.Cached(choice.Choice, ChoicePreviewService.ThumbnailWidth) is { } cached)
            {
                choice.Thumbnail = cached;
                return;
            }

            _ = _previews.RequestAsync(
                choice.Choice,
                ChoicePreviewService.ThumbnailWidth,
                bitmap => choice.Thumbnail = bitmap);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _galleryRebuildGeneration++;
            _searchDebounce?.Cancel();
            _searchDebounce?.Dispose();
            _searchDebounce = null;
        }

        partial void OnTextChanged(string value)
        {
            if (_loading || IsReadOnly)
                return;

            Apply(() => WriteText(value));
            NotifyValueShapeChanged();
        }

        partial void OnNumberChanged(decimal value)
        {
            if (_loading || IsReadOnly)
                return;

            // An integral GFF field must never silently truncate: the NumericUpDown accepts
            // "12.9" even on an Integer row, and the (long) cast in WriteNumber would store 12
            // while the box keeps showing 12.9. The edit is rejected instead - the box snaps
            // back to what the document actually stores.
            if (Definition.Kind == BehaviorFieldKind.Integer && decimal.Truncate(value) != value)
            {
                _loading = true;
                try
                {
                    Number = Store.GetInteger(Definition.Storage, Definition.Name) ?? 0;
                }
                finally
                {
                    _loading = false;
                }

                return;
            }

            Apply(() => WriteNumber(value));
        }

        partial void OnIsCheckedChanged(bool value)
        {
            if (_loading || IsReadOnly)
                return;

            Apply(() => WriteCheck(value), $"Toggle {Label}");
        }

        partial void OnChoiceChanged(BehaviorChoiceViewModel? value)
        {
            OnChoiceSelected(value);

            if (_loading || IsReadOnly || value == null)
                return;

            Apply(() => WriteChoice(value));
            NotifyValueShapeChanged();
        }

        partial void OnChoiceSearchTextChanged(string value) => RebuildFilteredChoices();

        partial void OnIsSearchExpandedChanged(bool value)
        {
            OnPropertyChanged(nameof(SearchSummary));
            OnPropertyChanged(nameof(CanLoadMoreSearchResults));
            if (value)
                return;

            FilteredChoices.Clear();
            _searchMatches.Clear();
            _searchPublished = 0;
        }

        /// <summary>Called whenever the selected choice changes, including during a reload.</summary>
        /// <remarks>
        /// A popup gallery resolves one picture, not the whole set: the chosen option is the only
        /// one on screen until the grid is opened.
        /// </remarks>
        protected virtual void OnChoiceSelected(BehaviorChoiceViewModel? value)
        {
            OnPropertyChanged(nameof(SelectedChoiceDisplay));
            OnPropertyChanged(nameof(SelectedChoiceIdentifier));
            OnPropertyChanged(nameof(HasSelectedChoiceIdentifier));
            OnPropertyChanged(nameof(CanClearChoice));

            // Mark only the two entries whose state changed. Walking thousands of choices on every
            // reload or pick made the row cost proportional to its entire catalog even though only
            // a virtualized page can be visible.
            if (_markedChoice != null && !ReferenceEquals(_markedChoice, value))
                _markedChoice.IsSelected = false;
            if (value != null)
                value.IsSelected = true;
            _markedChoice = value;

            if (IsPopupGallery)
                _ = LoadSelectedPreviewAsync(value);
        }

        /// <summary>
        /// Which selection the large preview is being loaded for. Incremented on every request so a
        /// slower earlier resolve can tell that it has been overtaken.
        /// </summary>
        private int _previewGeneration;

        /// <summary>
        /// Loads the large picture for the current selection.
        /// </summary>
        /// <remarks>
        /// Two uncached choices picked in quick succession resolve independently, and whichever
        /// finished last used to win - so a slow first load could land after a fast second one and
        /// leave the panel showing artwork A while the field stored B. That is worse than a blank
        /// panel: the builder is choosing by the picture. A result is now published only if its
        /// request is still the current one.
        /// </remarks>
        private async Task LoadSelectedPreviewAsync(BehaviorChoiceViewModel? choice)
        {
            var generation = ++_previewGeneration;

            if (_previews == null || choice == null || !choice.HasArtwork)
            {
                SelectedPreview = null;
                return;
            }

            if (_previews.Cached(choice.Choice, ChoicePreviewService.PreviewWidth) is { } cached)
            {
                SelectedPreview = cached;
                return;
            }

            // Cleared while the picture is resolved, so the panel does not keep showing the previous
            // choice's artwork under the new choice's name.
            SelectedPreview = null;

            await _previews
                .RequestAsync(
                    choice.Choice,
                    ChoicePreviewService.PreviewWidth,
                    bitmap =>
                    {
                        if (generation == _previewGeneration)
                            SelectedPreview = bitmap;
                    })
                .ConfigureAwait(true);
        }

        private void Apply(Action mutation, string? description = null)
        {
            if (!RunEditFunc(description ?? $"Change {Label}", mutation))
                Reload();
            else
                OnApplied();
        }

        private void RebuildFilteredChoices()
        {
            if (!IsSearchableChoice || !IsSearchExpanded || !AreChoicesLoaded)
                return;

            var query = ChoiceSearchText.Trim();
            _searchMatches = Choices.Where(option =>
                    query.Length == 0 ||
                    option.Display.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (option.StringValue?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
            _searchPublished = 0;
            FilteredChoices.Clear();
            PublishSearchPage();
        }

        private void PublishSearchPage()
        {
            if (!IsSearchableChoice || !IsSearchExpanded || !AreChoicesLoaded)
                return;

            var end = Math.Min(
                _searchPublished + SearchPageSize,
                Math.Min(_searchMatches.Count, MaxSearchResults));
            for (var index = _searchPublished; index < end; index++)
            {
                var option = _searchMatches[index];
                if (!FilteredChoices.Contains(option))
                    FilteredChoices.Add(option);
            }
            _searchPublished = end;

            // Paging excludes a non-match by accident where a filter excludes it on purpose. Keep
            // the stored value visible while browsing the unfiltered catalog.
            if (ChoiceSearchText.Length == 0 && Choice != null && !FilteredChoices.Contains(Choice))
                FilteredChoices.Insert(0, Choice);

            OnPropertyChanged(nameof(SearchSummary));
            OnPropertyChanged(nameof(CanLoadMoreSearchResults));
        }

        /// <summary>
        /// Resolves a deferred choice source and builds its initial presentation. Specialized
        /// editors use this when their selected work pane is itself the picker, so the user never
        /// has to press an extra button merely to reveal the options.
        /// </summary>
        protected async Task EnsureChoicesLoadedAsync()
        {
            if (_choicePageLoader != null)
            {
                if (_choicePagesActivated)
                    return;

                _choicePagesActivated = true;
                if (!await LoadChoicePageAsync(reset: true).ConfigureAwait(true))
                    return;
                _reusePagedChoicesOnNextRebuild = true;
                ReloadWithoutStartingGalleryRebuild();
                if (IsInlineGallery)
                    await RebuildGalleryAsync().ConfigureAwait(true);
                return;
            }

            if (await LoadDeferredChoicesAsync().ConfigureAwait(true))
                ReloadWithoutStartingGalleryRebuild();

            // Reload starts inline presentation without blocking the editor's first paint. An
            // explicit activation is different: its caller is waiting for the visible picker, so
            // do not complete until the first bounded page and its facet controls are published.
            if (IsInlineGallery)
                await RebuildGalleryAsync().ConfigureAwait(true);
        }

        private void ReloadWithoutStartingGalleryRebuild()
        {
            _suppressAutomaticGalleryRebuild = true;
            try
            {
                Reload();
            }
            finally
            {
                _suppressAutomaticGalleryRebuild = false;
            }
        }

        /// <summary>
        /// Resolves a deferred picker because its owning work pane is now visible. Editors use the
        /// same progressive search and gallery machinery as an explicit open action without adding
        /// a second button in front of the choices.
        /// </summary>
        public Task ActivateChoicesAsync() => EnsureChoicesLoadedAsync();

        private async Task<bool> LoadDeferredChoicesAsync()
        {
            var loader = _choiceLoader;
            var asyncLoader = _asyncChoiceLoader;
            if (loader == null && asyncLoader == null)
                return false;

            if (loader != null)
                return LoadSynchronousDeferredChoices();

            // Resolve before clearing the loader so a malformed source can be retried instead of
            // leaving the row claiming that an empty set loaded successfully. The resolver itself
            // is cached by the editor service; this row pays only for its wrappers after its owning
            // pane becomes visible or the builder explicitly opens it.
            var choices = await asyncLoader!().ConfigureAwait(true);
            var loaded = await Task.Run(() => BehaviorChoiceViewModel.From(choices))
                .ConfigureAwait(true);
            _asyncChoiceLoader = null;
            Choices = loaded;
            OnPropertyChanged(nameof(AreChoicesLoaded));
            return true;
        }

        private bool LoadSynchronousDeferredChoices()
        {
            var loader = _choiceLoader;
            if (loader == null)
                return false;

            var loaded = BehaviorChoiceViewModel.From(loader());
            _choiceLoader = null;
            Choices = loaded;
            OnPropertyChanged(nameof(AreChoicesLoaded));
            return true;
        }

        private string StoredChoiceDisplay()
        {
            if (!IsChoice)
                return "Nothing chosen";

            if (Definition.FieldType is GffFieldType.CExoString or GffFieldType.ResRef)
            {
                var text = Store.GetString(Definition.Storage, Definition.Name);
                return string.IsNullOrWhiteSpace(text) ? "Nothing chosen" : text;
            }

            var value = Store.GetInteger(Definition.Storage, Definition.Name);
            return value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "Nothing chosen";
        }

        private bool HasStoredChoiceValue()
        {
            if (Definition.FieldType is GffFieldType.CExoString or GffFieldType.ResRef)
                return !string.IsNullOrWhiteSpace(Store.GetString(Definition.Storage, Definition.Name));

            return Store.GetInteger(Definition.Storage, Definition.Name).HasValue;
        }

        private void NotifyChoicePresentationChanged()
        {
            OnPropertyChanged(nameof(AreChoicesLoaded));
            OnPropertyChanged(nameof(IsGallery));
            OnPropertyChanged(nameof(IsInlineGallery));
            OnPropertyChanged(nameof(IsPopupGallery));
            OnPropertyChanged(nameof(IsSearchableChoice));
            OnPropertyChanged(nameof(IsInlineSearchChoice));
            OnPropertyChanged(nameof(IsPlainChoice));
            OnPropertyChanged(nameof(SearchSummary));
            OnPropertyChanged(nameof(CanLoadMoreSearchResults));
            OnPropertyChanged(nameof(GallerySummary));
            OnPropertyChanged(nameof(CanLoadMoreGallery));
        }

        /// <summary>Republishes the properties that depend on the stored value rather than on it alone.</summary>
        protected void NotifyValueShapeChanged()
        {
            OnPropertyChanged(nameof(SelectedChoiceDisplay));
            OnPropertyChanged(nameof(SelectedChoiceIdentifier));
            OnPropertyChanged(nameof(HasSelectedChoiceIdentifier));
            OnPropertyChanged(nameof(Counter));
            OnPropertyChanged(nameof(HasValue));
            OnPropertyChanged(nameof(IsEmpty));
        }
    }
}
