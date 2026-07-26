using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tilesets;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for <see cref="TilePainter"/>: whole-tile terrain fill with border blend
    /// on a synthetic tileset (centre becomes solid terrain, orthogonal neighbours blend to it,
    /// re-paint is a fixed point), plus a corpus fixed-point gate proving the same idempotency holds
    /// against every real SWLOR tileset's corner rules.
    /// </summary>
    public class TilePainterTests
    {
        // ---- Synthetic tileset -------------------------------------------------------------
        // Two terrains, G(rass) and D(irt). Tiles cover a solid of each, an edge-half, and both
        // one-corner variants, so a single-cell paint can always fully blend its ring.

        private static TileDefinition Tile(string nw, string ne, string sw, string se,
            string top = "", string right = "", string bottom = "", string left = "") => new()
        {
            TopLeft = nw, TopRight = ne, BottomLeft = sw, BottomRight = se,
            Top = top, Right = right, Bottom = bottom, Left = left
        };

        private const int SolidGrass = 0;
        private const int SolidDirt = 1;

        private static TilesetDefinition Synthetic() => new()
        {
            Floor = "Grass",
            Terrains = new[]
            {
                new TerrainDefinition("Grass", null, null),
                new TerrainDefinition("Dirt", null, null)
            },
            Tiles = new[]
            {
                Tile("Grass", "Grass", "Grass", "Grass"), // 0 solid grass
                Tile("Dirt", "Dirt", "Dirt", "Dirt"),     // 1 solid dirt
                Tile("Grass", "Grass", "Dirt", "Dirt"),   // 2 north-half grass / south-half dirt
                Tile("Dirt", "Grass", "Grass", "Grass"),  // 3 one corner dirt (NW)
                Tile("Grass", "Dirt", "Dirt", "Dirt")     // 4 three corners dirt (all but NW)
            }
        };

        private static Func<int, int, TileCandidate?> Grid(
            IReadOnlyDictionary<(int, int), TileCandidate> cells, int width, int height) =>
            (c, r) => c >= 0 && r >= 0 && c < width && r < height && cells.TryGetValue((c, r), out var v)
                ? v
                : null;

        private static Dictionary<(int, int), TileCandidate> Filled(int width, int height, int tileId)
        {
            var cells = new Dictionary<(int, int), TileCandidate>();
            for (var r = 0; r < height; r++)
            for (var c = 0; c < width; c++)
                cells[(c, r)] = new TileCandidate(tileId, 0);
            return cells;
        }

        private static void Apply(Dictionary<(int, int), TileCandidate> cells, IEnumerable<TilePaintChange> changes)
        {
            foreach (var ch in changes)
                cells[(ch.Col, ch.Row)] = new TileCandidate(ch.TileId, ch.Orientation);
        }

        [Test]
        public void CanRotateTile_RejectsAnAsymmetricTerrainBoundary()
        {
            var tileset = Synthetic();
            var cells = new Dictionary<(int, int), TileCandidate>
            {
                [(0, 2)] = new(SolidGrass, 0),
                [(1, 2)] = new(SolidGrass, 0),
                [(2, 2)] = new(SolidGrass, 0),
                [(0, 1)] = new(2, 0),
                [(1, 1)] = new(2, 0),
                [(2, 1)] = new(2, 0),
                [(0, 0)] = new(SolidDirt, 0),
                [(1, 0)] = new(SolidDirt, 0),
                [(2, 0)] = new(SolidDirt, 0)
            };

            TilePainter.CanRotateTile(tileset, Grid(cells, 3, 3), 1, 1, 1)
                .Should().BeFalse();
        }

        [Test]
        public void CanRotateTile_RejectsAnIncompatibleCrosserBoundary()
        {
            var tileset = new TilesetDefinition
            {
                Tiles = new[]
                {
                    Tile("Grass", "Grass", "Grass", "Grass", top: "Wall", right: "Fence"),
                    Tile("Grass", "Grass", "Grass", "Grass", bottom: "Wall")
                }
            };
            var cells = new Dictionary<(int, int), TileCandidate>
            {
                [(0, 0)] = new(0, 0),
                [(0, 1)] = new(1, 0)
            };

            TilePainter.CanRotateTile(tileset, Grid(cells, 1, 2), 0, 0, 1)
                .Should().BeFalse();
        }

        [Test]
        public void CanRotateTile_AllowsASymmetricTile()
        {
            var tileset = Synthetic();
            var cells = Filled(3, 3, SolidGrass);

            TilePainter.CanRotateTile(tileset, Grid(cells, 3, 3), 1, 1, 1)
                .Should().BeTrue();
        }

        [Test]
        public void CanRotateTile_RejectsAHeightSeamEvenWhenTerrainNamesMatch()
        {
            var tileset = new TilesetDefinition
            {
                Tiles = new[]
                {
                    new TileDefinition
                    {
                        TopLeft = "Grass", TopRight = "Grass",
                        BottomLeft = "Grass", BottomRight = "Grass",
                        TopLeftHeight = 1, BottomLeftHeight = 1
                    },
                    Tile("Grass", "Grass", "Grass", "Grass")
                }
            };
            var cells = new Dictionary<(int, int), PlacedTileState>
            {
                [(-1, 0)] = new(1, 0, 1),
                [(0, 0)] = new(0, 0, 0),
                [(1, 0)] = new(1, 0, 0)
            };
            PlacedTileState? At(int c, int r) =>
                cells.TryGetValue((c, r), out var state) ? state : null;

            TilePainter.CanRotateTile(tileset, At, 0, 0, 2).Should().BeFalse(
                "the rotation would swap the high and low edges and tear the ground");
        }

        [Test]
        public void PaintTerrain_FillsCentreWithSolidTerrain()
        {
            var ts = Synthetic();
            var cells = Filled(3, 3, SolidGrass);

            var changes = TilePainter.PaintTerrain(ts, 3, 3, Grid(cells, 3, 3), 1, 1, "Dirt");
            Apply(cells, changes);

            var centre = cells[(1, 1)];
            foreach (var corner in new[] { TileCorner.NorthWest, TileCorner.NorthEast, TileCorner.SouthWest, TileCorner.SouthEast })
                TileAdjacency.WorldCornerTerrain(ts.Tiles[centre.TileId], centre.Orientation, corner)
                    .Should().Be("Dirt", "the painted cell fills with the chosen terrain on every corner");
        }

        [Test]
        public void PaintTerrain_BlendsOrthogonalNeighboursToTheSharedCorner()
        {
            var ts = Synthetic();
            var cells = Filled(3, 3, SolidGrass);

            var changes = TilePainter.PaintTerrain(ts, 3, 3, Grid(cells, 3, 3), 1, 1, "Dirt");
            Apply(cells, changes);

            // Each orthogonal neighbour must now carry Dirt on the two corners it shares with the centre.
            void AssertSharedEdgeIsDirt(int c, int r, TileEdge edgeTowardCentre)
            {
                var tile = cells[(c, r)];
                var (near, far) = TileAdjacency.SharedCorners(edgeTowardCentre);
                foreach (var corner in new[] { near, far })
                    TileAdjacency.WorldCornerTerrain(ts.Tiles[tile.TileId], tile.Orientation, corner)
                        .Should().Be("Dirt", $"neighbour ({c},{r}) must blend to the centre along its {edgeTowardCentre} edge");
            }

            AssertSharedEdgeIsDirt(1, 2, TileEdge.South); // north neighbour, shares its south edge with centre
            AssertSharedEdgeIsDirt(1, 0, TileEdge.North); // south neighbour
            AssertSharedEdgeIsDirt(2, 1, TileEdge.West);  // east neighbour
            AssertSharedEdgeIsDirt(0, 1, TileEdge.East);  // west neighbour

            // Each diagonal shares exactly one vertex with the centre and must carry the terrain
            // there too, or the blend leaves a torn corner. (A vertex is shared by four cells, so a
            // constraint that only consults orthogonal neighbours silently misses these.)
            void AssertCornerIsDirt(int c, int r, TileCorner cornerTowardCentre)
            {
                var tile = cells[(c, r)];
                TileAdjacency.WorldCornerTerrain(ts.Tiles[tile.TileId], tile.Orientation, cornerTowardCentre)
                    .Should().Be("Dirt", $"diagonal ({c},{r}) shares its {cornerTowardCentre} vertex with the painted cell");
            }

            AssertCornerIsDirt(0, 2, TileCorner.SouthEast); // NW diagonal
            AssertCornerIsDirt(2, 2, TileCorner.SouthWest); // NE diagonal
            AssertCornerIsDirt(0, 0, TileCorner.NorthEast); // SW diagonal
            AssertCornerIsDirt(2, 0, TileCorner.NorthWest); // SE diagonal
        }

        [Test]
        public void PaintTerrain_IsIdempotent()
        {
            var ts = Synthetic();
            var cells = Filled(3, 3, SolidGrass);

            Apply(cells, TilePainter.PaintTerrain(ts, 3, 3, Grid(cells, 3, 3), 1, 1, "Dirt"));

            var second = TilePainter.PaintTerrain(ts, 3, 3, Grid(cells, 3, 3), 1, 1, "Dirt");
            second.Should().BeEmpty("re-painting the same terrain on the same cell is a fixed point");
        }

        [Test]
        public void PaintTerrain_RepaintingExistingTerrain_ChangesNothing()
        {
            var ts = Synthetic();
            var cells = Filled(2, 2, SolidGrass);

            var changes = TilePainter.PaintTerrain(ts, 2, 2, Grid(cells, 2, 2), 0, 0, "Grass");
            changes.Should().BeEmpty("a cell already showing the painted terrain, in an all-grass field, needs no rewrite");
        }

        [Test]
        public void PaintTerrain_OutOfBounds_BlankTerrain_And_UnpaintableTerrain_ReturnEmpty()
        {
            var ts = Synthetic();
            var reader = Grid(Filled(2, 2, SolidGrass), 2, 2);

            TilePainter.PaintTerrain(ts, 2, 2, reader, 5, 5, "Dirt").Should().BeEmpty("out of bounds");
            TilePainter.PaintTerrain(ts, 2, 2, reader, 0, 0, "  ").Should().BeEmpty("blank terrain");
            TilePainter.PaintTerrain(ts, 2, 2, reader, 0, 0, "Lava").Should().BeEmpty("no tile presents 'Lava'");
        }

        [Test]
        public void PaintTerrain_WhenAnyPopulatedNeighbourCannotBlend_RejectsTheWholePaint()
        {
            var ts = new TilesetDefinition
            {
                Terrains = new[]
                {
                    new TerrainDefinition("Office_Vinyl", null, null),
                    new TerrainDefinition("Office_Alum", null, null)
                },
                Tiles = new[]
                {
                    Tile("Office_Vinyl", "Office_Vinyl", "Office_Vinyl", "Office_Vinyl"),
                    Tile("Office_Alum", "Office_Alum", "Office_Alum", "Office_Alum")
                }
            };
            var cells = Filled(3, 3, 0);

            var changes = TilePainter.PaintTerrain(
                ts, 3, 3, Grid(cells, 3, 3), 1, 1, "Office_Alum");

            changes.Should().BeEmpty(
                "a solid tile without any mixed transition cannot form a valid boundary, so the centre must not be applied alone");
        }

        [Test]
        public void PaintTerrain_UsesTileRankToBreakTies_WhenCurrentTileIsInvalid()
        {
            // Two solid-grass tiles (0 and a duplicate at index 5); painting Grass onto an all-dirt
            // field forces a change, and the rank picks the preferred solid-grass tile.
            var ts = new TilesetDefinition
            {
                Terrains = new[] { new TerrainDefinition("Grass", null, null), new TerrainDefinition("Dirt", null, null) },
                Tiles = new[]
                {
                    Tile("Grass", "Grass", "Grass", "Grass"), // 0
                    Tile("Dirt", "Dirt", "Dirt", "Dirt"),     // 1
                    Tile("Grass", "Grass", "Grass", "Grass"), // 2
                    Tile("Grass", "Grass", "Grass", "Grass"), // 3
                    Tile("Grass", "Grass", "Grass", "Grass")  // 4
                }
            };
            var cells = Filled(1, 1, SolidDirt);

            // Rank prefers tile id 3 (lowest rank value there).
            Func<int, int> rank = id => id == 3 ? -100 : 0;
            var changes = TilePainter.PaintTerrain(ts, 1, 1, Grid(cells, 1, 1), 0, 0, "Grass", rank);

            changes.Should().ContainSingle();
            changes[0].TileId.Should().Be(3, "the corpus-frequency rank breaks the tie among equal solid tiles");
        }

        [Test]
        public void FindSolidTile_PrefersOpenGroundOverBuiltUpTiles()
        {
            // Two tiles satisfy "all corners Grass" equally; only the PathNode tells them apart.
            var ts = new TilesetDefinition
            {
                Floor = "Grass",
                Terrains = new[] { new TerrainDefinition("Grass", null, null) },
                Tiles = new[]
                {
                    new TileDefinition
                    {
                        TopLeft = "Grass", TopRight = "Grass", BottomLeft = "Grass", BottomRight = "Grass",
                        PathNode = "B" // lower id, but obstructed (e.g. carries a wall)
                    },
                    new TileDefinition
                    {
                        TopLeft = "Grass", TopRight = "Grass", BottomLeft = "Grass", BottomRight = "Grass",
                        PathNode = "A" // open ground
                    }
                }
            };

            TilePainter.FindSolidTile(ts, "Grass")!.Value.TileId
                .Should().Be(1, "an open tile must win over a lower-id obstructed one");
        }

        [Test]
        public void FindSolidTile_RealCityTileset_PicksOpenCobbleNotAWalledTile()
        {
            // Regression for a new tcn01 area coming out as a field of walls: tcn01 has 244
            // crosser-free all-Cobble tiles, and id 0 (the lowest, so the old tie-break winner)
            // carries a building wall (PathNode B). No corpus area uses tcn01, so frequency ranking
            // cannot break the tie either - the PathNode preference is what has to.
            var catalog = new TilesetCatalog(ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                Path.Combine(RepoRoot, "SWLOR_Haks")));

            if (!catalog.TryGetTileset("tcn01", out var tileset))
            {
                Assert.Ignore("tcn01 could not be resolved from the haks.");
                return;
            }

            var fill = TilePainter.FindSolidTile(tileset, "Cobble");

            fill.Should().NotBeNull();
            var chosen = tileset.Tiles[fill!.Value.TileId];
            chosen.PathNode.Trim().Should().BeEquivalentTo("A", "the fill must be open, walkable ground");
            fill.Value.TileId.Should().NotBe(0, "tile 0 is cobble with a building wall on it");
        }

        [Test]
        public void PaintTerrain_NeverLeavesAOneSidedCrosser()
        {
            // Regression: painting Water into a tcn01 cobble plaza produced boundaries where one tile
            // declared a Dock and its neighbour declared nothing - half a pier jutting into open
            // water, with a hole where the other half should have been. A crosser spans the boundary,
            // so both sides must agree.
            var catalog = new TilesetCatalog(ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                Path.Combine(RepoRoot, "SWLOR_Haks")));

            if (!catalog.TryGetTileset("tcn01", out var tileset))
            {
                Assert.Ignore("tcn01 could not be resolved from the haks.");
                return;
            }

            var fill = TilePainter.FindSolidTile(tileset, "Cobble");
            fill.Should().NotBeNull();

            const int size = 6;
            var cells = new Dictionary<(int, int), TileCandidate>();
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
                cells[(c, r)] = fill!.Value;

            // Paint a small blob of water, as a user would.
            foreach (var (c, r) in new[] { (2, 2), (3, 2), (2, 3), (3, 3) })
                Apply(cells, TilePainter.PaintTerrain(tileset, size, size, Grid(cells, size, size), c, r, "Water"));

            var offenders = new List<string>();
            for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
            {
                var here = cells[(c, r)];
                foreach (var (dc, dr, edge) in new[]
                         {
                             (1, 0, TileEdge.East), (0, 1, TileEdge.North)
                         })
                {
                    if (!cells.TryGetValue((c + dc, r + dr), out var there))
                        continue;

                    var mine = TileAdjacency.WorldEdgeCrosser(tileset.Tiles[here.TileId], here.Orientation, edge) ?? "";
                    var theirs = TileAdjacency.WorldEdgeCrosser(
                        tileset.Tiles[there.TileId], there.Orientation, TileAdjacency.OppositeEdge(edge)) ?? "";

                    if (!string.Equals(mine, theirs, StringComparison.OrdinalIgnoreCase))
                        offenders.Add($"({c},{r})->{edge}: '{mine}' vs '{theirs}'");
                }
            }

            offenders.Should().BeEmpty(
                "a crosser spans the boundary, so a painted result must never leave one dangling:\n"
                + string.Join("\n", offenders));
        }

        [Test]
        public void FindSolidTile_PrefersACrosserFreeTile()
        {
            var ts = new TilesetDefinition
            {
                Floor = "Grass",
                Terrains = new[] { new TerrainDefinition("Grass", null, null) },
                Tiles = new[]
                {
                    Tile("Grass", "Grass", "Grass", "Grass", top: "wall"), // 0 solid grass but has a wall crosser
                    Tile("Grass", "Grass", "Grass", "Grass")               // 1 solid grass, no crossers
                }
            };

            TilePainter.FindSolidTile(ts, "Grass")!.Value.TileId.Should().Be(1, "a plain terrain fill avoids crosser tiles");
        }

        [Test]
        public void DefaultFillTerrain_PicksFloorWhenFillable()
        {
            TilePainter.DefaultFillTerrain(Synthetic()).Should().Be("Grass", "the tileset's declared Floor is fillable");
            TilePainter.FillableTerrains(Synthetic()).Should().Contain(new[] { "Grass", "Dirt" });
        }

        // ---- Corpus fixed-point gate -------------------------------------------------------

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
        public void PaintTerrain_IsIdempotent_AcrossEveryRealTileset()
        {
            var installPath = NwnInstallLocator.Locate();
            if (installPath == null)
            {
                Assert.Ignore("No local NWN:EE install; some tilesets resolve only against base-game data.");
                return;
            }

            var index = ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                Path.Combine(RepoRoot, "SWLOR_Haks"),
                KeyBifCatalog.Load(Path.Combine(installPath, "data")));
            var catalog = new TilesetCatalog(index);

            var tilesetsExercised = 0;
            var failures = new List<string>();

            foreach (var name in catalog.GetTilesetNames())
            {
                if (string.Equals(name, "fcx01", StringComparison.OrdinalIgnoreCase))
                    continue; // documented 'holes' corner exception (see SetRuleCorpusTests)
                if (!catalog.TryGetTileset(name, out var ts) || ts == null)
                    continue;

                var fillable = TilePainter.FillableTerrains(ts);
                if (fillable.Count < 2)
                    continue; // need a base terrain to fill and a second to paint over it

                var baseTile = TilePainter.FindSolidTile(ts, fillable[0]);
                if (baseTile is not { } solid)
                    continue;

                const int size = 5;
                var cells = new Dictionary<(int, int), TileCandidate>();
                for (var r = 0; r < size; r++)
                for (var c = 0; c < size; c++)
                    cells[(c, r)] = solid;

                var reader = Grid(cells, size, size);
                Apply(cells, TilePainter.PaintTerrain(ts, size, size, reader, 2, 2, fillable[1]));

                var second = TilePainter.PaintTerrain(ts, size, size, Grid(cells, size, size), 2, 2, fillable[1]);
                if (second.Count > 0 && failures.Count < 20)
                    failures.Add($"[{name}] paint '{fillable[1]}' over '{fillable[0]}' rewrote {second.Count} cells on the second pass (not a fixed point)");

                tilesetsExercised++;
            }

            tilesetsExercised.Should().BeGreaterThan(10, "the gate must exercise many real tilesets");
            failures.Should().BeEmpty(
                "painting must be idempotent on real tileset corner rules:\n" + string.Join("\n", failures));
        }
    }
}
