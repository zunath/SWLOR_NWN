using System.IO;
using System.Linq;

namespace SWLOR.Game.Server.Service.AreaGenerationService.Tileset
{
    /// <summary>
    /// Shared offline (repo-relative) resolution for a tileset's .set file, used by every OFFLINE tool
    /// and test that needs to parse tileset data outside the running game (SWLOR.ProcgenReview,
    /// SWLOR.ContentBuilder's TilesetModelCache, and the AreaGeneration test suite's tileset loaders).
    ///
    /// Resolution order, given a repository root:
    ///   1. SWLOR_Haks (searched recursively) -- hak-shipped copies take precedence because a tileset
    ///      onboarded into the haks may carry SWLOR-specific customizations (see tds01/tdt01, which are
    ///      both a base-game tileset AND a current hak tileset).
    ///   2. basegame_sets -- verbatim NWN:EE base-game .set files extracted from nwn_base.key/
    ///      nwn_retail.key, covering every base-game tileset that has not (yet) been copied into a hak.
    ///
    /// RUNTIME tileset loading (AreaGeneration.GetTilesetModel, via ResManGetFileContents) is
    /// deliberately NOT routed through this class: at runtime every tileset -- hak-shipped or
    /// base-game -- is already present through the engine's resource manager, so no repo-relative
    /// file search is needed or wanted there.
    /// </summary>
    public static class TilesetSetSource
    {
        private const string HaksDirectoryName = "SWLOR_Haks";
        private const string BaseGameSetsDirectoryName = "basegame_sets";

        /// <summary>
        /// Locates the .set file for <paramref name="tilesetResref"/> under <paramref name="repoRoot"/>,
        /// or null if it exists in neither SWLOR_Haks nor basegame_sets.
        /// </summary>
        public static string FindSetFilePath(string repoRoot, string tilesetResref)
        {
            var haksDirectory = Path.Combine(repoRoot, HaksDirectoryName);
            if (Directory.Exists(haksDirectory))
            {
                var haksMatch = Directory
                    .EnumerateFiles(haksDirectory, tilesetResref + ".set", SearchOption.AllDirectories)
                    .FirstOrDefault();
                if (haksMatch != null)
                    return haksMatch;
            }

            var baseGamePath = Path.Combine(repoRoot, BaseGameSetsDirectoryName, tilesetResref + ".set");
            return File.Exists(baseGamePath) ? baseGamePath : null;
        }

        /// <summary>
        /// Locates and parses the .set file for <paramref name="tilesetResref"/> under
        /// <paramref name="repoRoot"/>. Throws <see cref="FileNotFoundException"/> if the tileset is
        /// not found in either SWLOR_Haks or basegame_sets.
        /// </summary>
        public static TilesetModel Load(string repoRoot, string tilesetResref)
        {
            var path = FindSetFilePath(repoRoot, tilesetResref);
            if (path == null)
            {
                throw new FileNotFoundException(
                    $"No .set file found for tileset '{tilesetResref}' under {HaksDirectoryName} or {BaseGameSetsDirectoryName} (repo root '{repoRoot}').");
            }

            return TilesetSetParser.Parse(tilesetResref, File.ReadAllText(path));
        }
    }
}
