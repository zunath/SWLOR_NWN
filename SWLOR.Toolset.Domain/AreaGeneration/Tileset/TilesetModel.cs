#nullable disable
using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration.Tileset
{
    /// <summary>
    /// In-memory model of a tileset's .set data, sufficient for corner-matching tile resolution.
    /// Built offline by TilesetSetParser (unit tests, boot-time cache) or live via the NWNX Tileset plugin.
    /// </summary>
    public class TilesetModel
    {
        public string Resref { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsInterior { get; set; }
        public bool HasHeightTransition { get; set; }
        public float HeightTransition { get; set; }

        /// <summary>Terrain used for the area border ([GENERAL] Border).</summary>
        public string BorderTerrain { get; set; } = string.Empty;
        /// <summary>Terrain new areas are filled with ([GENERAL] Default) — the "solid" terrain for generation.</summary>
        public string DefaultTerrain { get; set; } = string.Empty;
        /// <summary>Primary walkable terrain ([GENERAL] Floor) — the "open" terrain for generation.</summary>
        public string FloorTerrain { get; set; } = string.Empty;

        public List<string> Terrains { get; set; } = new();
        public List<string> Crossers { get; set; } = new();

        /// <summary>Indexed by tile ID: Tiles[n] corresponds to [TILEn] in the .set file.</summary>
        public List<TileRecord> Tiles { get; set; } = new();
        public List<TileGroupRecord> Groups { get; set; } = new();
    }
}
