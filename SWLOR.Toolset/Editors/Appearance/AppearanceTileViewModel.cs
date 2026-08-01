using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SWLOR.Toolset.Editors.Appearance
{
    /// <summary>One tile in an appearance grid: a picture, a caption, and whether it is the current one.</summary>
    public sealed partial class AppearanceTileViewModel : ObservableObject
    {
        public AppearanceOption Option { get; }

        public string Caption => Option.Caption;

        public string? Detail => Option.Detail;

        public bool HasDetail => !string.IsNullOrEmpty(Option.Detail);

        /// <summary>
        /// Tile edge in pixels. Kept on the item itself so a virtualized cell does not have to bind
        /// through a ListBox whose data context is temporarily null while the cell is recycled.
        /// </summary>
        public double TileSize { get; }

        /// <summary>Picture height inside this tile.</summary>
        public double TileImageHeight => TileSize * 0.73;

        /// <summary>Shown until the render lands, so a grid is never a field of empty boxes.</summary>
        public string Glyph => Caption.Length > 0 ? Caption[..1].ToUpperInvariant() : "?";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPreview))]
        private Bitmap? _preview;

        /// <summary>Whether the rendered picture should replace the temporary fallback glyph.</summary>
        public bool HasPreview => Preview != null;

        /// <summary>
        /// Whether this tile has asked for its preview. The view sets this indirectly through
        /// <see cref="AppearanceGallerySectionViewModel.EnsurePreview"/> when the shared virtualizing
        /// panel realizes the tile, matching the palette's progressive preview-loading contract.
        /// </summary>
        public bool PreviewRequested { get; set; }

        [ObservableProperty]
        private bool _isCurrent;

        public AppearanceTileViewModel(AppearanceOption option, bool isCurrent, double tileSize)
        {
            Option = option ?? throw new ArgumentNullException(nameof(option));
            TileSize = tileSize;
            _isCurrent = isCurrent;
        }
    }
}
