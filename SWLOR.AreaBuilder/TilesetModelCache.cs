using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.AreaBuilder
{
    /// <summary>
    /// Lazily parses each tileset's .set file the first time its profile is selected, by scanning
    /// SWLOR_Haks for a matching filename (same discovery approach as SWLOR.ProcgenReview). Parsed
    /// models are cached by tileset resref for the life of the process.
    /// </summary>
    internal static class TilesetModelCache
    {
        private static readonly Dictionary<string, TilesetModel> Cache =
            new(StringComparer.OrdinalIgnoreCase);

        public static TilesetModel Get(string tilesetResref)
        {
            if (Cache.TryGetValue(tilesetResref, out var cached))
                return cached;

            var setPath = Directory
                .EnumerateFiles(RepoPaths.HaksDirectory, tilesetResref + ".set", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (setPath == null)
            {
                throw new FileNotFoundException(
                    $"No .set file found for tileset '{tilesetResref}' under {RepoPaths.HaksDirectory}.");
            }

            var model = TilesetSetParser.Parse(tilesetResref, File.ReadAllText(setPath));
            Cache[tilesetResref] = model;
            return model;
        }
    }
}
