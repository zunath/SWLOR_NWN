using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Doors
{
    /// <summary>One named option in a door choice row, with artwork loaded only when requested.</summary>
    public sealed partial class DoorChoiceViewModel : ObservableObject
    {
        public BehaviorChoice Choice { get; }

        public long Value => Choice.Value;

        public string Display => Choice.Display;

        public bool HasArtwork => !string.IsNullOrWhiteSpace(Choice.ImageResRef);

        [ObservableProperty]
        private Bitmap? _thumbnail;

        public DoorChoiceViewModel(BehaviorChoice choice)
        {
            Choice = choice;
        }

        public override string ToString() => Display;
    }
}
