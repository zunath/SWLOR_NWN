using Nwn.Authoring.Areas.Tiles;
using Nwn.Toolset.Avalonia.Palettes.Workflow;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels.PaletteHost
{
    /// <summary>
    /// SWLOR's tilesets, shaped by the shared <see cref="TilePaletteBuilder"/>. <see cref="TilesetCatalog"/>
    /// caches the .set parse, so the repeat cost of a tab switch is the palette shaping alone.
    /// </summary>
    internal sealed class SwlorPaletteTilesetSource : IPaletteTilesetSource
    {
        private readonly TilesetCatalog _tilesets;
        private readonly Func<uint, string?>? _resolveStrRef;
        private readonly OutputLogService _log;

        public SwlorPaletteTilesetSource(
            TilesetCatalog tilesets,
            Func<uint, string?>? resolveStrRef,
            OutputLogService log)
        {
            _tilesets = tilesets ?? throw new ArgumentNullException(nameof(tilesets));
            _resolveStrRef = resolveStrRef;
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public PaletteTileset? Load(string tilesetResRef)
        {
            if (!_tilesets.TryGetTileset(tilesetResRef, out var tileset) || tileset == null)
                return null;

            return new PaletteTileset(
                TilePaletteBuilder.Build(tileset, _resolveStrRef, _log.AppendLine),
                _tilesets.GetDisplayName(tilesetResRef));
        }
    }
}
