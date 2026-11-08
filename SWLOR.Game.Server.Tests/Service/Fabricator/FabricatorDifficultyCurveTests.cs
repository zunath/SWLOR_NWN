using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;
using static SWLOR.Game.Server.Tests.Service.Fabricator.FabricatorTestData;

namespace SWLOR.Game.Server.Tests.Service.Fabricator;

/// <summary>Recipes above the curve's start level wear the machine faster and add less per Precision Pass, so gear decides how well the top recipes turn out.</summary>
[TestFixture]
public class FabricatorDifficultyCurveTests
{
    private static readonly int Start = FabricatorRules.DifficultyStartLevel;

    [TestCase(1)]
    [TestCase(25)]
    [TestCase(30)]
    public void RecipesUpToTheStartLevelKeepTheirCosts(int level)
    {
        FabricatorRules.QualityDifficulty(level).Should().Be(1.0);
        FabricatorRules.WearDifficulty(level).Should().Be(1.0);
        FabricatorRules.ServiceCp(level).Should().Be(FabricatorRules.ServiceCpBase + level / FabricatorRules.ServiceCpLevelDivisor);
    }

    [Test]
    public void TheTopRecipesWearTheMachineFaster()
    {
        Setup(level: 52, rank: 52).WearRate.Should().BeGreaterThan(Setup(level: Start, rank: Start).WearRate);
    }

    [Test]
    public void AServiceCostsMoreOnTheTopRecipesThanTheOldLevelStep()
    {
        FabricatorRules.ServiceCp(52).Should().BeGreaterThan(FabricatorRules.ServiceCpBase + 52 / FabricatorRules.ServiceCpLevelDivisor);
    }

    [Test]
    public void APrecisionPassAddsLessOnTheTopRecipes()
    {
        Setup(level: 52, rank: 52).PrecisionGain(primed: false).Should().BeLessThan(Setup(level: Start, rank: Start).PrecisionGain(primed: false));
    }

    [Test]
    public void ARunDoesNotWeaken_SoAPieceStillGetsBuilt()
    {
        Setup(level: 52, rank: 52).RunGain(onHead: true, primed: false).Should().Be(Setup(level: Start, rank: Start).RunGain(onHead: true, primed: false));
    }
}
