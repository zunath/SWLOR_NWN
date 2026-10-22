using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using static SWLOR.CraftSim.Tests.Support.BenchHarness;

namespace SWLOR.CraftSim.Tests.Gates
{
    [TestFixture]
    public class XpRateGateTests
    {
        private static readonly int[] Levels = { 5, 25 };

        private static GateContext Empty() => new(RecipeCatalog.From(Array.Empty<RecipeEntry>()), SharedData.Gear, 10, 1, AttributeSweep.Default);

        [Test]
        public void TheLoopSampleTurnsActionsIntoMinutesAndXpIntoAnHourlyRate()
        {
            var sample = new XpLoopSample(TotalXp: 1000, TotalActions: 100, Runs: 10);
            var timing = new XpLoopTiming(SecondsPerAction: 6, OverheadSecondsPerRun: 12);

            sample.XpPerRun.Should().Be(100);
            sample.ActionsPerRun.Should().Be(10);
            sample.SecondsPerRun(timing).Should().Be(72, "ten actions at 6 seconds and 12 seconds of overhead");
            sample.XpPerHour(timing).Should().BeApproximately(100 / 72.0 * 3600, 1e-9);
            new XpLoopSample(0, 0, 0).XpPerHour(timing).Should().Be(0);
        }

        [Test]
        public void PassesWhenTheReplacementLoopPaysAtLeastTheLegacyRate()
        {
            var cohort = new FakeCohort { LegacyXp = 100, LegacyActions = 10, NewXp = 100, NewActions = 5 };

            var result = new XpRateGate(cohort).Evaluate(Empty());

            result.Gate.Should().Be(GateNames.XpRate);
            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            result.Tables[0].Rows.Single()[8].Should().Be("2.00");
            result.Tables[0].Rows.Single()[9].Should().Be("-");
            result.Summary.Should().Contain("Fake").And.Contain("2.00x");
        }

        [Test]
        public void FailsAndSaysHowMuchMoreXpTheLoopNeeds()
        {
            var cohort = new FakeCohort { LegacyXp = 100, LegacyActions = 10, NewXp = 100, NewActions = 40 };

            var result = new XpRateGate(cohort).Evaluate(Empty());

            result.Verdict.Should().Be(GateVerdict.Failed);
            var row = result.Tables[0].Rows.Single();
            row[8].Should().Be("0.25");
            row[9].Should().Be("4.00x", "four times the XP per craft brings the slower loop level");
            row[^1].Should().Be("Failed");
            result.Summary.Should().Contain("Group / Variant (0.25x)");
        }

        [Test]
        public void UsesEachLoopsOwnSecondsPerAction()
        {
            // Twice the actions at half the time per action is the same rate.
            var cohort = new FakeCohort
            {
                LegacyXp = 100, LegacyActions = 10, NewXp = 100, NewActions = 20,
                Legacy = new XpLoopTiming(6), Replacement = new XpLoopTiming(3)
            };

            var result = new XpRateGate(cohort).Evaluate(Empty());

            result.Tables[0].Rows.Single()[8].Should().Be("1.00");
            result.Verdict.Should().Be(GateVerdict.Passed);
            result.Tables[0].Title.Should().Contain("6.0 s per action legacy").And.Contain("3.0 s per action new");
        }

        [Test]
        public void CountsPerRunOverheadOnBothSides()
        {
            var cohort = new FakeCohort
            {
                LegacyXp = 100, LegacyActions = 10, NewXp = 100, NewActions = 10,
                Legacy = new XpLoopTiming(6, 60), Replacement = new XpLoopTiming(6, 0)
            };

            var result = new XpRateGate(cohort).Evaluate(Empty());

            result.Tables[0].Rows.Single()[8].Should().Be("2.00", "the new loop has no overhead and the legacy loop costs a minute a run on top of its minute");
        }

        [Test]
        public void TheRatioIsTheNamedThreshold()
        {
            var cohort = new FakeCohort { LegacyXp = 100, LegacyActions = 10, NewXp = 100, NewActions = 10 };

            new XpRateGate(cohort).Evaluate(Empty()).Verdict.Should().Be(GateVerdict.Passed, "equal rates meet 'at least today's'");
            new XpRateGate(cohort, GateThresholds.Default with { XpRateMinimumRatio = 1.5 }).Evaluate(Empty()).Verdict.Should().Be(GateVerdict.Failed);
        }

        [Test]
        public void EveryCaseMustPassOnItsOwn()
        {
            var cohort = new FakeCohort { Cases = new[] { new XpRateCase("1-10", "no gear"), new XpRateCase("1-10", "best gear") } };
            cohort.NewXpFor = xpCase => xpCase.Label == "no gear" ? 400 : 50;

            var result = new XpRateGate(cohort).Evaluate(Empty());

            result.Verdict.Should().Be(GateVerdict.Failed);
            RowVerdicts(result).Should().Equal("Passed", "Failed");
            result.Summary.Should().Contain("1-10 / best gear").And.NotContain("no gear (");
        }

        [Test]
        public void IsNotRunForACohortWithNoCases()
        {
            var cohort = new FakeCohort { Cases = Array.Empty<XpRateCase>() };

            new XpRateGate(cohort).Evaluate(Empty()).Verdict.Should().Be(GateVerdict.NotRun);
        }

        [Test]
        public void TheItemCraftCohortHasABandAndGearTierCaseForEachRecipeBand()
        {
            var suite = Suite(new BenchScenario());
            var context = Context(new[] { 5, 25, 52 });

            var cases = suite.XpCohort.Cases(context);

            cases.Select(c => $"{c.Group}/{c.Label}").Should().Equal(
                "1-10/no gear", "1-10/mid gear", "1-10/best gear",
                "21-30/no gear", "21-30/mid gear", "21-30/best gear",
                "41-53/no gear", "41-53/mid gear", "41-53/best gear");
            suite.XpCohort.Name.Should().Be("Item crafts");
        }

        [Test]
        public void ItemCraftXpFollowsTodaysFormulaWithoutFirstCraftOrBlueprintBonuses()
        {
            // A single recipe at level 25, rank 25: a success pays base + base x quality, a failure 15% of the base.
            var context = Context(new[] { 25 }, runs: 30);
            var suite = Suite(new BenchScenario());
            var xpCase = new XpRateCase("21-30", "best gear");
            var baseXp = ServerXp.ForDelta(0);

            var replacement = suite.XpCohort.PlayReplacement(xpCase, context);

            replacement.Runs.Should().Be(30);
            replacement.XpPerRun.Should().BeInRange(baseXp * 0.15, baseXp * 2.0, "between a failure's 15% and a full-quality success's double");
            replacement.ActionsPerRun.Should().BeInRange(5, 40);
        }

        [Test]
        public void TheLegacySideIsTheOldMinigamesLookaheadPlayerOnTheSameRecipes()
        {
            var context = Context(new[] { 25 }, runs: 30);
            var suite = Suite(new BenchScenario());
            var xpCase = new XpRateCase("21-30", "best gear");

            var legacy = suite.XpCohort.PlayLegacy(xpCase, context);

            legacy.Runs.Should().Be(30);
            legacy.ActionsPerRun.Should().BeInRange(15, 30, "the baseline report shows 22-23 actions for best gear at levels 21-30");
            legacy.XpPerRun.Should().BeGreaterThan(ServerXp.ForDelta(0), "a success pays more than the base XP");
            suite.XpCohort.LegacyTiming.Should().Be(new XpLoopTiming(GateThresholds.Default.LegacySecondsPerAction, 0));
        }

        [Test]
        public void TheCohortUsesTheScenariosSecondsPerActionAndTheThresholdsLegacyEstimate()
        {
            var suite = new CraftGateSuite(
                new BenchScenario { SecondsPerAction = 2.5 }, GateThresholds.Default with { LegacySecondsPerAction = 9, OverheadSecondsPerAttempt = 4 });

            suite.XpCohort.ReplacementTiming.Should().Be(new XpLoopTiming(2.5, 4));
            suite.XpCohort.LegacyTiming.Should().Be(new XpLoopTiming(9, 4));
        }

        [Test]
        public void TheItemCraftGateComparesTheOldAndNewLoopsOverTheSameRuns()
        {
            var fast = Evaluate("xp", new BenchScenario { SecondsPerAction = 2 }, Context(Levels, runs: 24));
            var slow = Evaluate("xp", new BenchScenario { SecondsPerAction = 30 }, Context(Levels, runs: 24));

            fast.Verdict.Should().Be(GateVerdict.Passed, fast.Summary);
            slow.Verdict.Should().Be(GateVerdict.Failed);
            slow.Tables[0].Rows.Should().OnlyContain(row => row[9].EndsWith("x"), "a failing case says the XP multiplier it needs");
            slow.Tables[0].Header.Should().Contain("XP multiplier needed");
        }

        /// <summary>A cohort whose two loops return the numbers a test sets, standing in for a gathering or fishing activity.</summary>
        private sealed class FakeCohort : IXpRateCohort
        {
            public double LegacyXp { get; init; } = 100;
            public double LegacyActions { get; init; } = 10;
            public double NewXp { get; init; } = 100;
            public double NewActions { get; init; } = 10;
            public XpLoopTiming Legacy { get; init; } = new(6);
            public XpLoopTiming Replacement { get; init; } = new(6);
            public IReadOnlyList<XpRateCase> Cases { get; init; } = new[] { new XpRateCase("Group", "Variant") };
            public Func<XpRateCase, double> NewXpFor { get; set; }

            public string Name => "Fake";
            public string RunName => "run";
            public XpLoopTiming LegacyTiming => Legacy;
            public XpLoopTiming ReplacementTiming => Replacement;

            IReadOnlyList<XpRateCase> IXpRateCohort.Cases(GateContext context) => Cases;

            public XpLoopSample PlayLegacy(XpRateCase xpCase, GateContext context) => new(LegacyXp * 5, LegacyActions * 5, 5);

            public XpLoopSample PlayReplacement(XpRateCase xpCase, GateContext context) =>
                new((NewXpFor?.Invoke(xpCase) ?? NewXp) * 5, NewActions * 5, 5);
        }
    }
}
