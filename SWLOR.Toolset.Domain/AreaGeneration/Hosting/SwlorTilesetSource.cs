using Nwn.Authoring.Areas.Generation.Hosting;
using Nwn.Authoring.Areas.Generation.Tilesets;
using SWLOR.Toolset.Domain.GameData.Lookups;

namespace SWLOR.Toolset.Domain.AreaGeneration.Hosting;

/// <summary>Feeds the shared generator the tilesets indexed from the base game and HAKs.</summary>
public sealed class SwlorTilesetSource : IAreaGenerationTilesetSource
{
    private readonly TilesetCatalog _tilesets;

    public SwlorTilesetSource(TilesetCatalog tilesets)
    {
        _tilesets = tilesets ?? throw new ArgumentNullException(nameof(tilesets));
    }

    public IReadOnlyCollection<string> GetTilesetResRefs() => _tilesets.GetTilesetNames();

    public bool TryGetTileset(string tilesetResRef, out GeneratorTileset tileset)
    {
        if (!_tilesets.TryGetTileset(tilesetResRef, out var definition))
        {
            tileset = null!;
            return false;
        }

        tileset = new GeneratorTileset(TilesetSetParser.FromDefinition(tilesetResRef, definition), string.Empty);
        return true;
    }
}
