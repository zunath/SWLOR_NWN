using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Doors;

namespace SWLOR.Toolset.Editors.Doors
{
    /// <summary>One searchable model tile in the door Appearance tab.</summary>
    public sealed partial class DoorAppearanceTileViewModel : ObservableObject
    {
        public DoorAppearanceChoice Choice { get; }

        public string Caption => Choice.Display;

        public string ModelName => Choice.Model ?? string.Empty;

        [ObservableProperty]
        private Bitmap? _preview;

        [ObservableProperty]
        private bool _isCurrent;

        public DoorAppearanceTileViewModel(DoorAppearanceChoice choice, bool isCurrent)
        {
            Choice = choice;
            IsCurrent = isCurrent;
        }
    }
}
