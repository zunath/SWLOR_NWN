using System.Collections.Generic;
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Tests.Service;

public class SpawnSpacingTests
{
    private static Func<Vector3> Sequence(params Vector3[] candidates)
    {
        var queue = new Queue<Vector3>(candidates);
        return () => queue.Dequeue();
    }

    [Test]
    public void PickSpacedPosition_WithNoOccupiedPositions_TakesFirstCandidate()
    {
        var position = SpawnSpacing.PickSpacedPosition(
            Sequence(new Vector3(10, 10, 0), new Vector3(50, 50, 0)),
            new List<Vector3>());

        position.Should().Be(new Vector3(10, 10, 0));
    }

    [Test]
    public void PickSpacedPosition_SkipsCandidatesNextToAnExistingNode()
    {
        var occupied = new List<Vector3> { new(20, 20, 0) };

        var position = SpawnSpacing.PickSpacedPosition(
            Sequence(new Vector3(22, 20, 0), new Vector3(20, 24, 0), new Vector3(30, 20, 0)),
            occupied);

        position.Should().Be(new Vector3(30, 20, 0));
    }

    [Test]
    public void PickSpacedPosition_AcceptsCandidateExactlyAtMinimumDistance()
    {
        var occupied = new List<Vector3> { new(0, 0, 0) };

        var position = SpawnSpacing.PickSpacedPosition(
            Sequence(new Vector3(SpawnSpacing.MinimumPlaceableDistance, 0, 0)),
            occupied);

        position.Should().Be(new Vector3(SpawnSpacing.MinimumPlaceableDistance, 0, 0));
    }

    [Test]
    public void PickSpacedPosition_IgnoresHeightDifference()
    {
        var occupied = new List<Vector3> { new(20, 20, 0) };

        var position = SpawnSpacing.PickSpacedPosition(
            Sequence(new Vector3(21, 20, 10), new Vector3(40, 20, 0)),
            occupied);

        position.Should().Be(new Vector3(40, 20, 0));
    }

    [Test]
    public void PickSpacedPosition_WhenNoCandidateQualifies_ReturnsMostOpenCandidate()
    {
        var occupied = new List<Vector3> { new(20, 20, 0), new(26, 20, 0) };

        var position = SpawnSpacing.PickSpacedPosition(
            Sequence(new Vector3(21, 20, 0), new Vector3(23, 20, 0), new Vector3(25, 20, 0)),
            occupied,
            maximumAttempts: 3);

        position.Should().Be(new Vector3(23, 20, 0));
    }

    [Test]
    public void PickSpacedPosition_StopsAfterMaximumAttempts()
    {
        var occupied = new List<Vector3> { new(0, 0, 0) };
        var calls = 0;

        SpawnSpacing.PickSpacedPosition(
            () =>
            {
                calls++;
                return new Vector3(1, 0, 0);
            },
            occupied);

        calls.Should().Be(SpawnSpacing.MaximumAttempts);
    }
}
