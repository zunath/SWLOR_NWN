using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Tilesets;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Deterministic unit coverage for <see cref="TileAdjacency"/>'s orientation rotation and the
    /// match predicates - fast and hermetic, complementing the full-corpus <see cref="SetRuleCorpusTests"/>.
    /// </summary>
    public class TileAdjacencyTests
    {
        // A tile whose corners/edges are labelled by their LOCAL (unrotated) position so a rotation
        // is easy to read: NW corner = "NW", Top edge = "T", etc.
        private static TileDefinition LabeledTile() => new()
        {
            TopLeft = "NW",
            TopLeftHeight = 1,
            TopRight = "NE",
            TopRightHeight = 2,
            BottomLeft = "SW",
            BottomLeftHeight = 3,
            BottomRight = "SE",
            BottomRightHeight = 4,
            Top = "T",
            Right = "R",
            Bottom = "B",
            Left = "L"
        };

        [Test]
        public void Orientation0_MapsLocalCornersAndEdgesToTheirNaturalWorldPositions()
        {
            var t = LabeledTile();

            TileAdjacency.WorldCornerTerrain(t, 0, TileCorner.NorthWest).Should().Be("NW");
            TileAdjacency.WorldCornerTerrain(t, 0, TileCorner.NorthEast).Should().Be("NE");
            TileAdjacency.WorldCornerTerrain(t, 0, TileCorner.SouthWest).Should().Be("SW");
            TileAdjacency.WorldCornerTerrain(t, 0, TileCorner.SouthEast).Should().Be("SE");

            TileAdjacency.WorldEdgeCrosser(t, 0, TileEdge.North).Should().Be("T");
            TileAdjacency.WorldEdgeCrosser(t, 0, TileEdge.East).Should().Be("R");
            TileAdjacency.WorldEdgeCrosser(t, 0, TileEdge.South).Should().Be("B");
            TileAdjacency.WorldEdgeCrosser(t, 0, TileEdge.West).Should().Be("L");
        }

        [Test]
        public void Orientation1_RotatesEverythingOneQuarterTurnCounterClockwise()
        {
            var t = LabeledTile();

            // A CCW quarter turn moves each local corner to the next world position CCW:
            // local NE -> world NW, local SE -> world NE, local SW -> world SE, local NW -> world SW.
            TileAdjacency.WorldCornerTerrain(t, 1, TileCorner.NorthWest).Should().Be("NE");
            TileAdjacency.WorldCornerTerrain(t, 1, TileCorner.NorthEast).Should().Be("SE");
            TileAdjacency.WorldCornerTerrain(t, 1, TileCorner.SouthEast).Should().Be("SW");
            TileAdjacency.WorldCornerTerrain(t, 1, TileCorner.SouthWest).Should().Be("NW");

            // Edges likewise: local East (R) rotates to world North, etc.
            TileAdjacency.WorldEdgeCrosser(t, 1, TileEdge.North).Should().Be("R");
            TileAdjacency.WorldEdgeCrosser(t, 1, TileEdge.West).Should().Be("T");
            TileAdjacency.WorldEdgeCrosser(t, 1, TileEdge.South).Should().Be("L");
            TileAdjacency.WorldEdgeCrosser(t, 1, TileEdge.East).Should().Be("B");

            TileAdjacency.WorldCornerHeight(t, 1, TileCorner.NorthWest).Should().Be(2);
            TileAdjacency.WorldCornerHeight(t, 1, TileCorner.NorthEast).Should().Be(4);
        }

        [Test]
        public void FourQuarterTurns_ReturnToTheStart()
        {
            var t = LabeledTile();
            TileAdjacency.WorldCornerTerrain(t, 4, TileCorner.NorthWest)
                .Should().Be(TileAdjacency.WorldCornerTerrain(t, 0, TileCorner.NorthWest));
        }

        [Test]
        public void SharedCornersAndOppositeEdge_PairAdjacentTilesConsistently()
        {
            TileAdjacency.OppositeEdge(TileEdge.East).Should().Be(TileEdge.West);
            TileAdjacency.OppositeEdge(TileEdge.North).Should().Be(TileEdge.South);

            // The east edge's two shared corners are the NE (north end) and SE (south end); the
            // neighbour's west edge pairs them with its NW and SW at the same ends.
            TileAdjacency.SharedCorners(TileEdge.East).Should().Be((TileCorner.NorthEast, TileCorner.SouthEast));
            TileAdjacency.SharedCorners(TileEdge.West).Should().Be((TileCorner.NorthWest, TileCorner.SouthWest));
        }

        [Test]
        public void CornerTerrainsMatch_IsCaseInsensitiveEquality()
        {
            TileAdjacency.CornerTerrainsMatch("Grass", "grass").Should().BeTrue();
            TileAdjacency.CornerTerrainsMatch("Grass", "Dirt").Should().BeFalse();
            TileAdjacency.CornerTerrainsMatch("", "").Should().BeTrue();
        }

        [Test]
        public void EdgeCrossersMatch_IsBlankTolerant()
        {
            TileAdjacency.EdgeCrossersMatch("Wall", "wall").Should().BeTrue();
            TileAdjacency.EdgeCrossersMatch("Wall", "Fence").Should().BeFalse();
            TileAdjacency.EdgeCrossersMatch("", "Wall").Should().BeTrue("a blank side never constrains the neighbour's crosser");
            TileAdjacency.EdgeCrossersMatch("Wall", "").Should().BeTrue();
            TileAdjacency.EdgeCrossersMatch("", "").Should().BeTrue();
        }
    }
}
