using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using static SWLOR.Game.Server.Tests.Service.Circuit.CircuitTestData;

namespace SWLOR.Game.Server.Tests.Service.Circuit;

/// <summary>Recipes above the curve's start level cost more to wire and tune, so gear decides how well the top recipes turn out.</summary>
[TestFixture]
public class CircuitDifficultyCurveTests
{
    private static readonly int Start = CircuitRules.DifficultyStartLevel;

    [TestCase(1)]
    [TestCase(25)]
    [TestCase(30)]
    public void RecipesUpToTheStartLevelKeepTheirCosts(int level)
    {
        CircuitRules.CalibrationDifficulty(level).Should().Be(1.0);
        CircuitRules.IntegrityDifficulty(level).Should().Be(1.0);
        CircuitRules.PatchCpAt(level).Should().Be(CircuitRules.PatchCp);
        CircuitRules.TuneCpAt(level).Should().Be(CircuitRules.TuneCp);
        CircuitRules.MicroTuneCpAt(level).Should().Be(CircuitRules.MicroTuneCp);
    }

    [Test]
    public void TheTopRecipesCostMoreOfEveryBudget()
    {
        CircuitRules.CalibrationDifficulty(52).Should().BeGreaterThan(CircuitRules.CalibrationDifficulty(Start + 1));
        CircuitRules.IntegrityDifficulty(52).Should().BeGreaterThan(CircuitRules.IntegrityDifficulty(Start + 1));
        CircuitRules.PatchCpAt(52).Should().BeGreaterThan(CircuitRules.PatchCp);
        CircuitRules.TuneCpAt(52).Should().BeGreaterThan(CircuitRules.TuneCp);
        CircuitRules.MicroTuneCpAt(52).Should().BeGreaterThan(CircuitRules.MicroTuneCp);
    }

    [Test]
    public void TheSetupChargesTheLevelsCosts()
    {
        var top = Setup(level: 52);

        top.TuneCp.Should().Be(CircuitRules.TuneCpAt(52));
        top.MicroTuneCp.Should().Be(CircuitRules.MicroTuneCpAt(52));
        top.PatchCp.Should().Be(CircuitRules.PatchCpAt(52));
        top.WireIntegrity.Should().BeGreaterThan(Setup(level: Start).WireIntegrity);
    }

    [Test]
    public void CalibrationComesSlowerOnTheTopRecipes()
    {
        Setup(level: 52, rank: 52).TuneGain(0).Should().BeLessThan(Setup(level: Start, rank: Start).TuneGain(0));
    }

    [Test]
    public void AssemblyDoesNotSlow_SoAPieceStillGetsBuilt()
    {
        Setup(level: 52, rank: 52).WireGain(0).Should().Be(Setup(level: Start, rank: Start).WireGain(0));
    }
}
