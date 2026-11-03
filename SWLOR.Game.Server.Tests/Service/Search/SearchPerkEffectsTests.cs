using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.StatService;
using static SWLOR.Game.Server.Tests.Service.Search.SearchTestData;

namespace SWLOR.Game.Server.Tests.Service.Search;

/// <summary>
/// Covers how a scavenger's perk stats become <see cref="SearchEffects"/>, and how the effects change the board's setup: the chance of a
/// Search and the risk of a Dig Deeper and nothing else. The loot-weight bonuses are read by the loot roll, not by the setup.
/// </summary>
public class SearchPerkEffectsTests
{
    private static readonly int[] Ranks = { 0, 9, 10, 20, 30, 40, 50 };

    private static readonly Dictionary<StatType, Func<SearchEffects, int>> WeightFields = new()
    {
        [StatType.SearchDigRareWeight] = effects => effects.DeeperWeightBonus,
        [StatType.SearchFindRareWeight] = effects => effects.FindWeightBonus,
        [StatType.SearchJackpotRareWeight] = effects => effects.JackpotWeightBonus
    };

    private static SearchEffects EffectsOf(StatType stat, int value) => SearchEffects.FromStats(read => read == stat ? value : 0);

    /// <summary>Every number of a setup that a perk could change, and the ones it must not, in one value to compare.</summary>
    private static string Numbers(SearchSetup setup) => string.Join(
        ", ",
        new[]
        {
            setup.Searches, setup.PromisingSpots, setup.OrdinaryChance, setup.PromisingChance, setup.DigRiskBase, setup.DigRiskStep,
            setup.DigFindChance, setup.DigRisk(0), setup.DigRisk(1), setup.DigRisk(2), setup.DigRisk(3), setup.Rank, setup.SiteLevel
        });

    [Test]
    public void NoStatsMeanNoEffects()
    {
        SearchEffects.FromStats(_ => 0).Should().Be(SearchEffects.None);
        SearchEffects.None.Should().Be(new SearchEffects());
        SearchEffects.None.DigRiskPercent.Should().Be(100);
    }

    [Test]
    public void FromStatsNeedsAReader()
    {
        var noReader = () => SearchEffects.FromStats(null);

        noReader.Should().Throw<ArgumentNullException>();
    }

    [TestCase(0, 100)]
    [TestCase(15, 85)]
    [TestCase(50, 50)]
    [TestCase(99, 1)]
    [TestCase(100, 1)]
    [TestCase(250, 1)]
    [TestCase(-10, 100)]
    public void TheDigRiskLeftIsOneHundredLessTheReduction_KeptFromOneToOneHundred(int reduction, int expected)
    {
        EffectsOf(StatType.SearchDigRiskReductionPercent, reduction).DigRiskPercent.Should().Be(expected);
    }

    [TestCase(-3)]
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(40)]
    [TestCase(500)]
    public void TheSearchChanceDeltaIsTheStatAsItIs_ANegativeOneIncluded(int points)
    {
        EffectsOf(StatType.SearchChancePoints, points).SearchChanceDelta.Should().Be(points);
    }

    [TestCase(StatType.SearchDigRareWeight)]
    [TestCase(StatType.SearchFindRareWeight)]
    [TestCase(StatType.SearchJackpotRareWeight)]
    public void TheWeightBonusesAreTheStat_NeverBelowZero(StatType stat)
    {
        var field = WeightFields[stat];

        field(EffectsOf(stat, 4)).Should().Be(4);
        field(EffectsOf(stat, 0)).Should().Be(0);
        field(EffectsOf(stat, -2)).Should().Be(0, "a bonus is never negative");
    }

    [Test]
    public void EachStatSetsItsOwnFieldAndNothingElse()
    {
        EffectsOf(StatType.SearchDigRareWeight, 7).Should().Be(new SearchEffects(DeeperWeightBonus: 7));
        EffectsOf(StatType.SearchDigRiskReductionPercent, 7).Should().Be(new SearchEffects(DigRiskPercent: 93));
        EffectsOf(StatType.SearchChancePoints, 7).Should().Be(new SearchEffects(SearchChanceDelta: 7));
        EffectsOf(StatType.SearchFindRareWeight, 7).Should().Be(new SearchEffects(FindWeightBonus: 7));
        EffectsOf(StatType.SearchJackpotRareWeight, 7).Should().Be(new SearchEffects(JackpotWeightBonus: 7));
    }

    [Test]
    public void AProspectingStatChangesNoSearchEffect_AndNoStatSetsExtraPromisingSpotsOrTheDigFindBonus()
    {
        var prospectingStats = Enum.GetValues<StatType>().Where(candidate => candidate.ToString().StartsWith("Prospecting", StringComparison.Ordinal));
        foreach (var stat in prospectingStats)
            EffectsOf(stat, 7).Should().Be(SearchEffects.None, stat.ToString());

        var everything = SearchEffects.FromStats(_ => 7);
        everything.ExtraPromisingSpots.Should().Be(0);
        everything.DigFindBonus.Should().Be(0);
    }

    [Test]
    public void TheStatsOfSeveralPerksAddUpInTheEffects()
    {
        var stats = new Dictionary<StatType, int>
        {
            [StatType.SearchChancePoints] = SearchPerkRules.KeenEyePoints + SearchPerkRules.CarefulHandsSearchChancePoints,
            [StatType.SearchDigRareWeight] = SearchPerkRules.FineTrowelRank2 + SearchPerkRules.CarefulHandsDigRareWeight,
            [StatType.SearchDigRiskReductionPercent] = SearchPerkRules.SteadyDigRiskReductionPercent,
            [StatType.SearchFindRareWeight] = SearchPerkRules.RichPickingsRareWeight,
            [StatType.SearchJackpotRareWeight] = SearchPerkRules.JackpotRareWeight
        };

        SearchEffects.FromStats(stat => stats.GetValueOrDefault(stat)).Should().Be(new SearchEffects(
            DigRiskPercent: 100 - SearchPerkRules.SteadyDigRiskReductionPercent,
            SearchChanceDelta: SearchPerkRules.KeenEyePoints + SearchPerkRules.CarefulHandsSearchChancePoints,
            DeeperWeightBonus: SearchPerkRules.FineTrowelRank2 + SearchPerkRules.CarefulHandsDigRareWeight,
            FindWeightBonus: SearchPerkRules.RichPickingsRareWeight,
            JackpotWeightBonus: SearchPerkRules.JackpotRareWeight));
    }

    [TestCase(1)]
    [TestCase(3)]
    [TestCase(-3)]
    [TestCase(-7)]
    public void ASearchChanceDelta_MovesBothChances_AtEveryRank(int points)
    {
        foreach (var rank in Ranks)
        {
            var plain = SearchSetup.ForRank(rank, 3);
            var perked = SearchSetup.ForRank(rank, 3, new SearchEffects(SearchChanceDelta: points));

            perked.OrdinaryChance.Should().Be(SearchRules.OrdinaryChance + points, $"rank {rank}");
            perked.PromisingChance.Should().Be(plain.PromisingChance + points, $"rank {rank}");
        }
    }

    [Test]
    public void TheChancesAreKeptToTheLimits_AtBothEnds()
    {
        var headroom = SearchRules.MaximumChance - CraftRankMilestones.PromisingChanceForRank(50);
        var atTheCeiling = SearchSetup.ForRank(50, 3, new SearchEffects(SearchChanceDelta: headroom));
        var overTheCeiling = SearchSetup.ForRank(50, 3, new SearchEffects(SearchChanceDelta: headroom + 1));
        var atTheFloor = SearchSetup.ForRank(0, 3, new SearchEffects(SearchChanceDelta: SearchRules.MinimumChance - SearchRules.OrdinaryChance));
        var underTheFloor = SearchSetup.ForRank(0, 3, new SearchEffects(SearchChanceDelta: SearchRules.MinimumChance - SearchRules.OrdinaryChance - 1));

        atTheCeiling.PromisingChance.Should().Be(SearchRules.MaximumChance);
        overTheCeiling.PromisingChance.Should().Be(SearchRules.MaximumChance, "a Search never becomes certain");
        atTheFloor.OrdinaryChance.Should().Be(SearchRules.MinimumChance);
        underTheFloor.OrdinaryChance.Should().Be(SearchRules.MinimumChance, "a Search always has some chance");
        underTheFloor.PromisingChance.Should().BeGreaterThan(underTheFloor.OrdinaryChance);
    }

    [Test]
    public void KeenEyeAddsItsPointsToBothChances_AndCarefulHandsTakesItsPointsOff()
    {
        var keenEye = SearchSetup.ForRank(20, 3, new SearchEffects(SearchChanceDelta: SearchPerkRules.KeenEyePoints));
        var carefulHands = SearchSetup.ForRank(20, 3, new SearchEffects(SearchChanceDelta: SearchPerkRules.CarefulHandsSearchChancePoints));
        var both = SearchSetup.ForRank(20, 3, new SearchEffects(SearchChanceDelta: SearchPerkRules.KeenEyePoints + SearchPerkRules.CarefulHandsSearchChancePoints));
        var promising = CraftRankMilestones.PromisingChanceForRank(20);

        keenEye.OrdinaryChance.Should().Be(SearchRules.OrdinaryChance + SearchPerkRules.KeenEyePoints);
        keenEye.PromisingChance.Should().Be(promising + SearchPerkRules.KeenEyePoints);
        carefulHands.OrdinaryChance.Should().Be(SearchRules.OrdinaryChance + SearchPerkRules.CarefulHandsSearchChancePoints);
        carefulHands.PromisingChance.Should().Be(promising + SearchPerkRules.CarefulHandsSearchChancePoints);
        both.OrdinaryChance.Should().Be(SearchRules.OrdinaryChance + SearchPerkRules.KeenEyePoints + SearchPerkRules.CarefulHandsSearchChancePoints);
        both.PromisingChance.Should().Be(promising + SearchPerkRules.KeenEyePoints + SearchPerkRules.CarefulHandsSearchChancePoints);
        carefulHands.PromisingChance.Should().BeLessThan(keenEye.PromisingChance, "Careful Hands trades finds for rarer digs");
    }

    [Test]
    public void TheDigRiskIsRoundedHalfUp_AfterTheShareThePerksLeaveIsTaken()
    {
        // Risks of 25, 45, 65 and 85 percent with 85 percent of each left are 21.25, 38.25, 55.25 and 72.25.
        var steady = Setup(riskBase: 25, riskStep: 20, effects: new SearchEffects(DigRiskPercent: 85));

        steady.DigRisk(0).Should().Be(21);
        steady.DigRisk(1).Should().Be(38);
        steady.DigRisk(2).Should().Be(55);
        steady.DigRisk(3).Should().Be(72);

        // Halves round up: 12.5 becomes 13, and 85 percent of 30 (25.5) becomes 26.
        Setup(riskBase: 25, riskStep: 20, effects: new SearchEffects(DigRiskPercent: 50)).DigRisk(0).Should().Be(13);
        Setup(riskBase: 30, riskStep: 20, effects: new SearchEffects(DigRiskPercent: 85)).DigRisk(0).Should().Be(26);
        Setup(riskBase: 25, riskStep: 20, effects: new SearchEffects(DigRiskPercent: 100)).DigRisk(0).Should().Be(25, "the whole risk is left");
    }

    [Test]
    public void TheDigRiskIsKeptToTheMaximumBeforeThePerksShareIsTaken()
    {
        var steady = Setup(riskBase: 80, riskStep: 30, effects: new SearchEffects(DigRiskPercent: 85));

        steady.DigRisk(0).Should().Be((80 * 85 + 50) / 100);
        steady.DigRisk(1).Should().Be((SearchRules.DigRiskMaximum * 85 + 50) / 100, "110 is kept to the maximum first");
        steady.DigRisk(5).Should().Be(steady.DigRisk(1));
    }

    [Test]
    public void AnAbsurdRiskReductionLeavesOnePercentOfTheRisk_WhichMayRoundToNoRiskAtAll()
    {
        var setup = Setup(riskBase: 25, riskStep: 20, effects: SearchEffects.FromStats(read => read == StatType.SearchDigRiskReductionPercent ? 1000 : 0));
        var engine = Engine(setup, 100);
        engine.Perform(SearchAt(Center)).Performed.Should().BeTrue();

        setup.Effects.DigRiskPercent.Should().Be(1);
        setup.DigRisk(0).Should().Be(0);
        engine.Preview(Dig(Center)).Outcomes.Select(outcome => outcome.Label).Should().Equal("Find", "Nothing");
    }

    [Test]
    public void SteadyDigLowersTheRiskOfEveryDig_ByTheShareItsRulesNames()
    {
        var steady = SearchSetup.ForRank(20, 3, EffectsOf(StatType.SearchDigRiskReductionPercent, SearchPerkRules.SteadyDigRiskReductionPercent));
        var plain = SearchSetup.ForRank(20, 3);

        for (var digs = 0; digs <= 3; digs++)
        {
            var expected = (plain.DigRisk(digs) * (100 - SearchPerkRules.SteadyDigRiskReductionPercent) + 50) / 100;

            steady.DigRisk(digs).Should().Be(expected, $"{digs} digs already made");
            steady.DigRisk(digs).Should().BeLessThan(plain.DigRisk(digs));
        }
    }

    [Test]
    public void ADigPreviewShowsThePerkedRisk_AndTheFindChanceOfTheSafeShare()
    {
        var steady = SearchSetup.ForRank(0, 3, new SearchEffects(DigRiskPercent: 85));
        var plain = SearchSetup.ForRank(0, 3);

        // A miss at the center (an ordinary spot of the test layout), then the preview of the first dig there.
        var perkedEngine = EngineWithLayout(steady, Layout, 100);
        var plainEngine = EngineWithLayout(plain, Layout, 100);
        perkedEngine.Perform(SearchAt(Center)).Performed.Should().BeTrue();
        plainEngine.Perform(SearchAt(Center)).Performed.Should().BeTrue();

        foreach (var (setup, engine) in new[] { (steady, perkedEngine), (plain, plainEngine) })
        {
            var risk = setup.DigRisk(0);
            var safe = 100 - risk;
            var find = (safe * setup.DigFindChance + 50) / 100;
            var outcomes = engine.Preview(Dig(Center)).Outcomes;

            outcomes.Select(outcome => outcome.Label).Should().Equal("Find", "Nothing", "Spoils the spot");
            outcomes.Select(outcome => outcome.ChancePercent).Should().Equal(find, safe - find, risk);
        }

        steady.DigRisk(0).Should().BeLessThan(plain.DigRisk(0));
    }

    [Test]
    public void ASearchChanceDelta_ChangesWhereTheRollLandsOnABoard()
    {
        var keenEye = SearchSetup.ForRank(0, 3, new SearchEffects(SearchChanceDelta: 1));
        var plain = SearchSetup.ForRank(0, 3);

        // The center is ordinary ground: a roll just over the plain chance finds only with the point more.
        var roll = SearchRules.OrdinaryChance + 1;
        var perked = EngineWithLayout(keenEye, Layout, roll);
        var unperked = EngineWithLayout(plain, Layout, roll);
        perked.Perform(SearchAt(Center));
        unperked.Perform(SearchAt(Center));

        perked.Last.Result.Should().Be(SearchEventResult.Found);
        unperked.Last.Result.Should().Be(SearchEventResult.Nothing);
    }

    [Test]
    public void TheEffectsDoNotTouchTheRanksSearchesPromisingSpotsOrDigFindChance()
    {
        foreach (var rank in Ranks)
        {
            var plain = SearchSetup.ForRank(rank, 3);

            foreach (var effects in new[]
                     {
                         new SearchEffects(SearchChanceDelta: -3),
                         new SearchEffects(DigRiskPercent: 85),
                         new SearchEffects(DeeperWeightBonus: 5, FindWeightBonus: 2, JackpotWeightBonus: 8),
                         new SearchEffects(DigRiskPercent: 85, SearchChanceDelta: 1, DeeperWeightBonus: 5, FindWeightBonus: 2, JackpotWeightBonus: 8)
                     })
            {
                var perked = SearchSetup.ForRank(rank, 3, effects);

                perked.Searches.Should().Be(plain.Searches, $"rank {rank}");
                perked.PromisingSpots.Should().Be(plain.PromisingSpots, $"rank {rank}");
                perked.DigRiskBase.Should().Be(plain.DigRiskBase);
                perked.DigRiskStep.Should().Be(plain.DigRiskStep);
                perked.DigFindChance.Should().Be(plain.DigFindChance);
                perked.Rank.Should().Be(rank);
                perked.SiteLevel.Should().Be(3);
                perked.Effects.Should().BeSameAs(effects, "the setup keeps the perks, which a dig and the loot roll read");
            }
        }
    }

    [Test]
    public void TheLootWeightBonusesChangeNoNumberOfTheSetup()
    {
        foreach (var rank in Ranks)
        {
            var plain = Numbers(SearchSetup.ForRank(rank, 2));

            Numbers(SearchSetup.ForRank(rank, 2, new SearchEffects(DeeperWeightBonus: 3))).Should().Be(plain);
            Numbers(SearchSetup.ForRank(rank, 2, new SearchEffects(FindWeightBonus: 1))).Should().Be(plain);
            Numbers(SearchSetup.ForRank(rank, 2, new SearchEffects(JackpotWeightBonus: 8))).Should().Be(plain);
        }
    }

    [Test]
    public void ASetupWithNoEffects_IsTodaysSetup_ForEveryRankAndSiteLevel()
    {
        for (var rank = 0; rank <= 50; rank++)
        {
            for (var level = 0; level <= 5; level++)
            {
                var plain = SearchSetup.ForRank(rank, level);

                Numbers(SearchSetup.ForRank(rank, level, SearchEffects.None)).Should().Be(Numbers(plain), $"rank {rank} level {level}");
                Numbers(SearchSetup.ForRank(rank, level, new SearchEffects())).Should().Be(Numbers(plain), $"rank {rank} level {level}");

                plain.Effects.Should().Be(SearchEffects.None);
                plain.OrdinaryChance.Should().Be(SearchRules.OrdinaryChance);
                plain.PromisingChance.Should().Be(CraftRankMilestones.PromisingChanceForRank(rank));
                plain.Searches.Should().Be(CraftRankMilestones.SearchesForRank(rank));
                plain.PromisingSpots.Should().Be(CraftRankMilestones.PromisingSpotsForRank(rank));
                for (var digs = 0; digs <= 4; digs++)
                {
                    plain.DigRisk(digs).Should().Be(
                        Math.Min(SearchRules.DigRiskMaximum, SearchRules.DigRiskBase + SearchRules.DigRiskStep * digs),
                        $"rank {rank}, {digs} digs");
                }
            }
        }
    }
}
