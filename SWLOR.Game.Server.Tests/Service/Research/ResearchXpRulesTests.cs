using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Research;

namespace SWLOR.Game.Server.Tests.Service.Research;

/// <summary>Covers the Fabrication XP an experiment and a completed job give, from the recipe level against Fabrication rank.</summary>
public class ResearchXpRulesTests
{
    [OneTimeSetUp]
    public void LoadTheXpChart()
    {
        Skill.CacheXPChartData();
    }

    [TestCase(30, 30, 600)]
    [TestCase(30, 28, 750)]
    [TestCase(30, 26, 976)]
    [TestCase(29, 30, 450)]
    [TestCase(40, 30, 1200)]
    [TestCase(10, 14, 76)]
    [TestCase(10, 15, 0)]
    [TestCase(30, 36, 0)]
    public void TheBaseXpIsTheCharts_ForTheRecipeLevelAgainstFabricationRank(int recipeLevel, int rank, int expected)
    {
        ResearchXpRules.BaseXp(recipeLevel, rank).Should().Be(expected);
    }

    [Test]
    public void ResearchBelowTheResearchersRankGivesNothing()
    {
        ResearchXpRules.BaseXp(10, 50).Should().Be(0);
        ResearchXpRules.ExperimentXp(0, 10, 100).Should().Be(0);
        ResearchXpRules.CompletionXp(0, 10).Should().Be(0);
    }

    [TestCase(1, 100, 660)]
    [TestCase(5, 100, 900)]
    [TestCase(10, 100, 1200)]
    [TestCase(10, 50, 600)]
    [TestCase(10, 0, 0)]
    [TestCase(10, 150, 1200)]
    public void AnExperimentPaysTheChartXpWithALevelBonusInProportionToTheTrialsUsed(int level, int effortPercent, int expected)
    {
        ResearchXpRules.ExperimentXp(600, level, effortPercent).Should().Be(expected);
    }

    [TestCase(1, 330)]
    [TestCase(5, 450)]
    [TestCase(10, 600)]
    public void CompletingTheJobPaysHalfTheChartXpWithTheSameLevelBonus(int level, int expected)
    {
        ResearchXpRules.CompletionXp(600, level).Should().Be(expected);
    }

    [Test]
    public void ALevelTenJobAtLevelPaysThreeTimesTheChartXpInTotalAtFullEffort()
    {
        (ResearchXpRules.ExperimentXp(600, 10, 100) + ResearchXpRules.CompletionXp(600, 10)).Should().Be(1800);
    }

    [Test]
    public void TheXpValuesAreNamedStartingValues()
    {
        ResearchXpRules.ExperimentXpPercent.Should().Be(100);
        ResearchXpRules.CompletionXpPercent.Should().Be(50);
        ResearchXpRules.LevelBonusPercentPerLevel.Should().Be(10);
    }

    [Test]
    public void AnExperimentsXpIsOwedOnceItIsSettledAndNeverTwice()
    {
        var job = new ResearchJob
        {
            Experiment = new ResearchExperimentRecord
            {
                Status = ResearchExperimentStatus.Settled,
                TargetLevel = 5,
                TrialsTotal = 6,
                TrialsUsed = 3
            }
        };

        ResearchExperimentLifecycle.PendingExperimentXp(job, 600).Should().Be(450, "half the trials at level 5: 600 x 1.5 x 50%");

        ResearchExperimentLifecycle.MarkExperimentXpGranted(job);

        ResearchExperimentLifecycle.PendingExperimentXp(job, 600).Should().Be(0);
    }

    [Test]
    public void NoXpIsOwedForAnOpenExperimentOrAJobWithNone()
    {
        var open = new ResearchJob { Experiment = new ResearchExperimentRecord { TargetLevel = 2, TrialsTotal = 6, TrialsUsed = 6 } };

        ResearchExperimentLifecycle.PendingExperimentXp(open, 600).Should().Be(0, "it is owed when the experiment settles");
        ResearchExperimentLifecycle.PendingExperimentXp(new ResearchJob(), 600).Should().Be(0);
        ResearchExperimentLifecycle.PendingExperimentXp(null, 600).Should().Be(0);
    }
}
