using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.AI;

public class WanderBoundsTests
{
    private readonly WanderBounds _bounds = new(0f, 0f, 124f, 150f);

    [TestCase(0f, 0f, 124f, 150f)]
    [TestCase(-5f, -10f, 5f, 10f)]
    public void ValidBounds_CanBeAuthored(float minX, float minY, float maxX, float maxY)
    {
        new WanderBounds(minX, minY, maxX, maxY).Contains(new Vector3(minX, minY, 5f)).Should().BeTrue();
    }

    [TestCase(0f, 0f, 0f, 10f)]
    [TestCase(10f, 0f, 0f, 10f)]
    [TestCase(0f, 10f, 10f, 0f)]
    [TestCase(float.NaN, 0f, 10f, 10f)]
    [TestCase(0f, 0f, float.PositiveInfinity, 10f)]
    public void InvalidBounds_AreRejected(float minX, float minY, float maxX, float maxY)
    {
        Action construct = () => new WanderBounds(minX, minY, maxX, maxY);
        construct.Should().Throw<ArgumentException>();
    }

    [Test]
    public void EastwardSteps_AreClampedWithoutPreventingRoaming()
    {
        var current = new Vector3(123f, 70f, 5f);
        var destination = _bounds.Constrain(current + new Vector3(3f, 1f, 0f));
        destination.Should().Be(new Vector3(124f, 71f, 5f));
        _bounds.AllowsStep(current, new Vector3(122f, 70f, 5f), destination, 15f).Should().BeTrue();
    }

    [TestCase(124.1f, 70f, 5f)]
    [TestCase(123f, -0.1f, 5f)]
    [TestCase(123f, 151f, 5f)]
    [TestCase(123f, 70f, float.NaN)]
    [TestCase(123f, 70f, 5f)]
    [TestCase(119f, 70f, 5f)]
    public void InvalidResolvedSteps_AreRejected(float x, float y, float z)
    {
        _bounds.AllowsStep(new Vector3(123f, 70f, 5f), new Vector3(122f, 70f, 5f),
            new Vector3(x, y, z), 15f).Should().BeFalse();
    }

    [Test]
    public void ShortStepBeyondHomeRadius_IsRejected()
    {
        _bounds.AllowsStep(new Vector3(115f, 70f, 5f), new Vector3(100f, 70f, 5f),
            new Vector3(117f, 70f, 5f), 15f).Should().BeFalse();
    }

    [Test]
    public void SpawnConfiguration_IsOptInPerEntry()
    {
        var spawns = new SpawnTableBuilder().Create("TEST")
            .AddSpawn(ObjectType.Creature, "bounded").RandomlyWalksWithin(_bounds).ReturnsHome()
            .AddSpawn(ObjectType.Creature, "ordinary").RandomlyWalks()
            .Build()["TEST"].Spawns;
        spawns[0].WanderBounds.Should().BeSameAs(_bounds);
        spawns[0].AIFlags.Should().Be(AIFlag.RandomWalk | AIFlag.ReturnHome);
        spawns[1].WanderBounds.Should().BeNull();
        spawns[1].AIFlags.Should().Be(AIFlag.RandomWalk);
    }
}
