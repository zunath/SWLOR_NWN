using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>Covers the recipe XP scale a minigame's parameters may carry.</summary>
public class CraftXpScaleTests
{
    [Test]
    public void NoParametersAndParametersWithoutAScaleChangeNothing()
    {
        CraftXpScale.Apply(null, 120).Should().Be(120);
        CraftXpScale.Apply(new PlainParameters(), 120).Should().Be(120);
    }

    [TestCase(100, 120, 120)]
    [TestCase(160, 100, 160)]
    [TestCase(150, 33, 50)]
    [TestCase(125, 10, 13)]
    [TestCase(120, 0, 0)]
    public void ThePercentageScalesTheBaseXpAndRoundsHalvesAwayFromZero(int percent, int baseXp, int expected)
    {
        CraftXpScale.Apply(new ScaledParameters(percent), baseXp).Should().Be(expected);
    }

    private sealed class PlainParameters : RecipeMinigameParameters
    {
        public override IReadOnlyList<RecipeParameterValue> Values => Array.Empty<RecipeParameterValue>();
    }

    private sealed class ScaledParameters : RecipeMinigameParameters, ICraftXpScale
    {
        public ScaledParameters(int xpPercent)
        {
            XpPercent = xpPercent;
        }

        public int XpPercent { get; }

        public override IReadOnlyList<RecipeParameterValue> Values => Array.Empty<RecipeParameterValue>();
    }
}
