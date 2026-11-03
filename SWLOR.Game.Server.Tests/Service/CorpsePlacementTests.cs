using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Tests.Service;

public class CorpsePlacementTests
{
    private const float AreaSize = 320f;
    private const float FloorHeight = 3f;
    private const float OffMeshHeight = -6f;

    private static float Floor(Vector3 position) => FloorHeight;

    [Test]
    public void GetLootPosition_BodyAwayFromEdges_KeepsPositionWithoutReadingGround()
    {
        var groundChecked = false;

        var position = CorpsePlacement.GetLootPosition(
            new Vector3(160f, 160f, FloorHeight),
            AreaSize,
            AreaSize,
            _ =>
            {
                groundChecked = true;
                return OffMeshHeight;
            });

        position.Should().Be(new Vector3(160f, 160f, FloorHeight - CorpsePlacement.SinkDepth));
        groundChecked.Should().BeFalse();
    }

    [Test]
    public void GetLootPosition_BodyOnFloorNearEdge_KeepsPosition()
    {
        // The reporter's ship stood at (165.267, 319.657) on the floor; a body there is reachable.
        var position = CorpsePlacement.GetLootPosition(new Vector3(165.267f, 319.657f, FloorHeight), AreaSize, AreaSize, Floor);

        position.Should().Be(new Vector3(165.267f, 319.657f, FloorHeight - CorpsePlacement.SinkDepth));
    }

    [Test]
    public void GetLootPosition_BodyOnFloorAtEngineEdgeLimit_KeepsPosition()
    {
        // The engine pushes creatures created at x = 0 to x = 0.3, still on the floor.
        var position = CorpsePlacement.GetLootPosition(new Vector3(0.3f, 160f, FloorHeight), AreaSize, AreaSize, Floor);

        position.Should().Be(new Vector3(0.3f, 160f, FloorHeight - CorpsePlacement.SinkDepth));
    }

    [Test]
    public void GetLootPosition_BodyOnNorthBoundary_MovesInsideOntoFloor()
    {
        // A ship that flew toward a point past the north edge stopped at (140, 320) with height -6.
        var position = CorpsePlacement.GetLootPosition(new Vector3(140f, 320f, OffMeshHeight), AreaSize, AreaSize, Floor);

        position.X.Should().Be(140f);
        position.Y.Should().Be(AreaSize - CorpsePlacement.EdgeMargin);
        position.Z.Should().BeApproximately(FloorHeight - CorpsePlacement.SinkDepth, 0.0001f);
    }

    [Test]
    public void GetLootPosition_BodyOnNorthEastCorner_MovesInsideOnBothAxes()
    {
        var position = CorpsePlacement.GetLootPosition(new Vector3(320f, 320f, OffMeshHeight), AreaSize, AreaSize, Floor);

        position.X.Should().Be(AreaSize - CorpsePlacement.EdgeMargin);
        position.Y.Should().Be(AreaSize - CorpsePlacement.EdgeMargin);
        position.Z.Should().BeApproximately(FloorHeight - CorpsePlacement.SinkDepth, 0.0001f);
    }

    [Test]
    public void GetLootPosition_BodyOnSouthWestCorner_MovesInsideOnBothAxes()
    {
        var position = CorpsePlacement.GetLootPosition(new Vector3(0f, -0.2f, OffMeshHeight), AreaSize, AreaSize, Floor);

        position.X.Should().Be(CorpsePlacement.EdgeMargin);
        position.Y.Should().Be(CorpsePlacement.EdgeMargin);
        position.Z.Should().BeApproximately(FloorHeight - CorpsePlacement.SinkDepth, 0.0001f);
    }

    [Test]
    public void GetLootPosition_BodyOnBoundary_ReadsGroundAtTheMovedPosition()
    {
        var checkedPosition = Vector3.Zero;

        CorpsePlacement.GetLootPosition(
            new Vector3(140f, 320f, OffMeshHeight),
            AreaSize,
            AreaSize,
            position =>
            {
                checkedPosition = position;
                return FloorHeight;
            });

        checkedPosition.X.Should().Be(140f);
        checkedPosition.Y.Should().Be(AreaSize - CorpsePlacement.EdgeMargin);
    }
}
