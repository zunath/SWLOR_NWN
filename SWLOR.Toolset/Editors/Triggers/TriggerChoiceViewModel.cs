using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Triggers;

namespace SWLOR.Toolset.Editors.Triggers
{
    /// <summary>
    /// One entry of a choice row. A choice that names artwork gets its thumbnail filled in once
    /// something asks for it — the gallery being opened, or this being the selected screen.
    /// </summary>
    public sealed partial class TriggerChoiceViewModel : ObservableObject
    {
        public TriggerChoice Choice { get; }

        public long Value => Choice.Value;

        public string Display => Choice.Display;

        /// <summary>Whether this choice has artwork at all, decoded or not.</summary>
        public bool HasArtwork => !string.IsNullOrWhiteSpace(Choice.ImageResRef);

        /// <summary>Null until the thumbnail has been decoded; the row shows the name meanwhile.</summary>
        [ObservableProperty]
        private Bitmap? _thumbnail;

        public TriggerChoiceViewModel(TriggerChoice choice)
        {
            Choice = choice;
        }

        /// <summary>The combo box and any text fallback use this for the selected row.</summary>
        public override string ToString() => Display;
    }
}
