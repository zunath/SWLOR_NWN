using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Behaviors
{
    /// <summary>
    /// One entry of a choice row, for every behavior editor. A choice that names artwork gets its
    /// thumbnail filled in once something asks for it — the gallery being opened, or this being the
    /// selected value.
    /// </summary>
    public sealed partial class BehaviorChoiceViewModel : ObservableObject
    {
        public BehaviorChoice Choice { get; }

        public long Value => Choice.Value;

        public string? StringValue => Choice.StringValue;

        public string Display => Choice.Display;

        public string? Identifier => Choice.Identifier;

        public bool HasIdentifier => !string.IsNullOrWhiteSpace(Identifier);

        public string? Summary => Choice.Summary;

        public bool HasSummary => !string.IsNullOrWhiteSpace(Summary);

        public bool CanPreviewAudio => Choice.CanPreviewAudio;

        /// <summary>Whether this choice has a picture at all — a texture or a model — drawn or not.</summary>
        public bool HasArtwork =>
            !string.IsNullOrWhiteSpace(Choice.ImageResRef) ||
            !string.IsNullOrWhiteSpace(Choice.ModelResRef) ||
            !string.IsNullOrWhiteSpace(Choice.ImageUrl) ||
            Choice.BlueprintPreviewType.HasValue &&
            !string.IsNullOrWhiteSpace(Choice.BlueprintPreviewResRef);

        /// <summary>The resource a tile is drawn from, shown under the name so it stays identifiable.</summary>
        public string? Detail =>
            Choice.BlueprintPreviewResRef ?? Choice.ModelResRef ?? Choice.ImageResRef;

        public bool HasDetail => !string.IsNullOrWhiteSpace(Detail);

        /// <summary>Whether this option stands for "any of them" and draws its own mark.</summary>
        public bool IsAny => Choice.IsAny;

        /// <summary>Stands in until the picture lands, so a grid is never a field of empty boxes.</summary>
        public string Glyph =>
            string.IsNullOrWhiteSpace(Display) ? "?" : Display.Trim()[..1].ToUpperInvariant();

        /// <summary>
        /// Whether the tile falls back to its letter: no picture yet, and no mark of its own. The
        /// "any of them" option is never a letter, because a letter on an empty tile reads as
        /// artwork that failed rather than as a choice.
        /// </summary>
        public bool ShowsGlyph => Thumbnail == null && !IsAny;

        /// <summary>Null until the thumbnail has been decoded; the row shows the name meanwhile.</summary>
        [ObservableProperty]
        private Bitmap? _thumbnail;

        /// <summary>Whether this is the stored value, which the gallery draws as the current tile.</summary>
        [ObservableProperty]
        private bool _isSelected;

        public BehaviorChoiceViewModel(BehaviorChoice choice)
        {
            Choice = choice ?? throw new ArgumentNullException(nameof(choice));
        }

        partial void OnThumbnailChanged(Bitmap? value) => OnPropertyChanged(nameof(ShowsGlyph));

        public static IReadOnlyList<BehaviorChoiceViewModel> From(
            IReadOnlyList<BehaviorChoice> choices)
        {
            ArgumentNullException.ThrowIfNull(choices);

            if (choices.Count == 0)
                return Array.Empty<BehaviorChoiceViewModel>();

            var wrapped = new BehaviorChoiceViewModel[choices.Count];
            for (var index = 0; index < choices.Count; index++)
                wrapped[index] = new BehaviorChoiceViewModel(choices[index]);

            return wrapped;
        }

        /// <summary>The combo box and any text fallback use this for the selected row.</summary>
        public override string ToString() => Display;
    }
}
