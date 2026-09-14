using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.GameData.Tilesets;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// One cell of the palette grid: usually a blueprint, or - in Tiles mode - a tile or tile group.
    /// </summary>
    /// <remarks>
    /// Observable rather than a record because the preview arrives later: cells are published
    /// immediately and their thumbnails render on a background thread, so the grid appears at once and
    /// fills in rather than blocking on thousands of model loads.
    /// </remarks>
    public partial class PaletteTileViewModel : ObservableObject
    {
        public PaletteTileViewModel(
            string resRef,
            string name,
            string? categoryPath,
            PaletteSource source = PaletteSource.Custom)
        {
            ResRef = resRef;
            Name = name;
            CategoryPath = categoryPath;
            Source = source;
        }

        /// <summary>
        /// A tile or tile group rather than a blueprint. Its <see cref="ResRef"/> is the model the preview
        /// renders from, which is not a blueprint resref and must never be treated as one.
        /// </summary>
        public PaletteTileViewModel(TilePaletteEntry tile, string? categoryPath = null)
            : this(tile.PreviewModelResRef, tile.Label, categoryPath)
        {
            Tile = tile;
        }

        /// <summary>Non-null only in Tiles mode; what a click stamps into the area's grid.</summary>
        public TilePaletteEntry? Tile { get; }

        public bool IsTile => Tile != null;

        /// <summary>The palette half this blueprint came from; placement must preserve it.</summary>
        public PaletteSource Source { get; }

        /// <summary>
        /// For a blueprint, its resref. For a tile, the model resref its preview is rendered from - the
        /// grid shows it under the label either way, and for a tile that model name is the only stable
        /// identifier a builder can look up.
        /// </summary>
        public string ResRef { get; }

        public string Name { get; }

        /// <summary>
        /// The line under the label: a blueprint's resref, or a tile's footprint in cells.
        /// </summary>
        /// <remarks>
        /// Not the resref for a tile. <see cref="ResRef"/> holds the preview MODEL there, and groups
        /// routinely share one - four of shp02's groups preview from fci01_b01_01 - so printing it made
        /// four visibly different entries all claim the same name. The footprint is the thing a builder
        /// actually needs before clicking a cell: how much of the grid this stamp will overwrite.
        /// </remarks>
        public string Subtitle => Tile is { } tile
            ? tile.Columns * tile.Rows == 1 ? "1 tile" : $"{tile.Columns} x {tile.Rows} tiles"
            : ResRef;

        public string? CategoryPath { get; }

        public bool HasCategoryPath => !string.IsNullOrEmpty(CategoryPath);

        /// <summary>The rendered model, or null until it arrives - or forever, if it cannot be resolved.</summary>
        [ObservableProperty]
        private Bitmap? _preview;

        /// <summary>
        /// Whether this cell's preview has been asked for yet. Set by
        /// <see cref="PaletteViewModel.EnsurePreview"/> the first time the cell comes within reach of the
        /// viewport, so scrolling back over it does not queue the same work again.
        /// </summary>
        public bool PreviewRequested { get; set; }

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
