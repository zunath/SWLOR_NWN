using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Triggers;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Editors.Triggers
{
    /// <summary>
    /// One row of the trigger editor: a label on the left and its value on the right, in the shape
    /// the placeable editor uses.
    /// </summary>
    /// <remarks>
    /// Deliberately one view model rather than a subclass per kind. Every row shares the same
    /// geometry and differs only in which control sits in the value cell, so the alternative was
    /// nine near-identical classes and nine near-identical DataTemplates; the kind flags below let a
    /// single template switch the control instead.
    /// </remarks>
    public sealed partial class TriggerRowViewModel : ObservableObject
    {
        private readonly BehaviorValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Func<string, string?>? _resolveTag;
        private readonly ChoicePreviewService? _previews;
        private bool _galleryLoaded;
        private bool _loading;

        public BehaviorFieldDefinition Definition { get; }

        public string Label => Definition.Label;

        public bool IsRequired => Definition.IsRequired;

        public string? Note => Definition.Note;

        public bool HasNote => !string.IsNullOrEmpty(Definition.Note);

        /// <summary>Characters the box accepts; 0 lets Avalonia treat it as unlimited.</summary>
        public int MaxLength => Definition.MaxLength;

        /// <summary>
        /// "12/32", shown for as long as the row has a limit rather than only near it. A cap a
        /// builder cannot see until they hit it reads as the box breaking.
        /// </summary>
        public string? Counter => MaxLength > 0 ? $"{Text.Length}/{MaxLength}" : null;

        public bool HasCounter => MaxLength > 0;

        /// <summary>Resolved at construction, so a game-data choice set and a fixed one read alike.</summary>
        public IReadOnlyList<TriggerChoiceViewModel> Choices { get; }

        /// <summary>
        /// True when the choices carry artwork, which the picker shows as a large preview plus a
        /// gallery rather than as a list of names.
        /// </summary>
        public bool IsGallery => IsChoice && Choices.Any(choice => choice.HasArtwork);

        /// <summary>A plain list: every choice row that is not a gallery.</summary>
        public bool IsPlainChoice => IsChoice && !IsGallery;

        /// <summary>The selected screen, larger than a thumbnail so it can actually be judged.</summary>
        [ObservableProperty]
        private Bitmap? _selectedPreview;

        /// <summary>
        /// Whether the gallery is showing. Bound rather than left to the flyout so that picking a
        /// screen can close it — a picker that stays open after you have chosen makes you dismiss it
        /// yourself to see what you did.
        /// </summary>
        [ObservableProperty]
        private bool _isGalleryOpen;

        public bool IsText => Definition.Kind is BehaviorFieldKind.Text or BehaviorFieldKind.Script;
        public bool IsLocalizedText => Definition.Kind == BehaviorFieldKind.LocalizedText;
        public bool IsParagraph => Definition.Kind == BehaviorFieldKind.Paragraph;
        public bool IsNumber => Definition.Kind is BehaviorFieldKind.Integer or BehaviorFieldKind.Float;
        public bool IsCheck => Definition.Kind == BehaviorFieldKind.Check;
        public bool IsChoice => Definition.Kind == BehaviorFieldKind.Choice;
        public bool IsTagReference => Definition.Kind == BehaviorFieldKind.TagReference;
        public bool IsStatement => Definition.Kind == BehaviorFieldKind.Statement;

        /// <summary>Every kind that shows a single-line text box: text, scripts, names and tags.</summary>
        public bool IsTextEntry => IsText || IsTagReference || IsLocalizedText;

        [ObservableProperty]
        private string _text = string.Empty;

        [ObservableProperty]
        private decimal _number;

        [ObservableProperty]
        private bool _isChecked;

        [ObservableProperty]
        private TriggerChoiceViewModel? _choice;

        /// <summary>Live feedback beside the value: where a tag resolved, or why it did not.</summary>
        [ObservableProperty]
        private string? _status;

        [ObservableProperty]
        private bool _isStatusGood;

        public TriggerRowViewModel(
            BehaviorFieldDefinition definition,
            BehaviorValueStore store,
            Func<string, Action, bool> runEdit,
            Func<string, string?>? resolveTag,
            IReadOnlyList<BehaviorChoice>? choices = null,
            ChoicePreviewService? previews = null)
        {
            Definition = definition;
            _store = store;
            _runEdit = runEdit;
            _resolveTag = resolveTag;
            // No artwork is decoded here. Building the rows used to decode every load screen -
            // around thirty megabytes of DDS - before the tab could draw, which is what made
            // switching to Area Transition stall.
            Choices = (choices ?? definition.Choices)
                .Select(choice => new TriggerChoiceViewModel(choice))
                .ToList();
            _previews = previews;
            Reload();
        }

        /// <summary>Re-reads this row's value from the document, after an undo or a behavior swap.</summary>
        public void Reload()
        {
            if (Definition.Kind == BehaviorFieldKind.Statement)
                return;

            _loading = true;
            try
            {
                switch (Definition.Kind)
                {
                    case BehaviorFieldKind.Check:
                        IsChecked = _store.GetInteger(Definition.Storage, Definition.Name) == 1;
                        break;
                    case BehaviorFieldKind.Integer:
                        Number = _store.GetInteger(Definition.Storage, Definition.Name) ?? 0;
                        break;
                    case BehaviorFieldKind.Float:
                        Number = (decimal)(_store.GetFloat(Definition.Storage, Definition.Name) ?? 0);
                        break;
                    case BehaviorFieldKind.Choice:
                        var current = _store.GetInteger(Definition.Storage, Definition.Name) ?? 0;
                        Choice = Choices.FirstOrDefault(option => option.Value == current)
                                 ?? Choices.FirstOrDefault();
                        _ = LoadSelectedPreviewAsync(Choice);
                        break;
                    case BehaviorFieldKind.LocalizedText:
                        Text = _store.GetLocalizedText(Definition.Name);
                        break;
                    default:
                        Text = _store.GetString(Definition.Storage, Definition.Name);
                        break;
                }
            }
            finally
            {
                _loading = false;
            }

            UpdateStatus();
            OnPropertyChanged(nameof(Counter));
        }

        partial void OnTextChanged(string value)
        {
            if (_loading)
                return;

            var applied = Definition.Kind == BehaviorFieldKind.LocalizedText
                ? _runEdit($"Change {Label}", () => _store.SetLocalizedText(Definition.Name, value))
                : _runEdit($"Change {Label}",
                    () => _store.SetString(Definition.Storage, Definition.Name, Definition.FieldType, value));

            if (!applied)
                Reload();
            else
                UpdateStatus();

            OnPropertyChanged(nameof(Counter));
        }

        partial void OnNumberChanged(decimal value)
        {
            if (_loading)
                return;

            var applied = Definition.Kind == BehaviorFieldKind.Float
                ? _runEdit($"Change {Label}",
                    () => _store.SetFloat(Definition.Storage, Definition.Name, (double)value))
                : _runEdit($"Change {Label}",
                    () => _store.SetInteger(Definition.Storage, Definition.Name, Definition.FieldType, (long)value));

            if (!applied)
                Reload();
        }

        partial void OnIsCheckedChanged(bool value)
        {
            if (_loading)
                return;

            if (!_runEdit($"Toggle {Label}",
                    () => _store.SetInteger(Definition.Storage, Definition.Name, Definition.FieldType, value ? 1 : 0)))
                Reload();
        }

        partial void OnChoiceChanged(TriggerChoiceViewModel? value)
        {
            // One image, not the whole set: the selected screen is the only one on screen until the
            // gallery is opened.
            _ = LoadSelectedPreviewAsync(value);

            if (_loading || value == null)
                return;

            if (!_runEdit($"Change {Label}",
                    () => _store.SetInteger(Definition.Storage, Definition.Name, Definition.FieldType, value.Value)))
                Reload();
            else
                UpdateStatus();
        }

        /// <summary>
        /// Decodes the gallery's thumbnails, once, when the picker is first opened. Until then the
        /// editor has paid for exactly one image.
        /// </summary>
        [RelayCommand]
        private async Task OpenGallery()
        {
            IsGalleryOpen = true;

            if (_galleryLoaded || _previews == null)
                return;

            _galleryLoaded = true;
            foreach (var choice in Choices.Where(candidate => candidate.HasArtwork))
            {
                choice.Thumbnail = await _previews
                    .ResolveAsync(choice.Choice.ImageResRef, ChoicePreviewService.ThumbnailWidth)
                    .ConfigureAwait(true);
            }
        }

        private async Task LoadSelectedPreviewAsync(TriggerChoiceViewModel? choice)
        {
            if (_previews == null || choice == null || !choice.HasArtwork)
            {
                SelectedPreview = null;
                return;
            }

            SelectedPreview = _previews.Cached(choice.Choice.ImageResRef, ChoicePreviewService.PreviewWidth)
                ?? await _previews
                    .ResolveAsync(choice.Choice.ImageResRef, ChoicePreviewService.PreviewWidth)
                    .ConfigureAwait(true);
        }

        /// <summary>Picking from the gallery, which closes the flyout through the view.</summary>
        [RelayCommand]
        private void PickChoice(TriggerChoiceViewModel? choice)
        {
            if (choice != null)
                Choice = choice;

            IsGalleryOpen = false;
        }

        /// <summary>
        /// A required row that is empty says so, and a tag row says which area its target lives in —
        /// the check that catches a doorway pointing at a tag no area defines.
        /// </summary>
        private void UpdateStatus()
        {
            if (Definition.Kind == BehaviorFieldKind.TagReference && Text.Length > 0)
            {
                if (_resolveTag == null)
                {
                    Status = null;
                    return;
                }

                var area = _resolveTag(Text);
                IsStatusGood = area != null;
                Status = area != null ? $"✓ in {area}" : "✗ no area defines this tag";
                return;
            }

            // No "required" here: the label already carries that badge, and having both put two
            // pieces of text in the same row from opposite ends, which is what collided.
            Status = null;
        }
    }
}
