using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.ContentBuilder.Services
{
    /// <summary>Where a decoded minimap texture was found (or that it wasn't).</summary>
    internal enum MinimapImageSource
    {
        Loose,
        BaseGameArchive,
        Missing
    }

    internal sealed class MinimapCacheEntry
    {
        public BitmapSource Image { get; init; }
        public MinimapImageSource Source { get; init; }
    }

    /// <summary>
    /// Resolves a tile's ImageMap2D resref to a decoded minimap BitmapSource and caches the result
    /// per (tileset, tileId) so regenerating a preview stays fast. Search order: loose .tga files
    /// under SWLOR_Haks (custom tilesets sometimes ship them), then the base game's KEY/BIF archive
    /// (covers the stock BioWare tilesets). Never throws — a texture that can't be found or decoded
    /// is reported as Missing so callers can fall back to schematic colors.
    /// </summary>
    internal static class MinimapCache
    {
        private static readonly Dictionary<(string Tileset, int TileId), MinimapCacheEntry> Cache = new();

        private static Dictionary<string, string> _looseTgaIndex;
        private static KeyBifReader _baseGameArchive;
        private static string _baseGameArchiveError;
        private static bool _baseGameArchiveAttempted;

        /// <summary>Human-readable reason the base game archive isn't available, or null if it is (or hasn't been needed yet).</summary>
        public static string BaseGameArchiveStatus => _baseGameArchiveError;

        public static MinimapCacheEntry GetOrLoad(TilesetModel tileset, TileRecord tile)
        {
            var key = (tileset.Resref, tile.TileId);
            if (Cache.TryGetValue(key, out var cached))
                return cached;

            var entry = Load(tile);
            Cache[key] = entry;
            return entry;
        }

        private static MinimapCacheEntry Load(TileRecord tile)
        {
            var resref = tile.ImageMap2D;
            if (string.IsNullOrWhiteSpace(resref))
                return new MinimapCacheEntry { Image = null, Source = MinimapImageSource.Missing };

            var loosePath = FindLooseTga(resref);
            if (loosePath != null)
            {
                try
                {
                    var bitmap = TgaDecoder.Decode(File.ReadAllBytes(loosePath));
                    if (bitmap != null)
                        return new MinimapCacheEntry { Image = bitmap, Source = MinimapImageSource.Loose };
                }
                catch
                {
                    // Fall through to the base-game archive / missing path.
                }
            }

            var archive = GetBaseGameArchive();
            if (archive != null)
            {
                if (archive.TryGetResourceBytes(resref, KeyBifReader.ResTypeTga, out var tgaBytes))
                {
                    try
                    {
                        var bitmap = TgaDecoder.Decode(tgaBytes);
                        if (bitmap != null)
                            return new MinimapCacheEntry { Image = bitmap, Source = MinimapImageSource.BaseGameArchive };
                    }
                    catch
                    {
                        // fall through to missing
                    }
                }
                // DDS fallback intentionally omitted: uncompressed-only DDS decoding is out of scope
                // for the base tilesets this preview targets (TGA covers them).
            }

            return new MinimapCacheEntry { Image = null, Source = MinimapImageSource.Missing };
        }

        private static string FindLooseTga(string resref)
        {
            var index = GetLooseTgaIndex();
            return index.TryGetValue(resref, out var path) ? path : null;
        }

        private static Dictionary<string, string> GetLooseTgaIndex()
        {
            if (_looseTgaIndex != null) return _looseTgaIndex;

            var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var path in Directory.EnumerateFiles(RepoPaths.HaksDirectory, "*.tga", SearchOption.AllDirectories))
                {
                    var name = Path.GetFileNameWithoutExtension(path);
                    if (!index.ContainsKey(name))
                        index[name] = path;
                }
            }
            catch (IOException)
            {
                // HaksDirectory missing/inaccessible: leave the index empty, everything falls
                // through to the base game archive (or Missing).
            }

            _looseTgaIndex = index;
            return _looseTgaIndex;
        }

        private static KeyBifReader GetBaseGameArchive()
        {
            if (_baseGameArchiveAttempted) return _baseGameArchive;

            _baseGameArchiveAttempted = true;
            _baseGameArchive = KeyBifReader.TryCreate(out _baseGameArchiveError);
            return _baseGameArchive;
        }
    }
}
