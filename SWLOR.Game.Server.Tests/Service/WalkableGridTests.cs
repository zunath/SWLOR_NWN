using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Tests.Service;

public class WalkableGridTests
{
    private static WalkableGrid Grid(Func<float, float, (bool, float)> sample) => new(40f, 40f, sample);

    [Test]
    public void IsLineWalkable_OnFlatOpenGround_IsTrue()
    {
        Grid((_, _) => (true, 0f)).IsLineWalkable(10f, 10f, 20f, 15f).Should().BeTrue();
    }

    [Test]
    public void IsLineWalkable_ThroughAWall_IsFalse()
    {
        // Cave walls are ground the walkmesh does not carry: a strip of non-walkable surface from X 15 to 16.
        var grid = Grid((x, _) => (x < 15f || x > 16f, 0f));

        grid.IsLineWalkable(10f, 10f, 20f, 10f).Should().BeFalse();
    }

    [Test]
    public void IsLineWalkable_HuggingAWall_IsFalse()
    {
        // The line runs 0.2 m from the wall, closer than a creature's body allows.
        var grid = Grid((x, _) => (x < 15f, 0f));

        grid.IsLineWalkable(14.8f, 5f, 14.8f, 20f).Should().BeFalse();
    }

    [Test]
    public void IsLineWalkable_AlongsideAWallWithRoom_IsTrue()
    {
        var grid = Grid((x, _) => (x < 15f, 0f));

        grid.IsLineWalkable(13.5f, 5f, 13.5f, 20f).Should().BeTrue();
    }

    [Test]
    public void IsLineWalkable_OverACliff_IsFalse()
    {
        var grid = Grid((x, _) => (true, x < 15f ? 0f : -6f));

        grid.IsLineWalkable(10f, 10f, 20f, 10f).Should().BeFalse();
    }

    [Test]
    public void IsLineWalkable_UpAGentleSlope_IsTrue()
    {
        var grid = Grid((x, _) => (true, x * 0.3f));

        grid.IsLineWalkable(10f, 10f, 20f, 10f).Should().BeTrue();
    }

    [Test]
    public void IsLineWalkable_PastTheAreaEdge_IsFalse()
    {
        Grid((_, _) => (true, 0f)).IsLineWalkable(2f, 10f, -1f, 10f).Should().BeFalse();
    }

    [Test]
    public void IsLineWalkable_SamplesEachCellOnce()
    {
        var samples = 0;
        var grid = Grid((_, _) =>
        {
            samples++;
            return (true, 0f);
        });

        grid.IsLineWalkable(10f, 10f, 20f, 10f);
        var afterFirst = samples;
        grid.IsLineWalkable(10f, 10f, 20f, 10f);

        samples.Should().Be(afterFirst);
    }
}
