using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using static SWLOR.Game.Server.Tests.Service.Search.SearchTestData;

namespace SWLOR.Game.Server.Tests.Service.Search;

/// <summary>Covers how Gathering rank and perks set the board, the rank schedule that replaced Hard Look and Perception, and the XP of an attempt.</summary>
public class SearchSetupTests
{
    [TestCase(0)]
    [TestCase(9)]
    [TestCase(10)]
    [TestCase(25)]
    [TestCase(40)]
    [TestCase(50)]
    public void TheRankSetsTheSearchesThePromisingSpotsAndThePromisingChance_AndNothingElseIsRead(int rank)
    {
        var setup = SearchSetup.ForRank(rank, 3);

        setup.Searches.Should().Be(CraftRankMilestones.SearchesForRank(rank));
        setup.PromisingSpots.Should().Be(CraftRankMilestones.PromisingSpotsForRank(rank));
        setup.OrdinaryChance.Should().Be(SearchRules.OrdinaryChance);
        setup.PromisingChance.Should().Be(CraftRankMilestones.PromisingChanceForRank(rank));
        setup.DigRiskBase.Should().Be(SearchRules.DigRiskBase);
        setup.DigFindChance.Should().Be(SearchRules.DigFindChance);
        setup.Rank.Should().Be(rank);
        setup.SiteLevel.Should().Be(3);
    }

    [Test]
    public void TheSearchesPromisingSpotsAndPromisingChanceNeverFallAsTheRankRises_AndStayInsideTheGatheringLengthTarget()
    {
        var searches = 0;
        var promising = 0;
        var chance = 0;
        for (var rank = 0; rank <= 50; rank++)
        {
            var setup = SearchSetup.ForRank(rank, 1);

            setup.Searches.Should().BeGreaterThanOrEqualTo(searches, $"rank {rank}");
            setup.PromisingSpots.Should().BeGreaterThanOrEqualTo(promising, $"rank {rank}");
            setup.PromisingChance.Should().BeGreaterThanOrEqualTo(chance, $"rank {rank}");
            setup.Searches.Should().BeInRange(3, 8, "a gathering node is 3 to 8 actions");
            setup.PromisingSpots.Should().BeInRange(1, SearchBoard.Spots - 1);
            setup.PromisingChance.Should().BeGreaterThan(setup.OrdinaryChance, "promising ground finds more often than ordinary ground");
            searches = setup.Searches;
            promising = setup.PromisingSpots;
            chance = setup.PromisingChance;
        }
    }

    [Test]
    public void TheRankImprovesTheBoardThroughThePromisingChance_StepByStepAtTheRanksHardLookSteppedAt()
    {
        foreach (var rank in new[] { 0, 9, 10, 29, 30, 49, 50 })
            CraftRankMilestones.HardLookLevelForRank(rank).Should().Be(CraftRankMilestones.TierForRank(rank));

        CraftRankMilestones.SearchSchedule().Select(step => step.Rank).Should().Equal(new[] { 0, 10, 20, 30, 40 }, "the promising chance steps at the ranks Hard Look did");
        CraftRankMilestones.SearchSchedule().Select(step => step.PromisingChance).Should().BeInAscendingOrder().And.OnlyHaveUniqueItems("every tier is a better board");
        CraftRankMilestones.PromisingChanceForRank(0).Should().Be(29);
        CraftRankMilestones.PromisingChanceForRank(50).Should().Be(43);
        CraftRankMilestones.PromisingChanceForRank(9).Should().Be(CraftRankMilestones.PromisingChanceForRank(0), "it steps at the tier, not between");
    }

    [Test]
    public void ExtraPromisingSpots_AddSpots_UpToTheWholeBoard()
    {
        var plain = SearchSetup.ForRank(20, 2);

        SearchSetup.ForRank(20, 2, new SearchEffects(ExtraPromisingSpots: 1)).PromisingSpots.Should().Be(plain.PromisingSpots + 1);
        SearchSetup.ForRank(20, 2, new SearchEffects(ExtraPromisingSpots: 20)).PromisingSpots.Should().Be(SearchBoard.Spots);
    }

    [Test]
    public void ASearchChanceDelta_MovesBothChances_AndKeepsThemInsideTheLimits()
    {
        var careful = SearchSetup.ForRank(20, 2, new SearchEffects(SearchChanceDelta: -5));
        var absurd = SearchSetup.ForRank(20, 2, new SearchEffects(SearchChanceDelta: 500));
        var ruinous = SearchSetup.ForRank(20, 2, new SearchEffects(SearchChanceDelta: -500));

        careful.OrdinaryChance.Should().Be(SearchRules.OrdinaryChance - 5);
        careful.PromisingChance.Should().Be(CraftRankMilestones.PromisingChanceForRank(20) - 5);
        absurd.PromisingChance.Should().Be(SearchRules.MaximumChance);
        ruinous.OrdinaryChance.Should().Be(SearchRules.MinimumChance);
    }

    [Test]
    public void NoPerksChangeTheNumbers()
    {
        var plain = SearchSetup.ForRank(35, 4);
        var none = SearchSetup.ForRank(35, 4, SearchEffects.None);

        none.Searches.Should().Be(plain.Searches);
        none.PromisingSpots.Should().Be(plain.PromisingSpots);
        none.DigRisk(0).Should().Be(plain.DigRisk(0)).And.Be(SearchRules.DigRiskBase);
        none.DigRisk(2).Should().Be(SearchRules.DigRiskBase + 2 * SearchRules.DigRiskStep);
    }

    [Test]
    public void TheDigRiskIsKeptToTheMaximum()
    {
        var setup = Setup(riskBase: 80, riskStep: 30);

        setup.DigRisk(0).Should().Be(80);
        setup.DigRisk(1).Should().Be(SearchRules.DigRiskMaximum);
        setup.DigRisk(2).Should().Be(SearchRules.DigRiskMaximum);
    }

    [Test]
    public void ASetupRejectsNumbersThatCannotBeABoard()
    {
        Action noSearches = () => Setup(searches: 0);
        Action tooManySpots = () => Setup(promising: 10);
        Action negativeSpots = () => Setup(promising: -1);
        Action badChance = () => Setup(ordinary: 101);
        Action badRisk = () => Setup(riskBase: -1);
        Action badFind = () => Setup(findChance: 101);
        Action badReduction = () => Setup(effects: new SearchEffects(DigRiskPercent: 0));

        foreach (var action in new[] { noSearches, tooManySpots, negativeSpots, badChance, badRisk, badFind, badReduction })
            action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void AnAttemptPaysWhatTheSitePaysTimesTheMultiplier_AQuarterOnAFailure()
    {
        var multiplier = SearchRules.XpMultiplierForLevel(3);

        SearchRules.AttemptXp(600, 3, true).Should().Be(CraftMinigameXp.ScaleBaseXp(600, multiplier));
        SearchRules.AttemptXp(600, 3, false).Should().Be(CraftMinigameXp.ScaleBaseXp(150, multiplier), "a failure pays a quarter of the site's XP, as scavenging always did");
        SearchRules.AttemptXp(0, 3, true).Should().Be(0);
        SearchRules.AttemptXp(1050, 5, false).Should().Be(CraftMinigameXp.ScaleBaseXp(1050 / 4, SearchRules.XpMultiplierForLevel(5)));
    }

    [Test]
    public void TheSharedXpMechanismHasAMultiplierForEverySiteLevel_AndALevelZeroSiteTakesLevelOnes()
    {
        foreach (var level in Enumerable.Range(1, 5))
            CraftMinigameXp.MultiplierFor(CraftMinigameType.Search, level).Should().Be(SearchRules.XpMultiplierForLevel(level)).And.BeGreaterThanOrEqualTo(1.0);

        CraftMinigameXp.MultiplierFor(CraftMinigameType.Search, 0).Should().Be(SearchRules.XpMultiplierForLevel(1));
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Search, 9).Should().Be(SearchRules.XpMultiplierForLevel(5));
    }

    [Test]
    public void TheBoardReadsNoAttribute()
    {
        var root = TestRepository.FindRoot();
        foreach (var path in new[]
                 {
                     "SWLOR.Game.Server/Service/CraftService/Minigame/Search",
                     "SWLOR.Game.Server/Service/ScavengeService"
                 })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(root, path), "*.cs"))
            {
                var source = File.ReadAllText(file);
                source.Should().NotContain("GetAbilityModifier", Path.GetFileName(file));
                source.Should().NotContain("AbilityType.", Path.GetFileName(file));
            }
        }
    }
}

internal static class TestRepository
{
    /// <summary>The repository root, found by walking up from the test assembly to the folder that holds the solution.</summary>
    public static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server", "SWLOR.Game.Server.csproj")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("The repository root was not found above the test assembly.");
    }
}
