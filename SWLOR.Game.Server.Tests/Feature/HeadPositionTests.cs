using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition;

namespace SWLOR.Game.Server.Tests.Feature;

public class HeadPositionTests
{
    [TestCase(0.101f, 0.1f)]
    [TestCase(-0.101f, -0.1f)]
    [TestCase(0.083f, 0.08f)]
    [TestCase(0.5f, 0.1f)]
    [TestCase(-0.5f, -0.1f)]
    [TestCase(-0.004f, 0f)]
    [TestCase(float.NaN, 0f)]
    [TestCase(float.PositiveInfinity, 0f)]
    [TestCase(float.NegativeInfinity, 0f)]
    public void StoredOrEditedValuesRemainFiniteAndWithinRange(float input, float expected)
    {
        HeadPosition.Normalize(input).Should().Be(expected);
    }
}
