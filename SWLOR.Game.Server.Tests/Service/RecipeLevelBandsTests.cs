using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>Covers the Recipes window's level filter against the labels it shows (0-9, 10-19, ... 40-49, 50+).</summary>
public class RecipeLevelBandsTests
{
    [TestCase(1, 0, true)]
    [TestCase(1, 9, true)]
    [TestCase(1, 10, false)]
    [TestCase(2, 10, true)]
    [TestCase(2, 19, true)]
    [TestCase(2, 20, false)]
    [TestCase(5, 40, true)]
    [TestCase(5, 49, true)]
    [TestCase(5, 50, false)]
    [TestCase(6, 49, false)]
    [TestCase(6, 50, true)]
    [TestCase(6, 70, true)]
    [TestCase(0, 5, false)]
    [TestCase(7, 5, false)]
    public void ABandHoldsTheLevelsItsLabelNames(int band, int level, bool expected)
    {
        RecipeLevelBands.Contains(band, level).Should().Be(expected);
    }
}
