using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.AreaGenerationService.Tileset;

namespace SWLOR.ContentBuilder.Services
{
    /// <summary>
    /// Lazily parses each tileset's .set file the first time its profile is selected, using the
    /// shared TilesetSetSource resolution (SWLOR_Haks first, then basegame_sets — same discovery
    /// approach as SWLOR.ProcgenReview). Parsed models are cached by tileset resref for the life of
    /// the process.
    /// </summary>
    internal static class TilesetModelCache
    {
        private static readonly Dictionary<string, TilesetModel> Cache =
            new(StringComparer.OrdinalIgnoreCase);

        public static TilesetModel Get(string tilesetResref)
        {
            if (Cache.TryGetValue(tilesetResref, out var cached))
                return cached;

            var model = TilesetSetSource.Load(RepoPaths.Root, tilesetResref);
            Cache[tilesetResref] = model;
            return model;
        }
    }
}
