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

        [ObservableProperty]
        private bool _isCurrent;

        public AppearanceTileViewModel(AppearanceOption option, bool isCurrent)
        {
            Option = option ?? throw new ArgumentNullException(nameof(option));
            _isCurrent = isCurrent;
        }
    }
}
