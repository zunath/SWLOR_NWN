using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// One blueprint in the palette grid: what it is called, its resref, and - when the palette is
    /// showing search results rather than one folder - which category it came from.
    /// </summary>
    /// <remarks>
    /// Observable rather than a record because the preview arrives later: tiles are published
    /// immediately and their thumbnails render on a background thread, so the grid appears at once and
    /// fills in rather than blocking on thousands of model loads.
    /// </remarks>
    public partial class PaletteTileViewModel : ObservableObject
    {
        public PaletteTileViewModel(string resRef, string name, string? categoryPath)
        {
            ResRef = resRef;
            Name = name;
            CategoryPath = categoryPath;
        }

        public string ResRef { get; }

        public string Name { get; }

        public string? CategoryPath { get; }

        public bool HasCategoryPath => !string.IsNullOrEmpty(CategoryPath);

        /// <summary>The rendered model, or null until it arrives - or forever, if it cannot be resolved.</summary>
        [ObservableProperty]
        private Bitmap? _preview;

        public bool HasPreview => Preview != null;

        partial void OnPreviewChanged(Bitmap? value) => OnPropertyChanged(nameof(HasPreview));

        /// <summary>
        /// Shown while a preview is still resolving, and permanently only when game data is not loaded at
        /// all (no NWN install or hak sources found) - with game data every tile ends up with an image,
        /// either real artwork or its type's symbol.
        /// </summary>
        public string Glyph => string.IsNullOrWhiteSpace(Name) ? "?" : Name.Trim()[..1].ToUpperInvariant();
    }
}
