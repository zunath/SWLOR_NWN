using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Placeables;

namespace SWLOR.Toolset.Editors.Placeables
{
    /// <summary>One blueprint or visual effect in a behavior field's preview gallery.</summary>
    public partial class BehaviorGalleryTileViewModel : ObservableObject
    {
        private readonly BehaviorValueSourceProvider _sourceProvider;
        private readonly PlaceableValueSource _source;

        public BehaviorGalleryTileViewModel(
            BehaviorChoiceOption choice,
            PlaceableValueSource source,
            BehaviorValueSourceProvider sourceProvider)
        {
            Choice = choice;
            _source = source;
            _sourceProvider = sourceProvider;
            _preview = sourceProvider.CachedPreview(source, choice);
        }

        public BehaviorChoiceOption Choice { get; }
        public string Display => Choice.Display;
        public string Value => Choice.Value;
        public string? Group => Choice.Group;
        public string? Details => Choice.Details;
        public bool HasGroup => !string.IsNullOrWhiteSpace(Group);
        public bool HasDetails => !string.IsNullOrWhiteSpace(Details);
        public string Glyph => string.IsNullOrWhiteSpace(Display) ? "?" : Display.Trim()[..1].ToUpperInvariant();

        [ObservableProperty]
        private Bitmap? _preview;

        [ObservableProperty]
        private bool _isSelected;

        public bool PreviewRequested { get; private set; }

        public void EnsurePreview()
        {
            if (PreviewRequested)
                return;

            PreviewRequested = true;
            _sourceProvider.RequestPreview(_source, Choice, bitmap => Preview = bitmap);
        }
    }
}
