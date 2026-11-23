using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Tests.Service;

public class PlaceableSpawnRoomTests
{
    private record Box(float MinX, float MinY, float MaxX, float MaxY);

    // A segment is walkable when it touches none of the solid boxes (Liang-Barsky clipping).
    private static Func<float, float, float, float, bool> Geometry(params Box[] solids)
    {
        return (fromX, fromY, toX, toY) =>
        {
            foreach (var box in solids)
            {
                if (SegmentHitsBox(fromX, fromY, toX, toY, box))
                    return false;
            }

            return true;
        };
    }

    private static bool SegmentHitsBox(float x0, float y0, float x1, float y1, Box box)
    {
        var dx = x1 - x0;
        var dy = y1 - y0;
        var t0 = 0f;
        var t1 = 1f;
        foreach (var (p, q) in new List<(float, float)>
                 {
                     (-dx, x0 - box.MinX), (dx, box.MaxX - x0), (-dy, y0 - box.MinY), (dy, box.MaxY - y0),
                 })
        {
            if (p == 0f)
            {
                if (q < 0f)
                    return false;
                continue;
            }

            var t = q / p;
            if (p < 0f)
                t0 = MathF.Max(t0, t);
            else
                t1 = MathF.Min(t1, t);

            if (t0 > t1)
                return false;
        }

        return true;
    }

    // Walls of a north-south corridor of the given width, centered on X 0.
    private static Box[] Corridor(float width) => new[]
    {
        new Box(-50f, -50f, -width / 2f, 50f),
        new Box(width / 2f, -50f, 50f, 50f),
    };

    [Test]
    public void LeavesWalkingRoom_InOpenGround_IsTrue()
    {
        PlaceableSpawnRoom.LeavesWalkingRoom(0f, 0f, Geometry()).Should().BeTrue();
    }

    [Test]
    public void LeavesWalkingRoom_BesideASingleWall_IsTrue()
    {
        PlaceableSpawnRoom.LeavesWalkingRoom(0f, 0f, Geometry(new Box(1f, -50f, 50f, 50f))).Should().BeTrue();
    }

    [Test]
    public void LeavesWalkingRoom_InACornerOfARoom_IsTrue()
    {
        var walls = Geometry(new Box(1f, -50f, 50f, 50f), new Box(-50f, 1f, 50f, 50f));

        PlaceableSpawnRoom.LeavesWalkingRoom(0f, 0f, walls).Should().BeTrue();
    }

    [TestCase(3f)]
    [TestCase(4f)]
    [TestCase(5.5f)]
    public void LeavesWalkingRoom_InTheMiddleOfANarrowPassage_IsFalse(float width)
    {
        PlaceableSpawnRoom.LeavesWalkingRoom(0f, 0f, Geometry(Corridor(width))).Should().BeFalse();
    }

    [Test]
    public void LeavesWalkingRoom_InAPassageWithALaneBesideTheNode_IsTrue()
    {
        // 6.5 m wide: the ring stays inside the walls on both sides.
        PlaceableSpawnRoom.LeavesWalkingRoom(0f, 0f, Geometry(Corridor(6.5f))).Should().BeTrue();
    }

    [Test]
    public void LeavesWalkingRoom_AgainstOneWallOfAWidePassage_IsTrue()
    {
        // 6 m passage, point 1 m from the west wall: the lane on the east side stays open.
        PlaceableSpawnRoom.LeavesWalkingRoom(-2f, 0f, Geometry(Corridor(6f))).Should().BeTrue();
    }

    [Test]
    public void LeavesWalkingRoom_InFrontOfACorridorMouth_IsFalse()
    {
        // Room to the south of Y 0; a 3 m wide corridor leads north from the wall. The point sits 1.5 m in front of
        // the opening, so a node there would cover it.
        var walls = Geometry(new Box(-50f, 0f, -1.5f, 50f), new Box(1.5f, 0f, 50f, 50f));

        PlaceableSpawnRoom.LeavesWalkingRoom(0f, -1.5f, walls).Should().BeFalse();
    }

    [Test]
    public void LeavesWalkingRoom_FarFromACorridorMouth_IsTrue()
    {
        var walls = Geometry(new Box(-50f, 0f, -1.5f, 50f), new Box(1.5f, 0f, 50f, 50f));

        PlaceableSpawnRoom.LeavesWalkingRoom(0f, -5f, walls).Should().BeTrue();
    }

    [Test]
    public void LeavesWalkingRoom_BetweenTwoPillars_IsFalse()
    {
        var pillars = Geometry(new Box(-2.8f, -0.3f, -2.2f, 0.3f), new Box(2.2f, -0.3f, 2.8f, 0.3f));

        PlaceableSpawnRoom.LeavesWalkingRoom(0f, 0f, pillars).Should().BeFalse();
    }

    [Test]
    public void LeavesWalkingRoom_BesideOnePillar_IsTrue()
    {
        PlaceableSpawnRoom.LeavesWalkingRoom(0f, 0f, Geometry(new Box(2.2f, -0.3f, 2.8f, 0.3f))).Should().BeTrue();
    }

    [Test]
    public void LeavesWalkingRoom_InAnEnclosedPocket_IsFalse()
    {
        var pocket = Geometry(
            new Box(-50f, -50f, 50f, -1f), new Box(-50f, 1f, 50f, 50f),
            new Box(-50f, -50f, -1f, 50f), new Box(1f, -50f, 50f, 50f));

        PlaceableSpawnRoom.LeavesWalkingRoom(0f, 0f, pocket).Should().BeFalse();
    }
}
