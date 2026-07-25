namespace SWLOR.Toolset.Domain.GameData.Tilesets
{
    /// <summary>
    /// Turns a parsed <see cref="TilesetDefinition"/> into a browsable <see cref="TilePalette"/>.
    /// </summary>
    /// <remarks>
    /// Two categories, "Groups" then "All tiles", and deliberately NOT a terrain tree. Terrain is a
    /// poor category axis for this corpus: bucketing single tiles by "all four corners share one
    /// terrain" collapses shp02's 579 tiles into 405 "wall" plus 164 mixed, and ttd01's 388 into 289
    /// "Desert" plus 84 mixed - two buckets that tell a builder nothing. The tileset's own named
    /// groups are the meaningful axis (shp02 44 groups, ttd01 53, wsf10 347, with names like
    /// "Ruin01_2x2", "WallGate01", "BridgeDoor01"), so they lead, with the flat tile list behind
    /// them as the fallback for anything a group does not cover.
    ///
    /// Error tolerance follows <see cref="Categories.StandardPaletteLoader"/>: never throws, a null
    /// tileset yields <see cref="TilePalette.Empty"/>, and anything skipped is explained through
    /// <c>reportProblem</c> instead of being emitted as a broken entry.
    /// </remarks>
    public static class TilePaletteBuilder
    {
        public const string GroupsCategoryName = "Groups";
        public const string AllTilesCategoryName = "All tiles";

        /// <summary>
        /// A group slot holding this value is a hole in the group's rectangle, not a tile id.
        /// </summary>
        /// <remarks>
        /// Non-rectangular groups are declared as a bounding Rows x Columns with -1 in the cells
        /// that are not theirs; 90 groups across the 70-file hak corpus (tib01's "Room - Pit, Lava",
        /// wsf10's "Ship [A]_Docked 4x2", ...) use it, and -1 is the ONLY out-of-bounds value that
        /// appears anywhere in it. So a -1 slot is preserved in <see cref="TilePaletteEntry.TileIds"/>
        /// rather than treated as corruption: dropping or renumbering it would shift every later
        /// slot and place the group's tiles in the wrong cells.
        /// </remarks>
        public const int EmptyGroupSlot = -1;

        public static TilePalette Build(
            TilesetDefinition? tileset,
            Func<uint, string?>? resolveStrRef = null,
            Action<string>? reportProblem = null)
        {
            if (tileset == null)
                return TilePalette.Empty;

            try
            {
                var categories = new List<TilePaletteCategory>(2);

                var groups = BuildGroupEntries(tileset, resolveStrRef, reportProblem);
                if (groups.Count > 0)
                    categories.Add(new TilePaletteCategory(GroupsCategoryName, groups));

                var tiles = BuildTileEntries(tileset);
                if (tiles.Count > 0)
                    categories.Add(new TilePaletteCategory(AllTilesCategoryName, tiles));

                return categories.Count == 0 ? TilePalette.Empty : new TilePalette(categories);
            }
            catch (Exception ex)
            {
                // A palette panel is not worth failing over; a builder can still paint from nothing.
                reportProblem?.Invoke($"Could not build the tile palette for '{tileset.Name}': {ex.Message}");
                return TilePalette.Empty;
            }
        }

        private static List<TilePaletteEntry> BuildGroupEntries(
            TilesetDefinition tileset,
            Func<uint, string?>? resolveStrRef,
            Action<string>? reportProblem)
        {
            var entries = new List<TilePaletteEntry>(tileset.Groups.Count);

            for (var index = 0; index < tileset.Groups.Count; index++)
            {
                var group = tileset.Groups[index];
                var label = GroupLabel(group, index, resolveStrRef);

                if (group.TileIndices.Count == 0)
                {
                    reportProblem?.Invoke($"Tileset '{tileset.Name}' group '{label}' lists no tiles; skipped.");
                    continue;
                }

                // The declared rectangle has to match the slot count, because the placement code
                // walks TileIds row-major against Columns - a mismatch would read the wrong cells.
                if (group.Rows * group.Columns != group.TileIndices.Count)
                {
                    reportProblem?.Invoke(
                        $"Tileset '{tileset.Name}' group '{label}' declares {group.Rows}x{group.Columns} " +
                        $"but lists {group.TileIndices.Count} tiles; skipped.");
                    continue;
                }

                var outOfRange = group.TileIndices
                    .Where(id => id != EmptyGroupSlot && (id < 0 || id >= tileset.Tiles.Count))
                    .ToList();
                if (outOfRange.Count > 0)
                {
                    reportProblem?.Invoke(
                        $"Tileset '{tileset.Name}' group '{label}' references tile(s) " +
                        $"{string.Join(", ", outOfRange.Distinct())} outside its {tileset.Tiles.Count}-tile " +
                        "list; skipped.");
                    continue;
                }

                if (group.TileIndices.All(id => id == EmptyGroupSlot))
                {
                    reportProblem?.Invoke($"Tileset '{tileset.Name}' group '{label}' is all empty slots; skipped.");
                    continue;
                }

                entries.Add(new TilePaletteEntry(
                    label,
                    group.TileIndices,
                    group.Columns,
                    group.Rows,
                    PreviewModelFor(tileset, group.TileIndices)));
            }

            return entries;
        }

        private static List<TilePaletteEntry> BuildTileEntries(TilesetDefinition tileset)
        {
            var entries = new List<TilePaletteEntry>(tileset.Tiles.Count);

            // The list index IS the tile id: it is what a group's Tile{n}= keys point at and what an
            // area's Tile_List stores in Tile_ID (see AreaTiles), so ordering by index is ordering
            // by id and no separate id needs carrying.
            for (var tileId = 0; tileId < tileset.Tiles.Count; tileId++)
            {
                var model = tileset.Tiles[tileId].Model;
                entries.Add(new TilePaletteEntry(
                    model.Length > 0 ? model : $"Tile {tileId}",
                    new[] { tileId },
                    1,
                    1,
                    model));
            }

            return entries;
        }

        /// <summary>
        /// The group's localized name when its strref resolves, otherwise the raw name from the .set.
        /// Localized names are worth preferring even though most corpus groups have none: ttd01
        /// carries strrefs on 39 of its 53 groups, and those read as "Ruined Building" rather than
        /// "Ruin01_2x2".
        /// </summary>
        private static string GroupLabel(
            TileGroupDefinition group, int index, Func<uint, string?>? resolveStrRef)
        {
            var strRef = group.StrRef;
            if (resolveStrRef != null && strRef.HasValue && strRef.Value >= 0)
            {
                string? resolved = null;
                try
                {
                    resolved = resolveStrRef((uint)strRef.Value);
                }
                catch
                {
                    // A TLK that cannot answer just means we fall through to the .set's own name.
                }

                if (!string.IsNullOrWhiteSpace(resolved))
                    return resolved.Trim();
            }

            return string.IsNullOrWhiteSpace(group.Name) ? $"Group {index}" : group.Name.Trim();
        }

        /// <summary>
        /// The first real tile's model, skipping the group's holes. Every one of the corpus's 37,146
        /// tiles declares a model, so this only comes back empty for a group that is nothing but
        /// holes - which the caller treats as unplaceable.
        /// </summary>
        private static string PreviewModelFor(TilesetDefinition tileset, IReadOnlyList<int> tileIndices)
        {
            foreach (var tileId in tileIndices)
            {
                if (tileId == EmptyGroupSlot)
                    continue;

                var model = tileset.Tiles[tileId].Model;
                if (model.Length > 0)
                    return model;
            }

            return "";
        }
    }
}
