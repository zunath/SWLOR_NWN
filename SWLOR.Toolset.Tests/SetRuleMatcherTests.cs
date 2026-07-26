using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tilesets;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for the WP7.2 <see cref="SetRuleMatcher"/>: hermetic filtering behaviour on a
    /// synthetic tileset, plus a corpus soundness gate - for interior cells of real areas, the
    /// constraint derived purely from the PLACED NEIGHBOURS must always include the tile that is
    /// actually there. That proves the matcher never excludes the correct answer given real context.
    /// </summary>
    public class SetRuleMatcherTests
    {
        // ---- Synthetic tileset (hermetic) --------------------------------------------------

        private static TileDefinition Tile(string nw, string ne, string sw, string se) => new()
        {
            TopLeft = nw,
            TopRight = ne,
            BottomLeft = sw,
            BottomRight = se
        };

        private static TileDefinition Tile(
            string terrain, int nwHeight, int neHeight, int swHeight, int seHeight) => new()
        {
            TopLeft = terrain,
            TopRight = terrain,
            BottomLeft = terrain,
            BottomRight = terrain,
            TopLeftHeight = nwHeight,
            TopRightHeight = neHeight,
            BottomLeftHeight = swHeight,
            BottomRightHeight = seHeight
        };

        private static TilesetDefinition Tileset(params TileDefinition[] tiles) => new() { Tiles = tiles };

        [Test]
        public void FindMatchingTiles_UnconstrainedConstraint_ReturnsEveryTileAtEveryOrientation()
        {
            var ts = Tileset(
                Tile("Grass", "Grass", "Grass", "Grass"),
                Tile("Dirt", "Dirt", "Dirt", "Dirt"));

            var all = SetRuleMatcher.FindMatchingTiles(ts, new TileConstraint());

            all.Should().HaveCount(2 * 4, "two tiles, four orientations each, nothing filtered");
        }

        [Test]
        public void FindMatchingTiles_CornerConstraint_KeepsOnlyTilesThatCanPresentItThere()
        {
            // A half-grass/half-dirt tile: NW+NE grass, SW+SE dirt.
            var ts = Tileset(Tile("Grass", "Grass", "Dirt", "Dirt"));

            // Require grass at the NW world corner. Orientation 0 (NW=grass) and orientation 1
            // (which rotates the SW->NW... ) - assert by reconstructing via TileAdjacency rather than
            // hard-coding: every returned candidate must actually present grass at NW.
            var constraint = new TileConstraint { NorthWest = "Grass" };
            var matches = SetRuleMatcher.FindMatchingTiles(ts, constraint);

            matches.Should().NotBeEmpty();
            matches.Should().OnlyContain(c =>
                TileAdjacency.CornerTerrainsMatch(
                    TileAdjacency.WorldCornerTerrain(ts.Tiles[c.TileId], c.Orientation, TileCorner.NorthWest), "Grass"));

            // And it must NOT include an orientation that puts dirt at NW.
            matches.Should().NotContain(c =>
                TileAdjacency.WorldCornerTerrain(ts.Tiles[c.TileId], c.Orientation, TileCorner.NorthWest)
                    .Equals("Dirt", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void FindMatchingTiles_ImpossibleConstraint_ReturnsEmpty()
        {
            var ts = Tileset(Tile("Grass", "Grass", "Grass", "Grass"));

            var matches = SetRuleMatcher.FindMatchingTiles(ts, new TileConstraint { NorthWest = "Lava" });

            matches.Should().BeEmpty();
        }

        [Test]
        public void FindMatchingTiles_FullySpecifiedFromATilesOwnCorners_IncludesThatTile()
        {
            var ts = Tileset(Tile("Grass", "Dirt", "Water", "Stone"));
            var t = ts.Tiles[0];

            var constraint = new TileConstraint
            {
                NorthWest = TileAdjacency.WorldCornerTerrain(t, 0, TileCorner.NorthWest),
                NorthEast = TileAdjacency.WorldCornerTerrain(t, 0, TileCorner.NorthEast),
                SouthWest = TileAdjacency.WorldCornerTerrain(t, 0, TileCorner.SouthWest),
                SouthEast = TileAdjacency.WorldCornerTerrain(t, 0, TileCorner.SouthEast)
            };

            SetRuleMatcher.FindMatchingTiles(ts, constraint)
                .Should().Contain(new TileCandidate(0, 0));
        }

        [Test]
        public void ConstraintFromNeighbours_PinsCornersSharedWithPlacedNeighbours()
        {
            var ts = Tileset(Tile("Grass", "Grass", "Grass", "Grass")); // id 0, uniform grass

            // Only a west neighbour is placed (at 4,5); the target cell is (5,5).
            TileCandidate? PlacedAt(int c, int r) => c == 4 && r == 5 ? new TileCandidate(0, 0) : null;

            var constraint = SetRuleMatcher.ConstraintFromNeighbours(ts, 5, 5, PlacedAt);

            constraint.NorthWest.Should().Be("Grass", "shared with the west neighbour's NE corner");
            constraint.SouthWest.Should().Be("Grass", "shared with the west neighbour's SE corner");
            constraint.NorthEast.Should().BeNull("no east or north neighbour touches this corner");
            constraint.SouthEast.Should().BeNull();
        }

        [Test]
        public void SolveCell_UsesNeighbourConstraintsToFilterCandidates()
        {
            var ts = Tileset(
                Tile("Grass", "Grass", "Grass", "Grass"), // id 0
                Tile("Dirt", "Dirt", "Dirt", "Dirt"));    // id 1

            TileCandidate? PlacedAt(int c, int r) => c == 0 && r == 0 ? new TileCandidate(0, 0) : null;

            // Cell (1,0)'s west neighbour is all-grass, pinning NW and SW to grass.
            var matches = SetRuleMatcher.SolveCell(ts, 1, 0, PlacedAt);

            matches.Should().NotBeEmpty();
            matches.Should().OnlyContain(c => c.TileId == 0, "only the all-grass tile presents grass at NW and SW");
        }

        [Test]
        public void SolveCell_PaintOverride_WinsOverNeighbours()
        {
            var ts = Tileset(
                Tile("Grass", "Grass", "Grass", "Grass"), // id 0
                Tile("Dirt", "Dirt", "Dirt", "Dirt"));    // id 1

            TileCandidate? PlacedAt(int c, int r) => null; // empty grid

            var painted = new Dictionary<TileCorner, string> { [TileCorner.NorthWest] = "Dirt" };
            var matches = SetRuleMatcher.SolveCell(ts, 5, 5, PlacedAt, painted);

            matches.Should().OnlyContain(c => c.TileId == 1, "the painted Dirt corner excludes the all-grass tile");
        }

        [Test]
        public void SolveCell_UsesAbsoluteCornerElevationToRejectAVisualSeam()
        {
            var ts = Tileset(
                Tile("Grass", 0, 0, 0, 0),
                Tile("Grass", 1, 0, 1, 0));

            PlacedTileState? PlacedAt(int c, int r) => (c, r) switch
            {
                (0, 0) => new PlacedTileState(0, 0, 1),
                (1, 0) => new PlacedTileState(0, 0, 0),
                _ => null
            };

            var matches = SetRuleMatcher.SolveCell(ts, 1, 0, PlacedAt);

            matches.Should().Contain(new TileCandidate(1, 0),
                "its west corners rise to the neighbour's absolute height");
            matches.Should().NotContain(new TileCandidate(0, 0),
                "equal terrain names do not make mismatched corner elevations continuous");
        }

        // ---- Corpus soundness gate ---------------------------------------------------------

        private static string RepoRoot
        {
            get
            {
                var c = new DirectoryInfo(AppContext.BaseDirectory);
                while (c != null)
                {
                    if (File.Exists(Path.Combine(c.FullName, "Build", "hakbuilder.json")) &&
                        Directory.Exists(Path.Combine(c.FullName, "SWLOR_Haks")))
                        return c.FullName;
                    c = c.Parent;
                }
                throw new DirectoryNotFoundException("repo root not found");
            }
        }

        [Test]
        public void Matcher_FromPlacedNeighbours_IncludesTheActualTile_AcrossTheCorpus()
        {
            var installPath = NwnInstallLocator.Locate();
            if (installPath == null)
            {
                Assert.Ignore("No local NWN:EE install; corpus soundness gate needs base-game tilesets for some areas.");
                return;
            }

            var index = ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                Path.Combine(RepoRoot, "SWLOR_Haks"),
                KeyBifCatalog.Load(Path.Combine(installPath, "data")));
            var catalog = new TilesetCatalog(index);
            var workspace = new ModuleWorkspace(CorpusLocator.ModuleDirectory);

            var cellsChecked = 0;
            var failures = new List<string>();

            foreach (var resRef in workspace.EnumerateAreaResRefs())
            {
                var (are, _, _) = workspace.LoadArea(resRef);
                var tsRef = are.Tileset ?? "";
                // fcx01's "holes" gap terrain is the documented corner exception (see SetRuleCorpusTests);
                // skip it here rather than special-casing every holes cell.
                if (string.Equals(tsRef, "fcx01", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!catalog.TryGetTileset(tsRef, out var ts) || ts == null)
                    continue;

                int w = are.Width ?? 0, h = are.Height ?? 0;
                var tiles = are.Tiles;
                if (w <= 0 || h <= 0 || tiles.Count != w * h)
                    continue;

                int Field(int idx, string name) => tiles[idx].TryGet(name, out var f) ? (int)f.GetInteger() : -1;
                bool Valid(int idx) { var id = Field(idx, "Tile_ID"); return id >= 0 && id < ts.Tiles.Count; }
                int Id(int idx) => Field(idx, "Tile_ID");
                int Or(int idx) { var o = Field(idx, "Tile_Orientation"); return o < 0 ? 0 : o; }
                int Elevation(int idx) { var value = Field(idx, "Tile_Height"); return value < 0 ? 0 : value; }

                PlacedTileState? PlacedAt(int c, int r)
                {
                    if (c < 0 || r < 0 || c >= w || r >= h) return null;
                    var idx = r * w + c;
                    return Valid(idx) ? new PlacedTileState(Id(idx), Or(idx), Elevation(idx)) : null;
                }

                // Every cell: solve it from its placed neighbours alone (the cell's own tile is never
                // fed in), and require the actually-placed tile to be among the candidates.
                for (var row = 0; row < h; row++)
                for (var col = 0; col < w; col++)
                {
                    var i = row * w + col;
                    if (!Valid(i))
                        continue;

                    cellsChecked++;
                    var matches = SetRuleMatcher.SolveCell(ts, col, row, PlacedAt);
                    if (!matches.Contains(new TileCandidate(Id(i), Or(i))) && failures.Count < 30)
                        failures.Add($"[{tsRef}] {resRef} ({col},{row}) tile {Id(i)}@{Or(i)} not among {matches.Count} neighbour-derived candidates");
                }
            }

            cellsChecked.Should().BeGreaterThan(50_000, "the soundness gate must exercise a large sample of real interior cells");
            failures.Should().BeEmpty(
                "the neighbour-derived constraint must always admit the actually-placed tile:\n" + string.Join("\n", failures));
        }
    }
}
