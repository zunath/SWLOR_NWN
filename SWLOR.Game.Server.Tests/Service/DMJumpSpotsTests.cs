using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.DMToolService;

namespace SWLOR.Game.Server.Tests.Service;

[TestFixture]
public class DMJumpSpotsTests
{
    // The radius of the engine's own safe-point search around a jump target.
    private const float EngineSearchRadius = 20f;

    [Test]
    public void GetAreaSpots_StartsAtTheCenterLikeTheNativeChooser()
    {
        DMJumpSpots.GetAreaSpots(4, 4).First().Should().Be(new Vector2(20f, 20f));
        DMJumpSpots.GetAreaSpots(8, 4).First().Should().Be(new Vector2(40f, 20f));
    }

    [TestCase(1, 1)]
    [TestCase(1, 32)]
    [TestCase(4, 4)]
    [TestCase(7, 3)]
    [TestCase(32, 32)]
    public void GetAreaSpots_LeavesNoFloorOutOfTheEngineSearchRadius(int widthInTiles, int heightInTiles)
    {
        var spots = DMJumpSpots.GetAreaSpots(widthInTiles, heightInTiles).ToList();

        for (var x = 0f; x <= widthInTiles * 10f; x += 1f)
        {
            for (var y = 0f; y <= heightInTiles * 10f; y += 1f)
            {
                var floor = new Vector2(x, y);
                spots.Should().Contain(spot => Vector2.Distance(spot, floor) <= EngineSearchRadius,
                    $"({x}, {y}) has to be within reach of a spot");
            }
        }
    }

    [TestCase(1, 1)]
    [TestCase(32, 32)]
    public void GetAreaSpots_StaysInsideTheArea(int widthInTiles, int heightInTiles)
    {
        DMJumpSpots.GetAreaSpots(widthInTiles, heightInTiles).Should().OnlyContain(spot =>
            spot.X > 0f && spot.X < widthInTiles * 10f &&
            spot.Y > 0f && spot.Y < heightInTiles * 10f);
    }

    [Test]
    public void GetSpotsAround_SurroundsTheTargetCloseBy()
    {
        var center = new Vector2(20f, 1.4f);

        var spots = DMJumpSpots.GetSpotsAround(center).ToList();

        spots.Should().HaveCount(8);
        spots.Should().OnlyContain(spot => MathF.Abs(Vector2.Distance(spot, center) - 1.5f) < 0.001f);
        // A door blocks one axis, so both of its sides have to be tried.
        spots.Should().Contain(spot => spot.Y > center.Y + 1f);
        spots.Should().Contain(spot => spot.Y < center.Y - 1f);
    }
}
