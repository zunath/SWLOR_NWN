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

        // ---- Vertex painting (the reference toolset's terrain model) -----------------------
        // Verified against Aurora live: a terrain click names a grid VERTEX; exactly the cells
        // sharing that vertex are re-solved, each ending with the painted terrain on the corner
        // facing the vertex - no wider ring.

        private static Func<int, int, PlacedTileState?> States(
            IReadOnlyDictionary<(int, int), TileCandidate> cells, int width, int height)
        {
            var reader = Grid(cells, width, height);
            return (c, r) => reader(c, r) is { } tile ? new PlacedTileState(tile.TileId, tile.Orientation, 0) : null;
        }

        private static string CornerAt(TilesetDefinition ts, TileCandidate tile, TileCorner corner) =>
            TileAdjacency.WorldCornerTerrain(ts.Tiles[tile.TileId], tile.Orientation, corner);

        [Test]
        public void PaintTerrainVertex_RewritesExactlyTheFourCellsSharingTheVertex()
        {
            var ts = Synthetic();
            var cells = Filled(4, 4, SolidGrass);

            var changes = TilePainter.PaintTerrainVertex(ts, 4, 4, States(cells, 4, 4), 2, 2, "Dirt");

            changes.Should().NotBeEmpty();
            changes.Select(c => (c.Col, c.Row)).Should().BeSubsetOf(new[] { (1, 1), (2, 1), (1, 2), (2, 2) },
                "only the four cells sharing the painted vertex may change - the reference rewrites no wider ring");

            Apply(cells, changes);

            // Each touched cell carries Dirt exactly on the corner facing vertex (2,2), Grass elsewhere.
            var expectations = new (int Col, int Row, TileCorner TowardVertex)[]
            {
                (1, 1, TileCorner.NorthEast),
                (2, 1, TileCorner.NorthWest),
                (1, 2, TileCorner.SouthEast),
                (2, 2, TileCorner.SouthWest)
            };
            foreach (var (col, row, toward) in expectations)
            {
                foreach (var corner in new[]
                         {
                             TileCorner.NorthWest, TileCorner.NorthEast, TileCorner.SouthWest, TileCorner.SouthEast
                         })
                {
                    CornerAt(ts, cells[(col, row)], corner).Should().Be(
                        corner == toward ? "Dirt" : "Grass",
                        $"cell ({col},{row}) blends only its {toward} corner to the painted vertex");
                }
            }

            // Everything outside the four vertex cells is untouched.
            for (var r = 0; r < 4; r++)
            for (var c = 0; c < 4; c++)
            {
                if ((c is 1 or 2) && (r is 1 or 2))
                    continue;
                cells[(c, r)].Should().Be(new TileCandidate(SolidGrass, 0), $"cell ({c},{r}) does not touch the vertex");
            }
        }

        [Test]
        public void PaintTerrainVertex_CornerVertexTouchesOnlyItsOneCell()
        {
            var ts = Synthetic();
            var cells = Filled(2, 2, SolidGrass);

            var changes = TilePainter.PaintTerrainVertex(ts, 2, 2, States(cells, 2, 2), 0, 0, "Dirt");

            changes.Should().HaveCount(1);
            changes[0].Col.Should().Be(0);
            changes[0].Row.Should().Be(0);
            Apply(cells, changes);
            CornerAt(ts, cells[(0, 0)], TileCorner.SouthWest).Should().Be("Dirt",
                "the area's south-west corner vertex is cell (0,0)'s SW corner");
        }

        [Test]
        public void PaintTerrainVertex_DropsACrosserBothTouchedCellsMustAbandonTogether()
        {
            // Two of the four cells sharing the painted vertex also share a Wall crosser, and no
            // dirt-corner tile carries one - the valid final blend drops the wall on BOTH sides.
            // Filtering the first cell hard against the second's stale pre-paint edge refused this
            // vertex paint outright.
            var tileset = new TilesetDefinition
            {
                Floor = "Grass",
                Terrains = new[]
                {
                    new TerrainDefinition("Grass", null, null),
                    new TerrainDefinition("Dirt", null, null)
                },
                Tiles = new[]
                {
                    Tile("Grass", "Grass", "Grass", "Grass"),               // 0 solid grass
                    Tile("Dirt", "Dirt", "Dirt", "Dirt"),                   // 1 solid dirt
                    Tile("Grass", "Grass", "Dirt", "Dirt"),                 // 2 half blend
                    Tile("Dirt", "Grass", "Grass", "Grass"),                // 3 one-corner blend
                    Tile("Grass", "Dirt", "Dirt", "Dirt"),                  // 4 three-corner blend
                    Tile("Grass", "Grass", "Grass", "Grass", right: "Wall"),// 5 walled east edge
                    Tile("Grass", "Grass", "Grass", "Grass", left: "Wall")  // 6 walled west edge
                }
            };

            var cells = Filled(2, 2, SolidGrass);
            cells[(0, 0)] = new TileCandidate(5, 0);
            cells[(1, 0)] = new TileCandidate(6, 0);

            var changes = TilePainter.PaintTerrainVertex(tileset, 2, 2, States(cells, 2, 2), 1, 1, "Dirt");

            changes.Should().NotBeEmpty(
                "the vertex paint is legal when both sides of the walled edge drop the crosser together");
            Apply(cells, changes);

            var west = cells[(0, 0)];
            var east = cells[(1, 0)];
            CornerAt(tileset, west, TileCorner.NorthEast).Should().Be("Dirt");
            CornerAt(tileset, east, TileCorner.NorthWest).Should().Be("Dirt");
            (TileAdjacency.WorldEdgeCrosser(tileset.Tiles[west.TileId], west.Orientation, TileEdge.East) ?? "")
                .Should().BeEmpty("no dirt-corner tile can carry the wall, so it must be gone");
            (TileAdjacency.WorldEdgeCrosser(tileset.Tiles[east.TileId], east.Orientation, TileEdge.West) ?? "")
                .Should().BeEmpty("the far side must agree with the near side");
        }

        [Test]
        public void PaintTerrainVertex_IsIdempotent()
        {
            var ts = Synthetic();
            var cells = Filled(4, 4, SolidGrass);

            Apply(cells, TilePainter.PaintTerrainVertex(ts, 4, 4, States(cells, 4, 4), 2, 2, "Dirt"));

            TilePainter.PaintTerrainVertex(ts, 4, 4, States(cells, 4, 4), 2, 2, "Dirt")
                .Should().BeEmpty("re-painting a vertex with its own terrain is a fixed point");
        }

        [Test]
        public void PaintTerrainVertex_RefusesAtomicallyWhenNoTransitionTileExists()
        {
            // Solid tiles only - no corner transitions - so a mid-field vertex paint cannot blend.
            var ts = new TilesetDefinition
            {
                Terrains = new[]
                {
                    new TerrainDefinition("Grass", null, null),
                    new TerrainDefinition("Dirt", null, null)
                },
                Tiles = new[]
                {
                    Tile("Grass", "Grass", "Grass", "Grass"),
                    Tile("Dirt", "Dirt", "Dirt", "Dirt")
                }
            };
            var cells = Filled(4, 4, 0);

            TilePainter.PaintTerrainVertex(ts, 4, 4, States(cells, 4, 4), 2, 2, "Dirt")
                .Should().BeEmpty("an unsolvable vertex paint is a silent, atomic no-op - exactly the reference behavior");
        }

        [Test]
        public void PaintTerrainVertex_OutOfRangeAndBlankTerrainReturnEmpty()
        {
            var ts = Synthetic();
            var reader = States(Filled(2, 2, SolidGrass), 2, 2);

            TilePainter.PaintTerrainVertex(ts, 2, 2, reader, 3, 0, "Dirt").Should().BeEmpty("vertex past the grid");
            TilePainter.PaintTerrainVertex(ts, 2, 2, reader, -1, 0, "Dirt").Should().BeEmpty("negative vertex");
            TilePainter.PaintTerrainVertex(ts, 2, 2, reader, 1, 1, " ").Should().BeEmpty("blank terrain");
        }

        // ---- Crosser painting (the reference toolset's edge model) -------------------------
        // Verified against Aurora live on ztd01: each road dab re-solved exactly the two cells
        // sharing the clicked edge into single-edge stub tiles, and a second dab on another edge of
        // the same cell re-solved it into the two-edge corner piece.

        private const int RoadSolid = 0;   // plain grass, no crosser
        private const int RoadStub = 1;    // road on the North edge only
        private const int RoadCorner = 2;  // road on North + East

        private static TilesetDefinition RoadSet() => new()
        {
            Terrains = new[] { new TerrainDefinition("Grass", null, null) },
            Crossers = new[] { new CrosserDefinition("Road", null, null) },
            Tiles = new[]
            {
                Tile("Grass", "Grass", "Grass", "Grass"),
                Tile("Grass", "Grass", "Grass", "Grass", top: "Road"),
                Tile("Grass", "Grass", "Grass", "Grass", top: "Road", right: "Road")
            }
        };

        private static string EdgeOf(TilesetDefinition ts, TileCandidate tile, TileEdge edge) =>
            TileAdjacency.WorldEdgeCrosser(ts.Tiles[tile.TileId], tile.Orientation, edge);

        [Test]
        public void PaintCrosserEdge_RewritesExactlyTheTwoCellsSharingTheEdge()
        {
            var ts = RoadSet();
            var cells = Filled(3, 3, RoadSolid);

            // Vertical edge between (1,1) and (2,1): edge column 2, row 1.
            var changes = TilePainter.PaintCrosserEdge(ts, 3, 3, States(cells, 3, 3), 2, 1, verticalEdge: true, "Road");

            changes.Select(c => (c.Col, c.Row)).Should().BeEquivalentTo(new[] { (1, 1), (2, 1) },
                "only the two cells sharing the painted edge may change");

            Apply(cells, changes);
            EdgeOf(ts, cells[(1, 1)], TileEdge.East).Should().Be("Road");
            EdgeOf(ts, cells[(2, 1)], TileEdge.West).Should().Be("Road");
            // The stubs face each other and carry nothing else - measured Aurora behavior.
            foreach (var (cell, painted) in new[] { ((1, 1), TileEdge.East), ((2, 1), TileEdge.West) })
            {
                foreach (var edge in new[] { TileEdge.North, TileEdge.East, TileEdge.South, TileEdge.West })
                {
                    if (edge != painted)
                        EdgeOf(ts, cells[cell], edge).Should().BeEmpty(
                            $"cell {cell} carries the road only on its painted {painted} edge");
                }
            }
        }

        [Test]
        public void PaintCrosserEdge_SecondDabTurnsTheSharedCellIntoACorner()
        {
            var ts = RoadSet();
            var cells = Filled(3, 3, RoadSolid);

            Apply(cells, TilePainter.PaintCrosserEdge(ts, 3, 3, States(cells, 3, 3), 2, 1, true, "Road"));
            // North edge of the same cell (1,1): horizontal edge column 1, row 2.
            Apply(cells, TilePainter.PaintCrosserEdge(ts, 3, 3, States(cells, 3, 3), 1, 2, false, "Road"));

            // (1,1) must now carry the road on BOTH its East and North edges - the corner piece.
            EdgeOf(ts, cells[(1, 1)], TileEdge.East).Should().Be("Road");
            EdgeOf(ts, cells[(1, 1)], TileEdge.North).Should().Be("Road");
            cells[(1, 1)].TileId.Should().Be(RoadCorner);
        }

        [Test]
        public void PaintCrosserEdge_EraserDissolvesTheRoadBack()
        {
            var ts = RoadSet();
            var cells = Filled(3, 3, RoadSolid);

            Apply(cells, TilePainter.PaintCrosserEdge(ts, 3, 3, States(cells, 3, 3), 2, 1, true, "Road"));
            Apply(cells, TilePainter.PaintCrosserEdge(ts, 3, 3, States(cells, 3, 3), 2, 1, true, ""));

            cells[(1, 1)].TileId.Should().Be(RoadSolid, "erasing the edge returns the cell to plain ground");
            cells[(2, 1)].TileId.Should().Be(RoadSolid);
        }

        [Test]
        public void PaintCrosserEdge_BorderEdgeTouchesOneCellAndRepaintIsAFixedPoint()
        {
            var ts = RoadSet();
            var cells = Filled(2, 2, RoadSolid);

            // West border of the grid: vertical edge column 0 touches only cell (0, row).
            var changes = TilePainter.PaintCrosserEdge(ts, 2, 2, States(cells, 2, 2), 0, 0, true, "Road");
            changes.Select(c => (c.Col, c.Row)).Should().BeEquivalentTo(new[] { (0, 0) },
                "a border edge has one cell - the road runs off the map");
            Apply(cells, changes);
            EdgeOf(ts, cells[(0, 0)], TileEdge.West).Should().Be("Road");

            TilePainter.PaintCrosserEdge(ts, 2, 2, States(cells, 2, 2), 0, 0, true, "Road")
                .Should().BeEmpty("repainting the same crosser on the same edge is a fixed point");
        }

        [Test]
        public void PaintCrosserEdge_RefusesAtomicallyWhenNoCrosserTileExists()
        {
            var ts = Synthetic(); // no crosser-carrying tiles at all
            var cells = Filled(3, 3, SolidGrass);

            TilePainter.PaintCrosserEdge(ts, 3, 3, States(cells, 3, 3), 1, 1, true, "Road")
                .Should().BeEmpty("an unsolvable crosser paint is a silent, atomic no-op");
        }

        [Test]
        public void PaintableCrossers_ListsOnlyCrossersSomeTileCarries()
        {
            TilePainter.PaintableCrossers(RoadSet()).Should().BeEquivalentTo(new[] { "Road" });
            TilePainter.PaintableCrossers(Synthetic()).Should().BeEmpty();
        }

        [Test]
        public void PaintCrosserEdge_ToleratesALegacyOneSidedCrosserBesideThePaint()
        {
            // A tileset with no corner piece: only the solid and the single-edge stub. Hand-place a
            // one-sided road: cell (1,1) carries Road on its North edge while (1,2) is plain solid -
            // the asymmetric boundary the corpus genuinely contains. Strict symmetry would demand
            // any repaint of (1,1) keep Road on North AND take Road on East, which no tile offers;
            // the tolerant retry must still accept painting the East edge.
            var ts = new TilesetDefinition
            {
                Terrains = new[] { new TerrainDefinition("Grass", null, null) },
                Crossers = new[] { new CrosserDefinition("Road", null, null) },
                Tiles = new[]
                {
                    Tile("Grass", "Grass", "Grass", "Grass"),
                    Tile("Grass", "Grass", "Grass", "Grass", top: "Road")
                }
            };
            var cells = Filled(3, 3, 0);
            cells[(1, 1)] = new TileCandidate(1, 0); // Road on North, neighbour (1,2) blank - one-sided

            var changes = TilePainter.PaintCrosserEdge(ts, 3, 3, States(cells, 3, 3), 2, 1, true, "Road");

            changes.Should().NotBeEmpty(
                "the tolerant retry accepts the paint despite the unmatched legacy crosser next door");
            Apply(cells, changes);
            EdgeOf(ts, cells[(1, 1)], TileEdge.East).Should().Be("Road");
            EdgeOf(ts, cells[(2, 1)], TileEdge.West).Should().Be("Road");
        }

        [Test]
        public void CanPaintCrosserEdge_DistinguishesRefusalFromNoChange()
        {
            var ts = RoadSet();
            var cells = Filled(3, 3, RoadSolid);

            TilePainter.CanPaintCrosserEdge(ts, 3, 3, States(cells, 3, 3), 2, 1, true, "Road")
                .Should().BeTrue("a plain field accepts a road");

            Apply(cells, TilePainter.PaintCrosserEdge(ts, 3, 3, States(cells, 3, 3), 2, 1, true, "Road"));
            TilePainter.CanPaintCrosserEdge(ts, 3, 3, States(cells, 3, 3), 2, 1, true, "Road")
                .Should().BeTrue("repainting an existing road changes nothing but is still a valid paint");

            TilePainter.CanPaintCrosserEdge(Synthetic(), 3, 3, States(Filled(3, 3, SolidGrass), 3, 3), 1, 1, true, "Road")
                .Should().BeFalse("no tile carries the crosser - the cursor must show red");
            TilePainter.CanPaintCrosserEdge(ts, 3, 3, States(cells, 3, 3), 5, 1, true, "Road")
                .Should().BeFalse("out-of-range edge");
        }

        [Test]
        public void PaintTerrainVertex_DoesNotBuildWallsTheBuilderDidNotPaint()
        {
            // An interior-shaped tileset: a plain floor tile, and a floor tile that also carries a
            // wall on one edge. Both satisfy an all-floor corner constraint, and the engine's edge
            // rule is blank-tolerant, so the wall tile is *legal* beside plain floor - which is how
            // a terrain dab came to surround itself with walls in a scifi-base interior.
            var ts = new TilesetDefinition
            {
                Terrains = new[]
                {
                    new TerrainDefinition("Floor", null, null),
                    new TerrainDefinition("Rock", null, null)
                },
                Crossers = new[] { new CrosserDefinition("Wall", null, null) },
                Tiles = new[]
                {
                    Tile("Rock", "Rock", "Rock", "Rock"),                        // 0 solid rock
                    Tile("Floor", "Floor", "Floor", "Floor", top: "Wall"),       // 1 floor + wall (lower id: wins on rank)
                    Tile("Floor", "Floor", "Floor", "Floor"),                    // 2 plain floor
                    Tile("Floor", "Rock", "Rock", "Rock"),                       // 3 one-corner floor
                    Tile("Floor", "Floor", "Rock", "Rock")                       // 4 half floor
                }
            };
            var cells = Filled(4, 4, 2); // an open floor field, no crossers anywhere

            var changes = TilePainter.PaintTerrainVertex(ts, 4, 4, States(cells, 4, 4), 2, 2, "Floor");

            changes.Should().BeEmpty(
                "every touched cell already shows Floor on the painted corner, so the stable choice " +
                "is the tile already there - not the equally legal wall-carrying one");

            // And when the cells must change, the wall tile still must not be volunteered: repaint
            // a rock field's vertex to Floor and check no edge gains a crosser.
            var rocky = Filled(4, 4, 0);
            var rockChanges = TilePainter.PaintTerrainVertex(ts, 4, 4, States(rocky, 4, 4), 2, 2, "Floor");
            Apply(rocky, rockChanges);
            foreach (var (col, row) in new[] { (1, 1), (2, 1), (1, 2), (2, 2) })
            {
                foreach (var edge in new[] { TileEdge.North, TileEdge.East, TileEdge.South, TileEdge.West })
                {
                    TileAdjacency.WorldEdgeCrosser(ts.Tiles[rocky[(col, row)].TileId], rocky[(col, row)].Orientation, edge)
                        .Should().BeEmpty($"cell ({col},{row}) had no wall before the paint and asked for none");
                }
            }
        }

        [Test]
        public void CanPaintTerrainVertex_DistinguishesRefusalFromNoChange()
        {
            var ts = Synthetic();
            var cells = Filled(4, 4, SolidGrass);

            TilePainter.CanPaintTerrainVertex(ts, 4, 4, States(cells, 4, 4), 2, 2, "Dirt")
                .Should().BeTrue();
            TilePainter.CanPaintTerrainVertex(ts, 4, 4, States(cells, 4, 4), 2, 2, "Grass")
                .Should().BeTrue("repainting the terrain already there is a valid no-op");
            TilePainter.CanPaintTerrainVertex(ts, 4, 4, States(cells, 4, 4), 2, 2, "Lava")
                .Should().BeFalse("no tile presents 'Lava'");
            TilePainter.CanPaintTerrainVertex(ts, 4, 4, States(cells, 4, 4), 5, 2, "Dirt")
                .Should().BeFalse("vertex out of range");
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
        public void PaintTerrain_DropsACrosserBothTouchedCellsMustAbandonTogether()
        {
            // Regression: two ring cells share a Wall crosser, and no dirt-blend tile carries one -
            // the only valid final blend drops the wall on BOTH sides. Filtering the first cell
            // hard against the second cell's stale pre-paint edge refused this paint outright;
            // edges between cells the paint re-solves are jointly mutable.
            var tileset = new TilesetDefinition
            {
                Floor = "Grass",
                Terrains = new[]
                {
                    new TerrainDefinition("Grass", null, null),
                    new TerrainDefinition("Dirt", null, null)
                },
                Tiles = new[]
                {
                    Tile("Grass", "Grass", "Grass", "Grass"),               // 0 solid grass
                    Tile("Dirt", "Dirt", "Dirt", "Dirt"),                   // 1 solid dirt
                    Tile("Grass", "Grass", "Dirt", "Dirt"),                 // 2 half blend
                    Tile("Dirt", "Grass", "Grass", "Grass"),                // 3 one-corner blend
                    Tile("Grass", "Dirt", "Dirt", "Dirt"),                  // 4 three-corner blend
                    Tile("Grass", "Grass", "Grass", "Grass", right: "Wall"),// 5 walled east edge
                    Tile("Grass", "Grass", "Grass", "Grass", left: "Wall")  // 6 walled west edge
                }
            };

            var cells = Filled(3, 3, SolidGrass);
            cells[(0, 0)] = new TileCandidate(5, 0);
            cells[(1, 0)] = new TileCandidate(6, 0);

            var changes = TilePainter.PaintTerrain(tileset, 3, 3, Grid(cells, 3, 3), 1, 1, "Dirt");

            changes.Should().NotBeEmpty(
                "the paint is legal when both sides of the walled edge drop the crosser together");
            Apply(cells, changes);

            var west = cells[(0, 0)];
            var east = cells[(1, 0)];
            (TileAdjacency.WorldEdgeCrosser(tileset.Tiles[west.TileId], west.Orientation, TileEdge.East) ?? "")
                .Should().BeEmpty("no blend tile can carry the wall, so it must be gone");
            (TileAdjacency.WorldEdgeCrosser(tileset.Tiles[east.TileId], east.Orientation, TileEdge.West) ?? "")
                .Should().BeEmpty("the far side must agree with the near side");
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
