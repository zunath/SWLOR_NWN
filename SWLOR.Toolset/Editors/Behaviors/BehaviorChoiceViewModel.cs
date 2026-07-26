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

        /// <summary>Whether this choice has artwork at all, decoded or not.</summary>
        public bool HasArtwork => !string.IsNullOrWhiteSpace(Choice.ImageResRef);

        /// <summary>Null until the thumbnail has been decoded; the row shows the name meanwhile.</summary>
        [ObservableProperty]
        private Bitmap? _thumbnail;

        public BehaviorChoiceViewModel(BehaviorChoice choice)
        {
            Choice = choice ?? throw new ArgumentNullException(nameof(choice));
        }

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
