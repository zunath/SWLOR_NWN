using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Native;

namespace SWLOR.Game.Server.Tests.Service;

public class CannonMuzzleAnchorsTests
{
    [TestCase((byte)1, true)]
    [TestCase((byte)3, true)]
    [TestCase((byte)7, true)]
    [TestCase((byte)10, true)]
    [TestCase((byte)2, false)]
    [TestCase((byte)4, false)]
    [TestCase((byte)8, false)]
    [TestCase((byte)9, false)]
    public void HitsAreDrawnOnTheTargetAndMissesBesideIt(byte result, bool hit) =>
        CannonMuzzleAnchors.IsHitResult(result).Should().Be(hit);

    [Test]
    public void TheMuzzleSitsAheadOfTheHandsWhereNativeBoltsStart()
    {
        // Native bolts start about 0.74 m ahead and 1.42 m up; the barrel end is further out and higher.
        CannonMuzzleAnchors.MuzzleOffset.Y.Should().BeGreaterThan(.74f);
        CannonMuzzleAnchors.MuzzleOffset.Z.Should().BeGreaterThan(1.42f);
    }

    [Test]
    public void TheMuzzleTurnsWithTheActorAndScalesWithItsSize()
    {
        var offset = new Vector3(0f, 1f, 2f);
        PairedRangedPresentation.ResolveMuzzle(Vector3.Zero, 0f, offset, 1f)
            .Should().Match<Vector3>(v => Near(v, new Vector3(1f, 0f, 2f)));
        PairedRangedPresentation.ResolveMuzzle(new Vector3(10f, 10f, 0f), 90f, offset, 2f)
            .Should().Match<Vector3>(v => Near(v, new Vector3(10f, 12f, 4f)));
    }

    private static bool Near(Vector3 actual, Vector3 expected) => Vector3.Distance(actual, expected) < .001f;
}
