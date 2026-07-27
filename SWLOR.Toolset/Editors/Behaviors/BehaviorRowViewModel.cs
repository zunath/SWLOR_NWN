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
        private bool _galleryLoaded;
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
        /// True when the choices carry artwork, which the picker shows as a large preview plus a
        /// searchable gallery rather than as a list of names. The load screens, the door
        /// appearances, and the portraits all arrive this way.
        /// </summary>
        public bool IsGallery => IsChoice && Choices.Any(choice => choice.HasArtwork);

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

        /// <summary>The chosen option's artwork, shown large enough to actually judge.</summary>
        [ObservableProperty]
        private Bitmap? _selectedPreview;

        /// <summary>
        /// Whether the gallery is showing. Bound rather than left to the flyout so that picking an
        /// option can close it - a picker that stays open after you have chosen makes you dismiss it
        /// yourself to see what you did.
        /// </summary>
        [ObservableProperty]
        private bool _isGalleryOpen;

        [ObservableProperty]
        private string _galleryQuery = string.Empty;

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
            // No artwork is decoded here. Building the rows used to decode every load screen -
            // around thirty megabytes of DDS - before the tab could draw, which is what made
            // switching to Area Transition stall.
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
        /// Builds the gallery, once, when the picker is first opened. Until then the row has paid
        /// for exactly one image - the one it is showing.
        /// </summary>
        [RelayCommand]
        private void OpenGallery()
        {
            IsGalleryOpen = true;
            if (_galleryLoaded)
                return;

            _galleryLoaded = true;
            RebuildGallery();
        }

        [RelayCommand]
        private void LoadMoreGallery() => PublishGalleryPage();

        partial void OnGalleryQueryChanged(string value)
        {
            if (!_galleryLoaded)
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

            var words = (GalleryQuery ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            _galleryMatches = Choices
                .Where(candidate => words.All(word =>
                    candidate.Display.Contains(word, StringComparison.OrdinalIgnoreCase) ||
                    (candidate.Choice.ImageResRef?.Contains(
                        word, StringComparison.OrdinalIgnoreCase) ?? false)))
                .ToList();
            _galleryPublished = 0;
            GalleryChoices.Clear();
            PublishGalleryPage();
        }

        private void PublishGalleryPage()
        {
            if (_disposed || !_galleryLoaded)
                return;

            var end = Math.Min(_galleryPublished + GalleryPageSize, _galleryMatches.Count);
            for (var index = _galleryPublished; index < end; index++)
            {
                var choice = _galleryMatches[index];
                GalleryChoices.Add(choice);
                _ = LoadThumbnailAsync(choice);
            }

            _galleryPublished = end;
            OnPropertyChanged(nameof(GallerySummary));
            OnPropertyChanged(nameof(CanLoadMoreGallery));
        }

        private async Task LoadThumbnailAsync(BehaviorChoiceViewModel choice)
        {
            if (_previews == null || !choice.HasArtwork || choice.Thumbnail != null)
                return;

            choice.Thumbnail =
                _previews.Cached(choice.Choice.ImageResRef, ChoicePreviewService.ThumbnailWidth)
                ?? await _previews
                    .ResolveAsync(choice.Choice.ImageResRef, ChoicePreviewService.ThumbnailWidth)
                    .ConfigureAwait(true);
        }

        private async Task LoadSelectedPreviewAsync(BehaviorChoiceViewModel? choice)
        {
            if (_previews == null || choice == null || !choice.HasArtwork)
            {
                SelectedPreview = null;
                return;
            }

            SelectedPreview =
                _previews.Cached(choice.Choice.ImageResRef, ChoicePreviewService.PreviewWidth)
                ?? await _previews
                    .ResolveAsync(choice.Choice.ImageResRef, ChoicePreviewService.PreviewWidth)
                    .ConfigureAwait(true);
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
        /// Decodes one image, not the whole set: the chosen option is the only one on screen until
        /// the gallery is opened.
        /// </remarks>
        protected virtual void OnChoiceSelected(BehaviorChoiceViewModel? value)
        {
            OnPropertyChanged(nameof(SelectedChoiceDisplay));

            if (IsGallery)
                _ = LoadSelectedPreviewAsync(value);
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
