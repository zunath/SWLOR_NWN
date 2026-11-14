using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Crafting.Minigame.Forge;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>Recipes above level 25 need more Shape and Finish, and a Shape-heavy recipe's small Finish meter ramps up to the floor.</summary>
[TestFixture]
public class ForgeDifficultyCurveTests
{
    [TestCase(1)]
    [TestCase(25)]
    public void RecipesUpToLevel25KeepTheirMeters(int level)
    {
        ForgeRules.ShapeMax(70, 0, level).Should().Be(1400);
        ForgeRules.FinishMax(70, level).Should().Be(600);
        ForgeRules.FinishMax(30, level).Should().Be(1400);
    }

    [TestCase(26)]
    [TestCase(35)]
    [TestCase(50)]
    [TestCase(53)]
    public void HigherRecipesNeedMoreOfBoth(int level)
    {
        ForgeRules.ShapeMax(50, 0, level).Should().BeGreaterThan(ForgeRules.ShapeMax(50, 0, level - 1));
        ForgeRules.FinishMax(50, level).Should().BeGreaterThan(ForgeRules.FinishMax(50, level - 1));
    }

    [Test]
    public void ShapeGrowsFarSlowerThanFinish()
    {
        (ForgeRules.ShapeDifficulty(52) - 1).Should().BeLessThan((ForgeRules.FinishDifficulty(52) - 1) / 2);
    }

    [Test]
    public void TheFinishFloorArrivesInFullAtLevel50()
    {
        var factor = ForgeRules.FinishDifficulty(50);
        ForgeRules.FinishMax(70, 50).Should().Be((int)Math.Round(ForgeRules.FinishFloor * factor, MidpointRounding.AwayFromZero));
        ForgeRules.FinishMax(70, 35).Should().BeLessThan(ForgeRules.FinishMax(70, 50));
        ForgeRules.FinishMax(70, 35).Should().BeGreaterThan(600);
    }

    [Test]
    public void ARecipeAlreadyAboveTheFloorOnlyTakesTheLevelFactor()
    {
        ForgeRules.FinishMax(30, 50).Should().Be((int)Math.Round(1400 * ForgeRules.FinishDifficulty(50), MidpointRounding.AwayFromZero));
    }

    [Test]
    public void EnhancementsStillRaiseTheShapeARecipeNeeds()
    {
        ForgeRules.ShapeMax(70, 20, 52).Should().BeGreaterThan(ForgeRules.ShapeMax(70, 0, 52));
    }
}
