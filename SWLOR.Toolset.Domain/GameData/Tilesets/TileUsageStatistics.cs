using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.GameData.Tilesets
{
    /// <summary>
    /// Counts how often each tile id is placed across a module's areas that use a given tileset -
    /// the corpus-frequency signal the paint tools use to break ties when
    /// <see cref="SetRuleMatcher"/> leaves a cell underspecified (many legal tiles). Preferring the
    /// most-used tile makes an auto-solved fill look like hand-authored areas rather than picking an
    /// arbitrary legal-but-unusual tile. Scanning is a plain read of the module's .are files;
    /// callers build the table once per tileset (off the UI thread) and cache it.
    /// </summary>
    public static class TileUsageStatistics
    {
        /// <summary>
        /// Tile-id → placement count across every area in <paramref name="workspace"/> whose Tileset
        /// equals <paramref name="tilesetResRef"/> (case-insensitive). Areas that fail to load are
        /// skipped rather than aborting the scan. An empty result simply means no area uses that
        /// tileset yet.
        /// </summary>
        public static IReadOnlyDictionary<int, int> CountTiles(ModuleWorkspace workspace, string tilesetResRef)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            var counts = new Dictionary<int, int>();
            if (string.IsNullOrWhiteSpace(tilesetResRef))
                return counts;

            foreach (var resRef in workspace.EnumerateAreaResRefs())
            {
                AreDocument are;
                try
                {
                    are = AreDocument.Load(workspace.GetResourcePath(ResourceType.Area, resRef));
                }
                catch
                {
                    continue;
                }

                if (!string.Equals(are.Tileset ?? "", tilesetResRef, StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var tile in are.Tiles)
                {
                    var id = tile.GetIntOrNull("Tile_ID");
                    if (id is { } value && value >= 0)
                        counts[value] = counts.TryGetValue(value, out var n) ? n + 1 : 1;
                }
            }

            return counts;
        }

        /// <summary>
        /// A ranking function for <see cref="TilePainter"/> from a usage table: more-used tiles rank
        /// lower (preferred). Unseen tiles rank at 0 (after every used tile, whose ranks are
        /// negative), so a never-placed tile is only chosen when nothing more common fits.
        /// </summary>
        public static Func<int, int> RankByUsage(IReadOnlyDictionary<int, int> counts)
        {
            ArgumentNullException.ThrowIfNull(counts);
            return id => counts.TryGetValue(id, out var n) ? -n : 0;
        }
    }
}
