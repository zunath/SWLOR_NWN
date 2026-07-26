using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.GameData.Lookups;

namespace SWLOR.Toolset.Editors.Placeables
{
    /// <summary>
    /// One cell of the model grid. Observable because the preview arrives later: tiles are published
    /// at once and their renders fill in behind, the same way the palette's grid behaves.
    /// </summary>
    public partial class AppearanceTileViewModel : ObservableObject
    {
        [ObservableProperty]
        private Bitmap? _preview;

        [ObservableProperty]
        private bool _isCurrent;

        public AppearanceTileViewModel(PlaceableModelRow row, int usageCount)
        {
            Row = row;
            UsageCount = usageCount;
        }

        public PlaceableModelRow Row { get; }

        public int Id => Row.Id;

        public string ModelName => Row.ModelName;

        /// <summary>The row's label, or its model resref when it has none - as two thirds do.</summary>
        public string Caption => Row.DisplayName;

        public int UsageCount { get; }

        public string UsageText => UsageCount switch
        {
            0 => string.Empty,
            1 => "1 blueprint",
            _ => $"{UsageCount} blueprints"
        };
    }
}
