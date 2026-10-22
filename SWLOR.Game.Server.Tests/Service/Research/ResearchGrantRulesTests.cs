using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Research;

namespace SWLOR.Game.Server.Tests.Service.Research;

/// <summary>
/// Covers what each research level grants, the stored-outcome path of a job that ran an experiment, the fallback when it gathered
/// no evidence, and the legacy path of a job created before experiments, which keeps today's rolls.
/// </summary>
public class ResearchGrantRulesTests
{
    // Rolls for the legacy path, in the order the code draws them: the weighted pick (0 to total weight - 1), the die, then the
    // extra bonus (0 to 999; 999 never grants it).
    private const int NoExtraBonus = 999;

    private static BlueprintDetail BlueprintAtLevel(int level, int enhancementSlots = 0) => new()
    {
        Level = level,
        EnhancementSlots = enhancementSlots
    };

    private static ResearchExperimentRecord Settled(
        int targetLevel,
        ResearchCandidate winner = ResearchCandidate.FixedGrant,
        int magnitude = 0,
        bool extraBonus = false) => new()
    {
        Status = ResearchExperimentStatus.Settled,
        TargetLevel = targetLevel,
        Winner = winner,
        Magnitude = magnitude,
        ExtraBonus = extraBonus
    };

    private static (ResearchCompletion Completion, List<int> Bonuses) Complete(
        BlueprintDetail blueprint,
        ResearchExperimentRecord experiment,
        params int[] rolls)
    {
        var bonuses = new List<int>();
        var completion = ResearchGrantRules.Complete(blueprint, experiment, new ScriptedMinigameRandom(rolls), bonuses.Add);
        return (completion, bonuses);
    }

    [Test]
    public void TheMaximumLevelMatchesTheCraftConstant()
    {
        ResearchGrantRules.MaxLevel.Should().Be(Craft.MaxResearchLevel);
    }

    [TestCase(1, ResearchLevelGrant.ItemBonus, "Item Bonus +1")]
    [TestCase(2, ResearchLevelGrant.LicensedRun, "Licensed Run +1")]
    [TestCase(3, ResearchLevelGrant.RandomBlueprintBonus, "Blueprint Bonus")]
    [TestCase(4, ResearchLevelGrant.LicensedRun, "Licensed Run +1")]
    [TestCase(5, ResearchLevelGrant.RandomBlueprintBonus, "Blueprint Bonus")]
    [TestCase(6, ResearchLevelGrant.ItemBonus, "Item Bonus +1")]
    [TestCase(7, ResearchLevelGrant.GuaranteedTierThreeBonus, "Guaranteed Item Bonus")]
    [TestCase(8, ResearchLevelGrant.RandomBlueprintBonus, "Blueprint Bonus")]
    [TestCase(9, ResearchLevelGrant.ItemBonus, "Item Bonus +1")]
    [TestCase(10, ResearchLevelGrant.EnhancementSlot, "Enhancement Slot +1")]
    public void EachLevelHasItsGrantAndItsWords(int level, ResearchLevelGrant grant, string words)
    {
        ResearchGrantRules.GrantFor(level).Should().Be(grant);
        ResearchGrantRules.Describe(level).Should().Be(words);
    }

    [Test]
    public void ALevelPastTheMaximumReadsAsMax()
    {
        ResearchGrantRules.Describe(11).Should().Be("MAX");
        var act = () => ResearchGrantRules.GrantFor(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void TheFixedLevelsKeepTheirGrantsWhetherOrNotAnExperimentRan()
    {
        foreach (var stored in new[] { false, true })
        {
            foreach (var level in new[] { 1, 2, 4, 6, 7, 9, 10 })
            {
                var blueprint = BlueprintAtLevel(level - 1);
                var experiment = stored ? Settled(level) : null;

                var (completion, bonuses) = Complete(blueprint, experiment, NoExtraBonus);

                blueprint.Level.Should().Be(level);
                completion.Level.Should().Be(level);
                completion.Grant.Should().Be(ResearchGrantRules.GrantFor(level));
                blueprint.ItemBonuses.Should().Be(level is 1 or 6 or 9 ? 1 : 0, $"level {level}, stored {stored}");
                blueprint.LicensedRuns.Should().Be(level is 2 or 4 ? 1 : 0, $"level {level}, stored {stored}");
                blueprint.EnhancementSlots.Should().Be(level == 10 ? 1 : 0, $"level {level}, stored {stored}");
                bonuses.Should().Equal(level == 7 ? new[] { 3 } : Array.Empty<int>(), $"level {level}, stored {stored}");
                blueprint.CreditReduction.Should().Be(0);
                blueprint.TimeReduction.Should().Be(0);
            }
        }
    }

    [Test]
    public void LevelSevenGrantsItsTierThreeBonusBeforeAnExtraBonus()
    {
        var blueprint = BlueprintAtLevel(6);

        var (completion, bonuses) = Complete(blueprint, Settled(7, extraBonus: true));

        bonuses.Should().Equal(3, 1);
        completion.ExtraBonusTier.Should().Be(1);
    }

    [TestCase(ResearchCandidate.LicensedRuns, 3, 3, 0, 0)]
    [TestCase(ResearchCandidate.CreditReduction, 6, 0, 6, 0)]
    [TestCase(ResearchCandidate.TimeReduction, 10, 0, 0, 10)]
    public void ARandomLevelGrantsExactlyTheStoredWinnerAndMagnitudeWithoutRolling(
        ResearchCandidate winner, int magnitude, int runs, int credit, int time)
    {
        foreach (var level in new[] { 3, 5, 8 })
        {
            var blueprint = BlueprintAtLevel(level - 1, enhancementSlots: 1);
            blueprint.LicensedRuns = 4;
            blueprint.CreditReduction = 2;
            blueprint.TimeReduction = 1;

            // The random source has no rolls scripted, so any roll would fail the test.
            var (completion, bonuses) = Complete(blueprint, Settled(level, winner, magnitude));

            blueprint.LicensedRuns.Should().Be(4 + runs, $"level {level}");
            blueprint.CreditReduction.Should().Be(2 + credit, $"level {level}");
            blueprint.TimeReduction.Should().Be(1 + time, $"level {level}");
            completion.Candidate.Should().Be(winner);
            completion.Amount.Should().Be(magnitude);
            completion.FromExperiment.Should().BeTrue();
            bonuses.Should().BeEmpty();
        }
    }

    [Test]
    public void ARandomLevelWithNoEvidenceFallsBackToTodaysWeightedRollAndDice()
    {
        var blueprint = BlueprintAtLevel(2);

        // A weighted roll of 300 is credit reduction, and the die rolls 7.
        var (completion, _) = Complete(blueprint, Settled(3, ResearchCandidate.None, extraBonus: false), 300, 7);

        blueprint.CreditReduction.Should().Be(7);
        completion.Candidate.Should().Be(ResearchCandidate.CreditReduction);
        completion.Amount.Should().Be(7);
        completion.FromExperiment.Should().BeFalse("today's roll decided the bonus");
    }

    [Test]
    public void TheFallbackStillGrantsTheExtraBonusTheExperimentRolledAtTheStart()
    {
        var blueprint = BlueprintAtLevel(4);

        var (completion, bonuses) = Complete(blueprint, Settled(5, ResearchCandidate.None, extraBonus: true), 0, 2);

        blueprint.LicensedRuns.Should().Be(2);
        bonuses.Should().Equal(1);
        completion.ExtraBonusTier.Should().Be(1);
    }

    [Test]
    public void TheStoredExtraBonusIsGrantedOnCompletionAtThatLevelsTier()
    {
        foreach (var (level, tier) in new[] { (1, 1), (4, 1), (8, 1), (9, 2), (10, 3) })
        {
            var blueprint = BlueprintAtLevel(level - 1);
            var experiment = Settled(level, ResearchGrantRules.IsRandomLevel(level) ? ResearchCandidate.TimeReduction : ResearchCandidate.FixedGrant, 2, true);

            var (completion, bonuses) = Complete(blueprint, experiment);

            bonuses.Should().Equal(new[] { tier }, $"level {level}");
            completion.ExtraBonusTier.Should().Be(tier);
        }
    }

    [Test]
    public void AStoredExperimentWithoutAnExtraBonusNeverRollsOne()
    {
        var blueprint = BlueprintAtLevel(9);

        var (completion, bonuses) = Complete(blueprint, Settled(10, extraBonus: false));

        bonuses.Should().BeEmpty();
        completion.ExtraBonusTier.Should().Be(0);
    }

    [Test]
    public void AnOpenExperimentIsNotAStoredOutcome_SoTheJobFallsBackToTodaysRoll()
    {
        var blueprint = BlueprintAtLevel(2);
        var open = new ResearchExperimentRecord { Status = ResearchExperimentStatus.InProgress, TargetLevel = 3 };

        var (completion, _) = Complete(blueprint, open, 0, 1, NoExtraBonus);

        completion.FromExperiment.Should().BeFalse();
        blueprint.LicensedRuns.Should().Be(1);
    }

    [Test]
    public void ABlueprintAlreadyAtTheMaximumStaysThere()
    {
        var blueprint = BlueprintAtLevel(10);

        Complete(blueprint, null, NoExtraBonus);

        blueprint.Level.Should().Be(10);
    }

    [TestCase(0, ResearchCandidate.LicensedRuns, 1)]
    [TestCase(299, ResearchCandidate.LicensedRuns, 3)]
    [TestCase(300, ResearchCandidate.CreditReduction, 1)]
    [TestCase(699, ResearchCandidate.CreditReduction, 10)]
    [TestCase(700, ResearchCandidate.TimeReduction, 4)]
    [TestCase(1099, ResearchCandidate.TimeReduction, 10)]
    public void TheLegacyPathPicksByTheWeights300To400To400(int weightedRoll, ResearchCandidate expected, int die)
    {
        var blueprint = BlueprintAtLevel(2);

        var (completion, _) = Complete(blueprint, null, weightedRoll, die, NoExtraBonus);

        completion.Candidate.Should().Be(expected);
        completion.Amount.Should().Be(die);
        completion.FromExperiment.Should().BeFalse();
        blueprint.LicensedRuns.Should().Be(expected == ResearchCandidate.LicensedRuns ? die : 0);
        blueprint.CreditReduction.Should().Be(expected == ResearchCandidate.CreditReduction ? die : 0);
        blueprint.TimeReduction.Should().Be(expected == ResearchCandidate.TimeReduction ? die : 0);
    }

    [TestCase(309, ResearchCandidate.LicensedRuns)]
    [TestCase(310, ResearchCandidate.CreditReduction)]
    [TestCase(729, ResearchCandidate.CreditReduction)]
    [TestCase(730, ResearchCandidate.TimeReduction)]
    public void TheLegacyPathUsesTheShiftedWeightsForABlueprintWithAnEnhancementSlot(int weightedRoll, ResearchCandidate expected)
    {
        var blueprint = BlueprintAtLevel(2, enhancementSlots: 1);

        var (completion, _) = Complete(blueprint, null, weightedRoll, 1, NoExtraBonus);

        completion.Candidate.Should().Be(expected);
    }

    [TestCase(1, 1, true)]
    [TestCase(1, 2, false)]
    [TestCase(2, 2, true)]
    [TestCase(2, 3, false)]
    [TestCase(3, 3, true)]
    [TestCase(4, 10, true)]
    [TestCase(4, 11, false)]
    [TestCase(5, 15, true)]
    [TestCase(6, 20, true)]
    [TestCase(7, 50, true)]
    [TestCase(7, 51, false)]
    [TestCase(8, 80, true)]
    [TestCase(9, 100, true)]
    [TestCase(9, 101, false)]
    [TestCase(10, 120, true)]
    [TestCase(10, 121, false)]
    public void TheLegacyPathGrantsTheExtraBonusOnTodaysRollForTheLevel(int level, int extraRoll, bool granted)
    {
        var blueprint = BlueprintAtLevel(level - 1);
        // A random level draws its weighted pick and die first; the rest draw nothing before the extra roll.
        var rolls = ResearchGrantRules.IsRandomLevel(level) ? new[] { 0, 1, extraRoll } : new[] { extraRoll };

        var (completion, bonuses) = Complete(blueprint, null, rolls);

        var expectedBonuses = new List<int>();
        if (level == 7)
            expectedBonuses.Add(3);
        if (granted)
            expectedBonuses.Add(ResearchGrantRules.ExtraBonusTier(level));
        bonuses.Should().Equal(expectedBonuses, $"level {level}, roll {extraRoll}");
        completion.ExtraBonusTier.Should().Be(granted ? ResearchGrantRules.ExtraBonusTier(level) : 0);
    }

    [TestCase(1, 0.2)]
    [TestCase(2, 0.3)]
    [TestCase(3, 0.4)]
    [TestCase(4, 1.1)]
    [TestCase(5, 1.6)]
    [TestCase(6, 2.1)]
    [TestCase(7, 5.1)]
    [TestCase(8, 8.1)]
    [TestCase(9, 10.1)]
    [TestCase(10, 12.1)]
    public void TheExtraBonusChanceRisesFromPointTwoToTwelvePointOnePercent(int level, double percent)
    {
        ResearchGrantRules.ExtraBonusChancePercent(level).Should().BeApproximately(percent, 1e-9);
    }

    [Test]
    public void RollExtraBonusUsesTodaysChanceForTheLevelOnASeededSource()
    {
        const int Draws = 200_000;
        foreach (var level in new[] { 1, 4, 7, 10 })
        {
            var random = new SeededMinigameRandom(level);
            var granted = Enumerable.Range(0, Draws).Count(_ => ResearchGrantRules.RollExtraBonus(level, random));

            (granted * 100.0 / Draws).Should().BeApproximately(ResearchGrantRules.ExtraBonusChancePercent(level), 0.35, $"level {level}");
        }
    }

    [Test]
    public void TheLegacyRandomBonusKeepsTodaysOddsAndDice()
    {
        const int Draws = 200_000;
        var random = new SeededMinigameRandom(7);
        var counts = new Dictionary<ResearchCandidate, int>();
        var sums = new Dictionary<ResearchCandidate, long>();
        for (var draw = 0; draw < Draws; draw++)
        {
            var (candidate, amount) = ResearchGrantRules.RollRandomBonus(BlueprintAtLevel(2), random);
            counts[candidate] = counts.GetValueOrDefault(candidate) + 1;
            sums[candidate] = sums.GetValueOrDefault(candidate) + amount;
            amount.Should().BeInRange(1, candidate == ResearchCandidate.LicensedRuns ? 3 : 10);
        }

        (counts[ResearchCandidate.LicensedRuns] / (double)Draws).Should().BeApproximately(300 / 1100.0, 0.01);
        (counts[ResearchCandidate.CreditReduction] / (double)Draws).Should().BeApproximately(400 / 1100.0, 0.01);
        (counts[ResearchCandidate.TimeReduction] / (double)Draws).Should().BeApproximately(400 / 1100.0, 0.01);
        (sums[ResearchCandidate.LicensedRuns] / (double)counts[ResearchCandidate.LicensedRuns]).Should().BeApproximately(2.0, 0.03);
        (sums[ResearchCandidate.CreditReduction] / (double)counts[ResearchCandidate.CreditReduction]).Should().BeApproximately(5.5, 0.05);
        (sums[ResearchCandidate.TimeReduction] / (double)counts[ResearchCandidate.TimeReduction]).Should().BeApproximately(5.5, 0.05);
    }

    [Test]
    public void TodaysWeightSharesAreTheOddsTheSupplyReportCompares()
    {
        ResearchGrantRules.TodayWeightShare(ResearchCandidate.LicensedRuns, false).Should().BeApproximately(300 / 1100.0, 1e-12);
        ResearchGrantRules.TodayWeightShare(ResearchCandidate.TimeReduction, true).Should().BeApproximately(420 / 1150.0, 1e-12);
        ResearchGrantRules.AverageDice(ResearchCandidate.LicensedRuns).Should().Be(2.0);
        ResearchGrantRules.AverageDice(ResearchCandidate.CreditReduction).Should().Be(5.5);
    }

    [Test]
    public void TheViewModelGrantsThroughTheRulesAndKeepsTheNewBlueprintLicensedRuns()
    {
        var source = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "ResearchViewModel.cs"));

        source.Should().Contain("ResearchGrantRules.Complete(");
        source.Should().Contain("var licensedRunBonus = CraftRankMilestones.GetBlueprintLicensedRunBonus(Player);");
        source.Should().NotContain("Random.D10(", "the dice live in the rules, where the legacy path is tested");
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull();
        return directory!.FullName;
    }
}
