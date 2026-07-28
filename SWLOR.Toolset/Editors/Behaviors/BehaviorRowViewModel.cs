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
        private const int GalleryPageSize = 48;

        /// <summary>How long typing pauses before the gallery re-filters.</summary>
        private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(250);

        /// <summary>How many filtered options a searchable row publishes before it stops.</summary>
        /// <remarks>
        /// The tag source alone offers five figures of options, and every published row is a control
        /// realized. A builder narrows the search rather than scrolling past the two hundredth
        /// result, so the cap costs nothing and keeps a keystroke from realizing thousands of rows.
        /// </remarks>
        public const int MaxSearchResults = 200;

        private readonly Action? _valueChanged;
        private readonly ChoicePreviewService? _previews;
        private List<BehaviorChoiceViewModel> _galleryMatches = new();
        private CancellationTokenSource? _searchDebounce;
        private int _galleryPublished;
        private bool _galleryBuilt;
        private bool _disposed;
        private bool _loading;

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

        /// <summary>Resolved at construction, so a game-data choice set and a fixed one read alike.</summary>
        public IReadOnlyList<BehaviorChoiceViewModel> Choices { get; }

        /// <summary>The filtered slice of <see cref="Choices"/> a searchable row shows.</summary>
        public ObservableCollection<BehaviorChoiceViewModel> FilteredChoices { get; } = new();

        /// <summary>The published page of gallery tiles, for a choice row whose options have artwork.</summary>
        public ObservableCollection<BehaviorChoiceViewModel> GalleryChoices { get; } = new();

        public bool IsText => Definition.Kind is BehaviorFieldKind.Text or BehaviorFieldKind.Script;
        public bool IsLocalizedText => Definition.Kind == BehaviorFieldKind.LocalizedText;
        public bool IsParagraph => Definition.Kind == BehaviorFieldKind.Paragraph;
        public bool IsNumber => Definition.Kind is BehaviorFieldKind.Integer or BehaviorFieldKind.Float;
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
        public bool IsGallery => IsChoice && Choices.Any(choice => choice.HasArtwork);

        /// <summary>
        /// A gallery whose whole set fits on the page, shown there rather than behind a button. A
        /// picture picker exists because the difference between its options is visible and not
        /// sayable, so hiding it leaves the row showing exactly the names it was meant to replace.
        /// </summary>
        public bool IsInlineGallery => IsGallery && Choices.Count <= InlineGalleryLimit;

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

        /// <summary>
        /// A choice row the builder searches rather than scrolls. Declared per field, and forced on
        /// once a set is large enough that a drop-down stops being usable. A gallery is already a
        /// browsable picker, so it never becomes a search list as well.
        /// </summary>
        public virtual bool IsSearchableChoice =>
            IsChoice && !IsGallery &&
            (Definition.IsSearchable || Choices.Count > SearchableChoiceThreshold);

        /// <summary>A plain drop-down: every choice row that is neither searchable nor a gallery.</summary>
        public virtual bool IsPlainChoice => IsChoice && !IsGallery && !IsSearchableChoice;

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
            IsChoice ? Choice != null :
            IsTextEntry || IsParagraph ? !string.IsNullOrWhiteSpace(Text) :
            true;

        /// <summary>The inverse of <see cref="HasValue"/>, for callers that read the empty case.</summary>
        public bool IsEmpty => !HasValue;

        /// <summary>Number of options matched by the current search, for the row's count line.</summary>
        public string SearchSummary =>
            FilteredChoices.Count == Choices.Count
                ? $"{Choices.Count} option{(Choices.Count == 1 ? string.Empty : "s")}"
                : FilteredChoices.Count == 0
                    ? "No matching options"
                    : $"{FilteredChoices.Count} of {Choices.Count} options";

        /// <summary>Watermark for a searchable row's filter box, named after what it searches.</summary>
        public string SearchWatermark => $"Search {Label.ToLowerInvariant()}";

        /// <summary>
        /// What the picker says is chosen. A property rather than a <c>Choice.Display</c> binding
        /// because a row whose stored value matches nothing has no Choice at all, and binding
        /// through the null logs an error on every render.
        /// </summary>
        public string SelectedChoiceDisplay => Choice?.Display ?? "Nothing chosen";

        /// <summary>How much of the gallery is on screen, for its count line.</summary>
        public string GallerySummary
        {
            get
            {
                if (_galleryMatches.Count == 0)
                    return "No choices match";

                return _galleryPublished >= _galleryMatches.Count
                    ? $"{_galleryMatches.Count} choice{(_galleryMatches.Count == 1 ? string.Empty : "s")}"
                    : $"{_galleryPublished} of {_galleryMatches.Count} choices";
            }
        }

        public bool CanLoadMoreGallery => _galleryPublished < _galleryMatches.Count;

        [ObservableProperty]
        private string _text = string.Empty;

        [ObservableProperty]
        private decimal _number;

        [ObservableProperty]
        private bool _isChecked;

        [ObservableProperty]
        private BehaviorChoiceViewModel? _choice;

        [ObservableProperty]
        private string _choiceSearchText = string.Empty;

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
            ChoicePreviewService? previews = null)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Store = store ?? throw new ArgumentNullException(nameof(store));
            RunEditFunc = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            _valueChanged = valueChanged;
            _previews = previews;
            // Wrapping the choices costs nothing: no picture is decoded or rendered until a tile
            // that shows one exists, and then only for the page that has been published. Building
            // the rows used to decode every load screen - around thirty megabytes of DDS - before
            // the tab could draw, which is what made switching to Area Transition stall.
            Choices = BehaviorChoiceViewModel.From(choices ?? definition.Choices);
        }

        /// <summary>
        /// Reads this row's value out of the document. Called by the constructor of the concrete row
        /// — not by this one, so a subclass finishes initializing its own state first.
        /// </summary>
        public void Reload()
        {
            _loading = true;
            try
            {
                ReadValue();
            }
            finally
            {
                _loading = false;
            }

            if (IsSearchableChoice)
                ChoiceSearchText = string.Empty;

            RebuildFilteredChoices();

            // An inline grid is part of the row rather than something opened, so it is built with the
            // row. Only the published page costs anything: the tiles beyond it are not realized and
            // their pictures are not requested until the builder scrolls to them. A popup gallery
            // still waits to be opened - four figures of portraits is not a page's worth of anything.
            if (IsInlineGallery)
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
        }

        [RelayCommand]
        private void ClearSearch() => ChoiceSearchText = string.Empty;

        /// <summary>
        /// Opens a popup gallery, building it on the first open. Until then the row has paid for
        /// exactly one picture — the one it is showing.
        /// </summary>
        [RelayCommand]
        private void OpenGallery()
        {
            if (!IsPopupGallery)
                return;

            IsGalleryOpen = true;
            if (_galleryBuilt)
                return;

            _galleryBuilt = true;
            RebuildGallery();
        }

        [RelayCommand]
        private void LoadMoreGallery() => PublishGalleryPage();

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

        private void RebuildGallery()
        {
            if (_disposed)
                return;

            _galleryBuilt = true;
            var words = (GalleryQuery ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            _galleryMatches = Choices
                .Where(candidate => words.All(word =>
                    candidate.Display.Contains(word, StringComparison.OrdinalIgnoreCase) ||
                    (candidate.Detail?.Contains(word, StringComparison.OrdinalIgnoreCase) ?? false)))
                .ToList();
            _galleryPublished = 0;
            GalleryChoices.Clear();
            PublishGalleryPage();
        }

        private void PublishGalleryPage()
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

        /// <summary>Called whenever the selected choice changes, including during a reload.</summary>
        /// <remarks>
        /// A popup gallery resolves one picture, not the whole set: the chosen option is the only
        /// one on screen until the grid is opened.
        /// </remarks>
        protected virtual void OnChoiceSelected(BehaviorChoiceViewModel? value)
        {
            OnPropertyChanged(nameof(SelectedChoiceDisplay));

            // Every choice presentation marks what is stored rather than restating it underneath -
            // a gallery tile, and equally a searchable list's row - so the option a builder is
            // looking at is the answer to "which one is this", on load and after every pick.
            foreach (var choice in Choices)
                choice.IsSelected = ReferenceEquals(choice, value);

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
            if (!IsSearchableChoice)
                return;

            var query = ChoiceSearchText.Trim();
            FilteredChoices.Clear();

            var published = 0;
            foreach (var option in Choices)
            {
                if (query.Length > 0 &&
                    !option.Display.Contains(query, StringComparison.OrdinalIgnoreCase) &&
                    !(option.StringValue?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    continue;
                }

                FilteredChoices.Add(option);
                if (++published >= MaxSearchResults)
                    break;
            }

            // A filter excludes a non-match on purpose; the cap excludes one by accident. When the
            // list was truncated, put what is stored back at the top - a value the editor will not
            // show is one a builder cannot see they have.
            if (published >= MaxSearchResults && Choice != null && !FilteredChoices.Contains(Choice))
                FilteredChoices.Insert(0, Choice);

            OnPropertyChanged(nameof(SearchSummary));
        }

        /// <summary>Republishes the properties that depend on the stored value rather than on it alone.</summary>
        protected void NotifyValueShapeChanged()
        {
            OnPropertyChanged(nameof(SelectedChoiceDisplay));
            OnPropertyChanged(nameof(Counter));
            OnPropertyChanged(nameof(HasValue));
            OnPropertyChanged(nameof(IsEmpty));
        }
    }
}
