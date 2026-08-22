using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Tilesets;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for <see cref="AreaTiles"/>: row-major (col,row) addressing over an .are Tile_List,
    /// and in-place writes that touch only Tile_ID/Tile_Orientation/Tile_Height (minimal diff, clean
    /// undo). Uses hand-built minimal .are documents so the grid layout under test is unambiguous.
    /// </summary>
    public class AreaTilesTests
    {
        /// <summary>Builds a minimal .are with the given dimensions and tiles (row-major); each tile also carries a distinctive Tile_Height so preservation is observable.</summary>
        private static AreDocument BuildArea(int width, int height, params (int Id, int Orientation)[] tiles)
        {
            var sb = new StringBuilder();
            sb.Append("{\"__data_type\":\"ARE \",");
            sb.Append($"\"Width\":{{\"type\":\"int\",\"value\":{width}}},");
            sb.Append($"\"Height\":{{\"type\":\"int\",\"value\":{height}}},");
            sb.Append("\"Tile_List\":{\"type\":\"list\",\"value\":[");
            for (var i = 0; i < tiles.Length; i++)
            {
                if (i > 0)
                    sb.Append(',');
                sb.Append("{\"__struct_id\":1,");
                sb.Append($"\"Tile_ID\":{{\"type\":\"int\",\"value\":{tiles[i].Id}}},");
                sb.Append($"\"Tile_Orientation\":{{\"type\":\"int\",\"value\":{tiles[i].Orientation}}},");
                sb.Append($"\"Tile_Height\":{{\"type\":\"int\",\"value\":{100 + i}}},");
                sb.Append("\"Tile_AnimLoop1\":{\"type\":\"byte\",\"value\":1}}");
            }

            sb.Append("]}}");
            return AreDocument.Parse(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        private static int Field(AreDocument are, int index, string name) =>
            are.Tiles[index].TryGet(name, out var f) ? (int)f.GetInteger() : int.MinValue;

        [Test]
        public void At_ReadsCellsRowMajor()
        {
            // 3 wide, 2 tall; ids encode (row*10 + col) so a mis-indexed read is obvious.
            var are = BuildArea(3, 2,
                (0, 0), (1, 0), (2, 0),
                (10, 0), (11, 0), (12, 0));

            AreaTiles.At(are, 0, 0)!.Value.TileId.Should().Be(0);
            AreaTiles.At(are, 2, 0)!.Value.TileId.Should().Be(2);
            AreaTiles.At(are, 0, 1)!.Value.TileId.Should().Be(10);
            AreaTiles.At(are, 2, 1)!.Value.TileId.Should().Be(12, "index is row*width + col");
        }

        [Test]
        public void StateAt_IncludesThePlacedTileHeight()
        {
            var are = BuildArea(1, 1, (7, 2));

            AreaTiles.StateAt(are, 0, 0).Should().Be(new PlacedTileState(7, 2, 100));
        }

        [Test]
        public void At_OutOfRange_ReturnsNull()
        {
            var are = BuildArea(2, 2, (1, 0), (2, 0), (3, 0), (4, 0));

            AreaTiles.At(are, -1, 0).Should().BeNull();
            AreaTiles.At(are, 2, 0).Should().BeNull("column 2 is outside a width-2 grid");
            AreaTiles.At(are, 0, 2).Should().BeNull();
        }

        [Test]
        public void SetTile_UpdatesIdAndOrientation()
        {
            var are = BuildArea(2, 1, (5, 0), (6, 0));

            AreaTiles.SetTile(are, 1, 0, 42, 3);

            var placed = AreaTiles.At(are, 1, 0)!.Value;
            placed.TileId.Should().Be(42);
            placed.Orientation.Should().Be(3);
        }

        [Test]
        public void SetTile_PreservesOtherTileFields()
        {
            var are = BuildArea(1, 1, (5, 0));
            var heightBefore = Field(are, 0, "Tile_Height");

            AreaTiles.SetTile(are, 0, 0, 99, 2);

            Field(are, 0, "Tile_Height").Should().Be(heightBefore, "paint must not disturb height/lighting/animation fields");
            Field(are, 0, "Tile_AnimLoop1").Should().Be(1);
        }

        [Test]
        public void SetTile_ToSameValues_ProducesNoByteChange()
        {
            var are = BuildArea(2, 1, (5, 1), (6, 2));
            var before = are.ToBytes();

            AreaTiles.SetTile(are, 0, 0, 5, 1); // identical to what's already there

            are.ToBytes().Should().Equal(before, "an unchanged paint must not rewrite any field");
        }

        [Test]
        public void SetTile_OutOfRange_IsNoOp()
        {
            var are = BuildArea(1, 1, (5, 0));
            var before = are.ToBytes();

            AreaTiles.SetTile(are, 3, 3, 42, 1);

            are.ToBytes().Should().Equal(before);
        }

        [Test]
        public void SetOrientation_And_SetHeightLevel_RoundTrip()
        {
            var are = BuildArea(1, 1, (5, 0));

            AreaTiles.SetOrientation(are, 0, 0, 2);
            AreaTiles.SetHeightLevel(are, 0, 0, 4);

            AreaTiles.At(are, 0, 0)!.Value.Orientation.Should().Be(2);
            AreaTiles.HeightLevelOf(are, 0, 0).Should().Be(4);
            // Round-trips through serialization.
            var reloaded = AreDocument.Parse(are.ToBytes());
            AreaTiles.At(reloaded, 0, 0)!.Value.Orientation.Should().Be(2);
            AreaTiles.HeightLevelOf(reloaded, 0, 0).Should().Be(4);
        }

        [Test]
        public void TryAdjustHeightLevel_RefusesToLowerBelowZero()
        {
            var are = BuildArea(1, 1, (5, 0));

            AreaTiles.SetHeightLevel(are, 0, 0, 0);
            AreaTiles.TryAdjustHeightLevel(are, 0, 0, -1).Should().BeFalse();
            AreaTiles.HeightLevelOf(are, 0, 0).Should().Be(0);

            AreaTiles.TryAdjustHeightLevel(are, 0, 0, 1).Should().BeTrue();
            AreaTiles.HeightLevelOf(are, 0, 0).Should().Be(1);
        }
    }
}
