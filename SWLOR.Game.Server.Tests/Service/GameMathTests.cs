using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class GameMathTests
{
    [Test]
    public void PercentOf_RoundsPartialPercentagesUp()
    {
        GameMath.PercentOf(101, 10).Should().Be(11);
    }

    [Test]
    public void PercentOf_ClampsPositiveCalculationsToAtLeastOne()
    {
        GameMath.PercentOf(1, 1).Should().Be(1);
    }
}
