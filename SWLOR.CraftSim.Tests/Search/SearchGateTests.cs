using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Search;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;

namespace SWLOR.CraftSim.Tests.Search
{
    /// <summary>
    /// The scavenging gates over a small study: the economy-parity (yield) gate, the length gate, and the XP-rate gate over the scavenging
    /// cohort. The small study plays a few thousand boards, so the limits carry their sampling tolerance.
    /// </summary>
    [TestFixture]
    public class SearchGateTests
    {
        private const int Runs = 2500;
        private const int Seed = 7;

        private static readonly SearchParityOptions Options = SearchParityOptions.Default with
        {
            Ranks = new[] { 0, 20, 50 },
            QuickSearchRuns = 40_000,
            Tolerance = 0.04
        };

        private static readonly GateContext Context = new(null, null, Runs, Seed, AttributeSweep.Default);

        private static SearchParity Parity() => new(Runs, Seed);

        [Test]
        public void TheYieldGatePasses_QuickSearchEqualsTodayAndTheBoardStaysInsideTheCap()
        {
            var result = new SearchYieldGate(Parity(), Options).Evaluate(Context);

            result.Gate.Should().Be(GateNames.EconomyParity);
            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            result.Tables.Select(table => table.Title).Should().Contain(title => title.StartsWith("Quick Search against today"))
                .And.Contain(title => title.StartsWith("Finds per board"))
                .And.Contain(title => title.StartsWith("The whole Treasure Seeker line against the cap"));
        }

        [Test]
        public void TheYieldGateFails_WhenTheSkilledBoardBeatsQuickSearchWithoutPerks()
        {
            var strict = Options with { UnperkedSkilledMaximumRatio = 0.5 };

            var result = new SearchYieldGate(Parity(), strict).Evaluate(Context);

            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Summary.Should().Contain("Yield falls outside its limits");
        }

        [Test]
        public void TheYieldGateFails_WhenTheWholeLineTakesASkilledPlayerPastThePlansCap()
        {
            var greedy = Options with
            {
                TreasureSeekerLine = new SearchEffects(ExtraPromisingSpots: 3, DigRiskPercent: 10, DigFindBonus: 40)
            };

            var result = new SearchYieldGate(Parity(), greedy).Evaluate(Context);

            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Summary.Should().Contain("Treasure Seeker line");
        }

        [Test]
        public void TheYieldGateFails_WhenTheCarelessPlayerFallsBelowTheFloor()
        {
            var demanding = Options with { CarelessMinimumRatio = 0.95 };

            new SearchYieldGate(Parity(), demanding).Evaluate(Context).Verdict.Should().Be(GateVerdict.Failed);
        }

        [Test]
        public void TheTreasureSeekerLineLiftsASkilledScavengerUpToTheCapAndNoFurther()
        {
            var parity = Parity();
            foreach (var rank in Options.Ranks)
            {
                var quick = ScavengeQuickRolls(rank);
                var plain = parity.Sample(rank, 1, SearchPlayerKind.Skilled).Finds / quick;
                var line = parity.Sample(rank, 1, SearchPlayerKind.Skilled, Options.TreasureSeekerLine).Finds / quick;

                plain.Should().BeLessThanOrEqualTo(Options.UnperkedSkilledMaximumRatio + Options.Tolerance, $"rank {rank}");
                line.Should().BeGreaterThan(plain, $"rank {rank}: the line helps");
                line.Should().BeLessThanOrEqualTo(Options.SkilledMaximumRatio + Options.Tolerance, $"rank {rank}");
            }
        }

        private static double ScavengeQuickRolls(int rank) =>
            Game.Server.Service.ScavengeService.ScavengeQuickSearch.ExpectedRolls(CraftRankMilestones.HardLookLevelForRank(rank));

        [Test]
        public void TheLengthGatePasses_EveryBoardIsAsLongAsTheScavengerHasSearches()
        {
            var result = new SearchLengthGate(Parity(), Options).Evaluate(Context);

            result.Gate.Should().Be(GateNames.Length);
            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            result.Tables.Single().Rows.Should().HaveCount(Options.Ranks.Count);
        }

        [Test]
        public void TheXpRateGatePasses_AtTheMultipliersTheGameApplies()
        {
            var cohort = new SearchXpRateCohort(Parity(), Options with { SiteLevels = new[] { 1, 3, 5 } });

            var result = new XpRateGate(cohort).Evaluate(Context);

            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
        }

        [Test]
        public void TheXpRateGateFails_IfTheMultipliersAreCutByAFifth()
        {
            var cohort = new SearchXpRateCohort(
                Parity(),
                Options with { SiteLevels = new[] { 1, 3, 5 } },
                level => SearchRules.XpMultiplierForLevel(level) * 0.8);

            var result = new XpRateGate(cohort).Evaluate(Context);

            result.Verdict.Should().Be(GateVerdict.Failed);
        }

        [Test]
        public void TheXpCohortIsTheSiteLevelsAtTenTimesTheLevel()
        {
            var cohort = new SearchXpRateCohort(Parity(), Options);

            cohort.Cases(Context).Select(xpCase => xpCase.Group).Should().Equal("Site level 1", "Site level 2", "Site level 3", "Site level 4", "Site level 5");
            SearchXpRateCohort.RankOf(3).Should().Be(30);
            cohort.RunName.Should().Be("site");
        }

        [Test]
        public void TheLegacyLoopTakesTwoSecondsAnAttemptAndTheBoardSixSecondsAnAction_WithTheSameTimeToReachTheSite()
        {
            var cohort = new SearchXpRateCohort(Parity(), Options);

            cohort.LegacyTiming.SecondsPerAction.Should().Be(2.0);
            cohort.ReplacementTiming.SecondsPerAction.Should().Be(SearchRules.ActionSeconds);
            cohort.ReplacementTiming.OverheadSecondsPerRun.Should().Be(cohort.LegacyTiming.OverheadSecondsPerRun);
        }

        [Test]
        public void TheXpCohortJudgesTheLowerXpPerHourOfTheTwoPlayers()
        {
            var parity = Parity();
            var cohort = new SearchXpRateCohort(parity, Options);
            var xpCase = cohort.Cases(Context).First(c => c.Group == "Site level 3");

            var judged = cohort.PlayReplacement(xpCase, Context).XpPerHour(cohort.ReplacementTiming);
            var careless = cohort.Play(3, 30, SearchPlayerKind.Careless).XpPerHour(cohort.ReplacementTiming);
            var skilled = cohort.Play(3, 30, SearchPlayerKind.Skilled).XpPerHour(cohort.ReplacementTiming);

            judged.Should().BeApproximately(Math.Min(careless, skilled), 1e-6);
        }

        [Test]
        public void QuickSearchPaysTodaysXpInTodaysTime_SoItsXpPerHourIsTodaysAndNotAbove()
        {
            var result = SearchReport.QuickSearchXp(Options, GateThresholds.Default);

            result.Gate.Should().Be(SearchReport.QuickSearchXpGate);
            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            result.Tables.Single().Rows.Should().OnlyContain(row => row[9] == "Equal");
        }
    }
}
