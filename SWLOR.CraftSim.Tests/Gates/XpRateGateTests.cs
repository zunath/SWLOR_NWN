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
        public void ALoopThatEarnsNoXpNeedsAnUnboundedMultiplier()
        {
            var cohort = new FakeCohort { LegacyXp = 100, LegacyActions = 10, NewXp = 0, NewActions = 10 };

            var result = new XpRateGate(cohort).Evaluate(Empty());

            result.Verdict.Should().Be(GateVerdict.Failed);
            var row = result.Tables[0].Rows.Single();
            row[8].Should().Be("0.00");
            row[9].Should().Be("unbounded (earns no XP)");
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
                new[] { "1-10", "21-30", "41-53" }.SelectMany(band => new[] { "no gear", "mid gear", "best gear" }.SelectMany(gear => new[]
                {
                    $"{band}/{gear}", $"{band}/{gear}, 1 enhancement", $"{band}/{gear}, 2 enhancements", $"{band}/{gear}, 3 enhancements"
                })));
            suite.XpCohort.Name.Should().Be("Item crafts");
        }

        [Test]
        public void TheItemCraftCohortPlaysEachBandAndGearTierPlainAndWithOneTwoAndThreeEnhancements()
        {
            var suite = Suite(new BenchScenario());

            var cases = suite.XpCohort.Cases(Context(new[] { 52 }));

            cases.Select(c => c.Label).Where(label => label.StartsWith("best gear")).Should().Equal(
                "best gear", "best gear, 1 enhancement", "best gear, 2 enhancements", "best gear, 3 enhancements");
            suite.XpCohort.Incomplete(Context(new[] { 52 })).Should().BeNull();
        }

        [Test]
        public void TheEnhancedCasesCountTheEnhancementsDifficultyOnBothSides()
        {
            var context = Context(new[] { 45 }, runs: 30);
            var suite = Suite(new BenchScenario());
            var plain = new XpRateCase("41-53", "best gear");
            var triple = new XpRateCase("41-53", "best gear, 3 enhancements");

            var newPlain = suite.XpCohort.PlayReplacement(plain, context);
            var newTriple = suite.XpCohort.PlayReplacement(triple, context);
            var legacyPlain = suite.XpCohort.PlayLegacy(plain, context);
            var legacyTriple = suite.XpCohort.PlayLegacy(triple, context);

            newTriple.Runs.Should().Be(30);
            legacyTriple.Runs.Should().Be(30);
            newTriple.ActionsPerRun.Should().BeGreaterThan(newPlain.ActionsPerRun, "the bench piece needs more progress with enhancements");
            legacyTriple.XpPerRun.Should().BeLessThan(legacyPlain.XpPerRun, "the old window needs more progress with enhancements, which leaves less quality and fewer crafts made");
        }

        [Test]
        public void TheEnhancedCasesUseTheSameRepresentativeEnhancementsAsGateSeven()
        {
            var context = Context(new[] { 45 }, runs: 10);
            var requested = new System.Collections.Concurrent.ConcurrentBag<CraftEnhancementConfiguration>();
            var spied = Suite(new BenchScenario { Spy = request => requested.Add(request.Enhancements) });
            var recipe = spied.RunFor(context).Bands.Single().Recipes.Single();
            var item = SharedData.Enhancements.ForRecipe(recipe.EnhancementType, recipe.Level);

            spied.XpCohort.PlayReplacement(new XpRateCase("41-53", "best gear, 2 enhancements"), context);

            // The engine check at the largest count builds a three-enhancement engine too; every enhanced request carries the same item's penalty.
            requested.Where(config => config.Count == 2).Should().NotBeEmpty();
            requested.Where(config => !config.IsNone).Should().OnlyContain(config => config.ProgressPenaltyEach == item.ProgressPenalty);
            requested.Select(config => config.Count).Distinct().Should().BeEquivalentTo(new[] { 0, 2, 3 });
        }

        [Test]
        public void ACohortThatCouldNotPlayEverythingIsNotRunInsteadOfPassed()
        {
            var cohort = new FakeCohort { IncompleteReason = "the enhanced crafts were not played." };

            var passing = new XpRateGate(cohort).Evaluate(Empty());
            var failing = new XpRateGate(new FakeCohort { NewActions = 100, IncompleteReason = "the enhanced crafts were not played." }).Evaluate(Empty());

            passing.Verdict.Should().Be(GateVerdict.NotRun);
            passing.Summary.Should().Contain("not complete").And.Contain("the enhanced crafts were not played.");
            passing.Tables.Should().HaveCount(1, "the cases that were played are still reported");
            failing.Verdict.Should().Be(GateVerdict.Failed, "a case that was played and fell short outranks the cases that were not played");
            failing.Summary.Should().Contain("Cases not played: the enhanced crafts were not played.");
        }

        [Test]
        public void AScenarioThatDoesNotModelEnhancementsLeavesTheEnhancedCasesUnplayedAndTheGateNotRun()
        {
            var suite = Suite(new BenchScenario { Enhancements = EnhancementSupport.NotModeled, SecondsPerAction = 2 });
            var context = Context(Levels, runs: 12);

            suite.XpCohort.Cases(context).Select(c => c.Label).Should().OnlyContain(label => !label.Contains("enhancement"));
            suite.XpCohort.Incomplete(context).Should().Contain("does not apply CraftEngineRequest.Enhancements");
            Evaluate("xp", new BenchScenario { Enhancements = EnhancementSupport.NotModeled, SecondsPerAction = 2 }, context).Verdict
                .Should().Be(GateVerdict.NotRun);
        }

        [Test]
        public void AMinigameWithoutEnhancementSlotsPlaysOnlyPlainCrafts()
        {
            var suite = Suite(new BenchScenario { Enhancements = EnhancementSupport.NoSlots });
            var context = Context(Levels, runs: 12);

            suite.XpCohort.Cases(context).Select(c => c.Label).Should().OnlyContain(label => !label.Contains("enhancement"));
            suite.XpCohort.Incomplete(context).Should().BeNull();
        }

        [Test]
        public void ACohortWithNoEnhanceableRecipesPlaysOnlyPlainCraftsEvenWhenTheScenarioIsNotModeled()
        {
            var plainRecipes = RecipeCatalog.From(BenchRecipes.Catalog(Levels).Recipes.Select(r =>
                new RecipeEntry(r.Type, r.Skill, r.Level, r.Resref, r.Category, r.Components)));
            var context = new GateContext(plainRecipes, SharedData.Gear, 12, 1, AttributeSweep.Default, enhancements: SharedData.Enhancements);
            var suite = Suite(new BenchScenario { Enhancements = EnhancementSupport.NotModeled });

            suite.XpCohort.Cases(context).Select(c => c.Label).Should().OnlyContain(label => !label.Contains("enhancement"));
            suite.XpCohort.Incomplete(context).Should().BeNull("there is nothing to enhance");
        }

        [Test]
        public void AScenarioWhoseEngineIgnoresEnhancementsLeavesTheEnhancedCasesUnplayed()
        {
            var suite = Suite(new BenchScenario { IgnoreEnhancements = true });

            suite.XpCohort.Incomplete(Context(Levels)).Should().Contain("does not rise with them");
            suite.XpCohort.Cases(Context(Levels)).Select(c => c.Label).Should().OnlyContain(label => !label.Contains("enhancement"));
        }

        [Test]
        public void WithoutEnhancementItemsTheEnhancedCasesCannotBePlayed()
        {
            var context = new GateContext(BenchRecipes.Catalog(Levels), SharedData.Gear, 12, 1, AttributeSweep.Default);

            Suite(new BenchScenario()).XpCohort.Incomplete(context).Should().Contain("no enhancement items");
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
        public void TheScenariosXpMultiplierScalesTheNewLoopsXpAndNotTheLegacyLoops()
        {
            var context = Context(new[] { 25 }, runs: 30);
            var xpCase = new XpRateCase("21-30", "best gear");
            var plain = Suite(new BenchScenario()).XpCohort;
            var doubled = Suite(new BenchScenario { XpMultiplier = 2 }).XpCohort;

            var before = plain.PlayReplacement(xpCase, context);
            var after = doubled.PlayReplacement(xpCase, context);

            after.ActionsPerRun.Should().Be(before.ActionsPerRun, "the multiplier changes XP and nothing the player does");
            after.XpPerRun.Should().BeApproximately(before.XpPerRun * 2, before.XpPerRun * 0.03);
            doubled.PlayLegacy(xpCase, context).XpPerRun.Should().Be(plain.PlayLegacy(xpCase, context).XpPerRun);
        }

        [Test]
        public void AMultiplierPerRecipeLevelScalesEachRecipeByItsOwnLevel()
        {
            var context = Context(new[] { 5, 25 }, runs: 30);
            var plain = Suite(new BenchScenario()).XpCohort;
            var leveled = Suite(new BenchScenario { XpMultiplierByLevel = level => level <= 10 ? 3.0 : 1.0 }).XpCohort;
            var low = new XpRateCase("1-10", "best gear");
            var high = new XpRateCase("21-30", "best gear");

            leveled.PlayReplacement(low, context).XpPerRun.Should().BeApproximately(plain.PlayReplacement(low, context).XpPerRun * 3, plain.PlayReplacement(low, context).XpPerRun * 0.05);
            leveled.PlayReplacement(high, context).XpPerRun.Should().Be(plain.PlayReplacement(high, context).XpPerRun);
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
            public string IncompleteReason { get; init; }

            public string Name => "Fake";
            public string RunName => "run";
            public XpLoopTiming LegacyTiming => Legacy;
            public XpLoopTiming ReplacementTiming => Replacement;

            IReadOnlyList<XpRateCase> IXpRateCohort.Cases(GateContext context) => Cases;

            string IXpRateCohort.Incomplete(GateContext context) => IncompleteReason;

            public XpLoopSample PlayLegacy(XpRateCase xpCase, GateContext context) => new(LegacyXp * 5, LegacyActions * 5, 5);

            public XpLoopSample PlayReplacement(XpRateCase xpCase, GateContext context) =>
                new((NewXpFor?.Invoke(xpCase) ?? NewXp) * 5, NewActions * 5, 5);
        }
    }
}
