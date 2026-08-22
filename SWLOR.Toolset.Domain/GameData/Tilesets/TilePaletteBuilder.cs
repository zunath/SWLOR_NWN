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
        public const string TerrainCategoryName = "Terrain";

        /// <summary>
        /// Named pieces that sit in a single row - an elevator, a front door, a double-wide entry.
        /// Aurora files these apart from Groups, and the distinction is one a builder feels: a feature
        /// goes against a wall, while a group claims a block of the map and has to be aimed.
        /// </summary>
        public const string FeaturesCategoryName = "Features";

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
                var categories = new List<TilePaletteCategory>(3);

                // Terrain leads because it is the brush: a builder lays ground first and reaches for a
                // specific piece afterwards. Crossers (roads, bridges, walls - painted onto grid
                // edges) file under the same category, before the terrains, matching the reference
                // toolset's Terrain tree. Only brushes the tileset can actually satisfy are offered -
                // a terrain needs a solid full tile, a crosser needs some tile carrying it.
                var brushes = BuildCrosserEntries(tileset, resolveStrRef);
                brushes.AddRange(BuildTerrainEntries(tileset, resolveStrRef));
                if (brushes.Count > 0)
                    categories.Add(new TilePaletteCategory(TerrainCategoryName, brushes));

                var allGroups = BuildGroupEntries(tileset, resolveStrRef, reportProblem);
                var features = allGroups.Where(IsFeature).ToList();
                var groups = allGroups.Where(entry => !IsFeature(entry)).ToList();

                if (features.Count > 0)
                    categories.Add(new TilePaletteCategory(FeaturesCategoryName, features));
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

        /// <summary>
        /// One brush per terrain the tileset can fill a whole cell with.
        /// </summary>
        /// <remarks>
        /// A terrain qualifies when some tile has that terrain on all four corners and no crosser on
        /// any edge - the same "solid" test <see cref="TilePainter"/> applies when it picks the centre
        /// tile. Offering a terrain the tileset cannot present that way would arm a brush whose every
        /// click silently did nothing.
        /// </remarks>
        private static List<TilePaletteEntry> BuildTerrainEntries(
            TilesetDefinition tileset,
            Func<uint, string?>? resolveStrRef)
        {
            var representative = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var id = 0; id < tileset.Tiles.Count; id++)
            {
                var tile = tileset.Tiles[id];
                var corner = tile.TopLeft;
                if (string.IsNullOrWhiteSpace(corner) ||
                    !string.Equals(corner, tile.TopRight, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(corner, tile.BottomLeft, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(corner, tile.BottomRight, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrWhiteSpace(tile.Top) || !string.IsNullOrWhiteSpace(tile.Right) ||
                    !string.IsNullOrWhiteSpace(tile.Bottom) || !string.IsNullOrWhiteSpace(tile.Left))
                    continue;

                if (!representative.ContainsKey(corner))
                    representative[corner] = id;
            }

            var entries = new List<TilePaletteEntry>();
            foreach (var terrain in tileset.Terrains)
            {
                if (string.IsNullOrWhiteSpace(terrain.Name) ||
                    !representative.TryGetValue(terrain.Name, out var tileId))
                    continue;

                entries.Add(new TilePaletteEntry(
                    TerrainLabel(terrain, resolveStrRef),
                    new[] { tileId },
                    Columns: 1,
                    Rows: 1,
                    tileset.Tiles[tileId].Model ?? string.Empty,
                    terrain.Name));
            }

            return entries;
        }

        /// <summary>
        /// A terrain's own name is what the .set author wrote and what the painter matches on, so it
        /// leads; a localized string is only used when it resolves to something.
        /// </summary>
        private static string TerrainLabel(TerrainDefinition terrain, Func<uint, string?>? resolveStrRef)
        {
            if (terrain.StrRef is { } strRef && strRef >= 0 &&
                resolveStrRef?.Invoke((uint)strRef) is { } resolved &&
                !string.IsNullOrWhiteSpace(resolved))
                return resolved;

            return terrain.Name;
        }

        /// <summary>
        /// One brush per crosser the tileset can actually paint - each has at least one tile
        /// carrying it on some edge, which is the same reachability test the terrains get. The
        /// entry's representative tile (thumbnail) is the first tile carrying the crosser.
        /// </summary>
        /// <summary>
        /// The eraser brush's label, and its sentinel: a crosser entry whose crosser is the empty
        /// string paints "nothing" onto an edge, which is how a road or wall is dissolved back to
        /// plain ground. Named like the reference toolset's own entry.
        /// </summary>
        public const string EraserLabel = "(Eraser)";

        private static List<TilePaletteEntry> BuildCrosserEntries(
            TilesetDefinition tileset,
            Func<uint, string?>? resolveStrRef)
        {
            var entries = new List<TilePaletteEntry>();

            var paintable = TilePainter.PaintableCrossers(tileset);
            if (paintable.Count > 0)
            {
                entries.Add(new TilePaletteEntry(
                    EraserLabel,
                    Array.Empty<int>(),
                    Columns: 1,
                    Rows: 1,
                    PreviewModelResRef: string.Empty,
                    Terrain: null,
                    Crosser: string.Empty));
            }

            foreach (var crosserName in paintable)
            {
                var representative = -1;
                for (var id = 0; id < tileset.Tiles.Count && representative < 0; id++)
                {
                    var tile = tileset.Tiles[id];
                    if (string.Equals(tile.Top, crosserName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(tile.Right, crosserName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(tile.Bottom, crosserName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(tile.Left, crosserName, StringComparison.OrdinalIgnoreCase))
                        representative = id;
                }

                if (representative < 0)
                    continue;

                var definition = tileset.Crossers.FirstOrDefault(candidate =>
                    string.Equals(candidate.Name, crosserName, StringComparison.OrdinalIgnoreCase));

                entries.Add(new TilePaletteEntry(
                    CrosserLabel(definition, crosserName, resolveStrRef),
                    new[] { representative },
                    Columns: 1,
                    Rows: 1,
                    tileset.Tiles[representative].Model ?? string.Empty,
                    Terrain: null,
                    Crosser: crosserName));
            }

            return entries;
        }

        /// <summary>Same name-first, strref-second rule as <see cref="TerrainLabel"/>.</summary>
        private static string CrosserLabel(
            CrosserDefinition? crosser, string fallbackName, Func<uint, string?>? resolveStrRef)
        {
            if (crosser?.StrRef is { } strRef && strRef >= 0 &&
                resolveStrRef?.Invoke((uint)strRef) is { } resolved &&
                !string.IsNullOrWhiteSpace(resolved))
                return resolved;

            return crosser?.Name ?? fallbackName;
        }

        /// <summary>
        /// Whether a group is really a feature: one that occupies a single row of the grid.
        /// </summary>
        /// <remarks>
        /// Split by footprint, because the .set has no flag saying which is which - Aurora is reading
        /// the same Rows/Columns that this does. The rule is inferred from the one tileset the two
        /// toolsets can be compared on: in tmi, Aurora files the elevators (1x1) AND the double-wide
        /// entries (1x2) under Features, and leaves Subway (3x4) as the only Group. A single row is
        /// the narrowest rule that separates those, and it matches what the pieces are - a feature
        /// goes against a wall, a group is a room.
        /// <para>
        /// A tileset with a wide single-row group would file it as a feature under this rule. None in
        /// the corpus does, and the cost if one appears is that it is listed under the wrong heading,
        /// not that it cannot be placed.
        /// </para>
        /// </remarks>
        private static bool IsFeature(TilePaletteEntry entry) => entry.Rows == 1;

        private static List<TilePaletteEntry> BuildGroupEntries(
            TilesetDefinition tileset,
            Func<uint, string?>? resolveStrRef,
            Action<string>? reportProblem)
        {
            var entries = new List<TilePaletteEntry>(tileset.Groups.Count);
            var used = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < tileset.Groups.Count; index++)
            {
                var group = tileset.Groups[index];
                var label = Disambiguate(GroupLabel(group, index, resolveStrRef), used);

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
                    PreviewModelFor(tileset, group.TileIndices),
                    Terrain: null,
                    Crosser: null,
                    FootprintModelResRefs: FootprintModelsFor(tileset, group.TileIndices)));
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
        /// Numbers a label that a previous group in the same tileset already used.
        /// </summary>
        /// <remarks>
        /// The .set files repeat names: tbx78 has three groups all called "room2x1". Distinguishing them
        /// is the whole point of a label, and this is a palette a builder picks from, so the copies get
        /// counted rather than left identical. Case-insensitive, because "Room2x1" and "room2x1" would
        /// read as the same label on screen.
        /// </remarks>
        private static string Disambiguate(string label, Dictionary<string, int> used)
        {
            if (!used.TryGetValue(label, out var seen))
            {
                used[label] = 1;
                return label;
            }

            // Keep counting from the first collision, and guard against a name that already looks
            // numbered colliding with the number we would add.
            string candidate;
            do
            {
                seen++;
                candidate = $"{label} ({seen})";
            }
            while (used.ContainsKey(candidate));

            used[label] = seen;
            used[candidate] = 1;
            return candidate;
        }

        /// <summary>
        /// The group's own name from the .set, falling back to its strref and then to a numbered
        /// placeholder.
        /// </summary>
        /// <remarks>
        /// <b>Name first, strref second</b> - the opposite of what a localized label usually deserves,
        /// because in this corpus the strrefs on custom tilesets are stale and the names are not.
        /// Measured on sw_t_modint2's tmi.set, whose first groups are AverageTwoWide, AverageFrontDoor,
        /// AverageElevator, PoorTwoWide, PoorFrontDoor, PoorElevator - and whose strrefs are 63552, 1, 2,
        /// 63552, 1, 2. Resolving those against the base dialog.tlk yields "Bath", "Barbarians", "Bard",
        /// "Bath", "Barbarians", "Bard": wrong for every one of them, and duplicated, because a copied
        /// .set brought another tileset's pointers with it. 64 of the 70 hak tilesets carry no group
        /// strref at all, so the name is the normal source anyway.
        /// <para>
        /// Adding the custom-TLK offset (16777216) would not rescue these - it is the right rule for a
        /// SWLOR strref, but these are not SWLOR strrefs. sw_tlk entry 1 is "Tough", entry 2 is "Tough
        /// Heroes", and 63552 is past the end of a 22,284-entry file, so the offset trades one set of
        /// wrong labels for another. The strrefs are simply not meant to be followed.
        /// </para>
        /// <para>
        /// The cost is that a base-game tileset shows its terse internal token - "Ruin01_2x2" rather than
        /// ttd01's localized "Ruined Building". That is a real loss, and the right trade: a terse label
        /// still identifies one group, while six groups all reading "Bath" identify none.
        /// </para>
        /// </remarks>
        private static string GroupLabel(
            TileGroupDefinition group, int index, Func<uint, string?>? resolveStrRef)
        {
            if (!string.IsNullOrWhiteSpace(group.Name))
                return group.Name.Trim();

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
                    // A TLK that cannot answer just means we fall through to the placeholder.
                }

                if (!string.IsNullOrWhiteSpace(resolved))
                    return resolved.Trim();
            }

            return $"Group {index}";
        }

        /// <summary>
        /// The first real tile's model, skipping the group's holes. Every one of the corpus's 37,146
        /// tiles declares a model, so this only comes back empty for a group that is nothing but
        /// holes - which the caller treats as unplaceable.
        /// </summary>
        /// <summary>
        /// Every footprint slot's model, in the group's own row-major order, blank where the slot is
        /// a hole or its tile declares no model - what the thumbnail composes the group's shape from.
        /// </summary>
        private static IReadOnlyList<string> FootprintModelsFor(
            TilesetDefinition tileset, IReadOnlyList<int> tileIndices)
        {
            var models = new string[tileIndices.Count];
            for (var slot = 0; slot < tileIndices.Count; slot++)
            {
                var tileId = tileIndices[slot];
                models[slot] = tileId == EmptyGroupSlot || tileId < 0 || tileId >= tileset.Tiles.Count
                    ? string.Empty
                    : tileset.Tiles[tileId].Model ?? string.Empty;
            }

            return models;
        }

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
