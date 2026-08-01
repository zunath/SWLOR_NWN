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

        /// <summary>Shown until the render lands, so a grid is never a field of empty boxes.</summary>
        public string Glyph => Caption.Length > 0 ? Caption[..1].ToUpperInvariant() : "?";

        [ObservableProperty]
        private Bitmap? _preview;

        /// <summary>
        /// Whether this tile has asked for its preview. The view sets this indirectly through
        /// <see cref="AppearanceGallerySectionViewModel.EnsurePreview"/> when the shared virtualizing
        /// panel realizes the tile, matching the palette's progressive preview-loading contract.
        /// </summary>
        public bool PreviewRequested { get; set; }

        [ObservableProperty]
        private bool _isCurrent;

        public AppearanceTileViewModel(AppearanceOption option, bool isCurrent)
        {
            Option = option ?? throw new ArgumentNullException(nameof(option));
            _isCurrent = isCurrent;
        }
    }
}
