using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Legacy.Crafting;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.CraftSim.Tests.Support.BenchHarness;

namespace SWLOR.CraftSim.Tests.Gates
{
    [TestFixture]
    public class EnhancementParityGateTests
    {
        // A level 45 recipe (band 41-45) and a level 52 one (band 51-53), played at rank 50.
        private static readonly int[] AtLevel = { 45, 52 };

        private static GateResult Run(ICraftScenario scenario, GateContext context, GateThresholds thresholds = null, int recipesPerBand = 1) =>
            Evaluate("enhancements", scenario, context, recipesPerBand, thresholds);

        private static GateTable Table(GateResult result, string titleStart) => result.Tables.Single(t => t.Title.StartsWith(titleStart));

        private static TableScenario Constant(int quality) => new() { EnhancedQuality = (_, _) => quality };

        [Test]
        public void TheBenchMinigamePassesWhenItsQualityIsHighEnough()
        {
            var result = Run(new BenchScenario(), Context(AtLevel, runs: 20));

            result.Gate.Should().Be(GateNames.EnhancementParity);
            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            var comparison = Table(result, "Rank 50, best gear, at level");
            comparison.Rows.Select(row => row[0]).Should().Equal("41-45", "41-45", "41-45", "51-53", "51-53", "51-53");
            comparison.Rows.Select(row => row[1]).Should().Equal("1", "2", "3", "1", "2", "3");
            RowVerdicts(result).Should().OnlyContain(verdict => verdict == "Passed");
            result.Summary.Should().Contain("thresholds in force (50% / 85% / 100%)").And.Contain("1, 2, 3 slot(s)");
        }

        [Test]
        public void ItFailsWhenTheQualityMeetsTheFirstThresholdOnlyAndSaysWhichCases()
        {
            // 60% quality: thresholds of 50/85/100 transfer exactly one, where today's chance roll averages 1.8 of 3.
            var result = Run(Constant(60), Context(AtLevel, runs: 20));

            result.Verdict.Should().Be(GateVerdict.Failed);
            var rows = Table(result, "Rank 50, best gear, at level").Rows;
            var triple = rows.Single(row => row[0] == "41-45" && row[1] == "3");
            triple[10].Should().StartWith("1.00 (+/- 0.00)");
            double.Parse(triple[7]).Should().BeGreaterThan(1.0, "today's three-slot craft transfers more than one");
            triple[^1].Should().Be("Failed");
            result.Summary.Should().Contain("41-45 / 3 slot(s): 1.00 against today's").And.Contain("transfer fewer enhancements than today");
        }

        [Test]
        public void ItPassesWhenEveryEnhancementTransfers()
        {
            var result = Run(Constant(100), Context(AtLevel, runs: 20));

            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            var rows = Table(result, "Rank 50, best gear, at level").Rows;
            rows.Select(row => row[10]).Should().OnlyContain(cell => cell.EndsWith("(+/- 0.00)"));
            rows.Where(row => row[1] == "3").Select(row => row[10]).Should().OnlyContain(cell => cell.StartsWith("3.00"));
        }

        [Test]
        public void ItCountsTransfersWithTheThresholdsInForceAtEachSlotCount()
        {
            // Quality falls by 10 points per enhancement: 90, 80, 70. Slot 1 needs 50, slot 2 needs 85, slot 3 needs 85: one transfers each time.
            var scenario = new TableScenario { EnhancedQuality = (_, config) => 100 - 10 * config.Count };

            var result = Run(scenario, Context(AtLevel, runs: 12));

            Table(result, "Rank 50, best gear, at level").Rows
                .Where(row => row[0] == "41-45").Select(row => row[10])
                .Should().Equal("1.00 (+/- 0.00)", "1.00 (+/- 0.00)", "1.00 (+/- 0.00)");
        }

        [Test]
        public void TodaysSideIsTheLegacyLookaheadPlayerWithTheSameRecipesPenaltiesAndDice()
        {
            var context = Context(AtLevel, runs: 25, seed: 4);
            var suite = Suite(Constant(100));
            var run = suite.RunFor(context);
            var gearIndex = run.Gear.Count - 1;

            var result = suite.Select(new[] { "enhancements" }).Single().Evaluate(context);

            var band = run.EnhancedBands(GateThresholds.Default.EnhancementParityBands).Single(b => b.Band.Band.Name == "41-45");
            var recipe = band.Recipes.Single();
            foreach (var slots in new[] { 1, 2, 3 })
            {
                var cell = LegacyEnhancementBaseline.PlayCell(
                    recipe.Recipe.Level, 50, run.Gear[gearIndex], gearIndex, LegacyEnhancementBaseline.LookaheadIndex,
                    new CraftEnhancementConfiguration(slots, recipe.Item.ProgressPenalty), 25, 4);
                var row = Table(result, "Rank 50, best gear, at level").Rows.Single(r => r[0] == "41-45" && r[1] == slots.ToString());

                row[7].Should().Be(cell.ExpectedTransfersPerAttempt.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                row[3].Should().Be(recipe.Item.ProgressPenalty.ToString());
            }
        }

        [Test]
        public void ItPlaysRankFiftyInTheBestGearAndTheGearAndRankAreStatedInTheTitle()
        {
            var requested = new System.Collections.Concurrent.ConcurrentBag<CraftEngineRequest>();
            var scenario = new BenchScenario { Spy = requested.Add };

            var result = Run(scenario, Context(AtLevel, runs: 8));

            requested.Where(r => !r.Enhancements.IsNone).Should().NotBeEmpty().And.OnlyContain(r => r.Rank == 50 && r.Gear.Name == "best gear" && r.Style == null);
            Table(result, "Rank 50, best gear, at level").Rows.Should().NotBeEmpty();
        }

        [Test]
        public void ThePlainAndEnhancedEnginesAreBothBuiltForTheSameRecipeAndEnhancedOnesCarryThePenalty()
        {
            var requested = new System.Collections.Concurrent.ConcurrentBag<CraftEngineRequest>();

            Run(new BenchScenario { Spy = requested.Add }, Context(new[] { 45 }, runs: 6));

            var enhanced = requested.Where(r => !r.Enhancements.IsNone).ToList();
            enhanced.Select(r => r.Enhancements.Count).Distinct().OrderBy(c => c).Should().Equal(1, 2, 3);
            enhanced.Select(r => r.Enhancements.ProgressPenaltyEach).Distinct().Should().ContainSingle();
        }

        [Test]
        public void TheComparisonTableCarriesTheDifficultyTheEnhancementsAdd()
        {
            var result = Run(Constant(100), Context(new[] { 45 }, runs: 6));

            var rows = Table(result, "Rank 50, best gear, at level").Rows;
            var extra = rows.Select(row => int.Parse(row[4].TrimEnd('%'))).ToList();
            extra.Should().BeInAscendingOrder().And.OnlyHaveUniqueItems("two enhancements ask for twice what one does");
            extra[0].Should().BeGreaterThan(10);
        }

        [Test]
        public void TheThresholdsThatWouldMeetParityAreReported()
        {
            // 80% quality every time: parity with today's 80% chance roll needs each slot's threshold at 80.
            var result = Run(Constant(80), Context(AtLevel, runs: 16));

            var thresholds = Table(result, "Thresholds, first / second / third slot");
            thresholds.Header.Should().Equal("Scope", "Thresholds that meet parity", "In steps of 5", "Thresholds in force");
            thresholds.Rows[0][0].Should().Contain("Every band together");
            thresholds.Rows[0][1].Should().Be("80% / 80% / 80%");
            thresholds.Rows[0][2].Should().Be("80% / 80% / 80%");
            thresholds.Rows.Select(row => row[3]).Should().OnlyContain(cell => cell == "50% / 85% / 100%");
            thresholds.Rows.Select(row => row[0]).Should().Contain(new[] { "Band 41-45 alone", "Band 51-53 alone" });
            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Summary.Should().Contain("Thresholds that meet parity in every band for this quality: 80% / 80% / 80%");
        }

        [Test]
        public void ARoundFigureIsOfferedWhenTheExactOneIsOdd()
        {
            var result = Run(Constant(83), Context(AtLevel, runs: 12));

            var row = Table(result, "Thresholds, first / second / third slot").Rows[0];
            row[1].Should().Be("83% / 83% / 83%");
            row[2].Should().Be("80% / 80% / 80%");
        }

        [Test]
        public void TheThresholdsInForceAreReadFromTheOutcomeRules()
        {
            var result = Run(Constant(100), Context(AtLevel, runs: 6));

            var expected = string.Join(" / ", CraftOutcomeRules.GetEnhancementThresholds(3).Select(t => t + "%"));
            Table(result, "Rank 50, best gear, at level").Title.Should().Contain(expected);
            Table(result, "Thresholds, first / second / third slot").Rows.Select(row => row[3]).Should().OnlyContain(cell => cell == expected);
            result.Summary.Should().Contain($"({expected})");
        }

        [Test]
        public void TheQualityTableGivesPercentilesOfTheItemsMade()
        {
            var result = Run(Constant(80), Context(AtLevel, runs: 10));

            var quality = Table(result, "Rank 50, best gear: quality");
            quality.Header.Should().Contain("Median");
            quality.Rows.Should().OnlyContain(row => row[2] == "100%" && row[3] == "80%" && row[5] == "80%" && row[7] == "80%");
        }

        [Test]
        public void TheRepresentativeEnhancementsAreNamedWithTheirSpread()
        {
            var result = Run(Constant(100), Context(AtLevel, runs: 6));

            var table = Table(result, "Representative enhancements");
            table.Rows.Should().NotBeEmpty();
            table.Rows.Select(row => row[0]).Should().OnlyContain(kind => kind == "Armor" || kind == "Weapon");
            table.Rows.Select(row => row[1]).Should().OnlyContain(name => name.Contains("`"));
            table.Rows.Select(row => row[4]).Should().OnlyContain(spread => spread.Contains("items:"));
        }

        [Test]
        public void AValueOfTheRatioThresholdLetsAShortfallThrough()
        {
            var lenient = GateThresholds.Default with { EnhancementParityMinimumRatio = 0.0 };

            Run(Constant(60), Context(AtLevel, runs: 12), lenient).Verdict.Should().Be(GateVerdict.Passed);
            Run(Constant(60), Context(AtLevel, runs: 12), GateThresholds.Default with { EnhancementParityMinimumRatio = 3.0 }).Verdict.Should().Be(GateVerdict.Failed);
        }

        [Test]
        public void TheSlotCountsAreAThreshold()
        {
            var twoOnly = GateThresholds.Default with { EnhancementSlotCounts = new[] { 2 } };

            var result = Run(Constant(100), Context(new[] { 45 }, runs: 6), twoOnly);

            Table(result, "Rank 50, best gear, at level").Rows.Select(row => row[1]).Should().Equal("2");
            Table(result, "Thresholds, first / second / third slot").Rows[0][1].Should().HaveLength("100% / 100%".Length);
        }

        [Test]
        public void TheRankAndBandsAreThresholds()
        {
            var thresholds = GateThresholds.Default with { EnhancementParityRank = 30, EnhancementParityBands = new[] { new RecipeBand("1-20", 1, 20) } };

            var result = Run(Constant(100), Context(new[] { 5, 15 }, runs: 6), thresholds);

            Table(result, "Rank 30, best gear, at level").Rows.Select(row => row[0]).Distinct().Should().Equal("1-20");
        }

        [Test]
        public void TheSameSeedRepeatsTheReportAndAnotherSeedChangesTheLegacyFigures()
        {
            string Report(int seed) => string.Join("|", Run(new BenchScenario(), Context(AtLevel, runs: 10, seed: seed)).Tables[0].Rows.Select(row => string.Join(",", row)));

            Report(3).Should().Be(Report(3));
            Report(3).Should().NotBe(Report(4));
        }

        [Test]
        public void ARecipeSampleOfSeveralRecipesIsWeightedEqually()
        {
            var result = Run(Constant(100), Context(new[] { 44, 45 }, runs: 6, perLevel: 2), recipesPerBand: 4);

            Table(result, "Rank 50, best gear, at level").Rows.Should().OnlyContain(row => row[2] == "4");
        }

        [Test]
        public void ASampledRecipeWithNoEnhancementItToUseIsLeftOutAndSaid()
        {
            var weaponsOnly = EnhancementCatalog.Build(SharedData.Enhancements.Items.Where(i => i.Type == RecipeEnhancementType.Weapon));
            var context = new GateContext(BenchRecipes.Catalog(new[] { 45 }), SharedData.Gear, 6, 1, AttributeSweep.Default, enhancements: weaponsOnly);

            var result = Evaluate("enhancements", Constant(100), context, 2);

            Table(result, "Representative enhancements").Title.Should().Contain("1 sampled recipe(s) accept no enhancement of their kind");
            Table(result, "Rank 50, best gear, at level").Rows.Should().OnlyContain(row => row[2] == "1");
        }

        [Test]
        public void IsNotRunWhenNoEnhanceableRecipeIsAtLevelForRankFifty()
        {
            var result = Run(Constant(100), Context(new[] { 5, 15 }, runs: 6));

            result.Verdict.Should().Be(GateVerdict.NotRun);
            result.Summary.Should().Contain("no recipe with an enhancement slot").And.Contain("41-45");
        }

        [Test]
        public void IsNotRunForRecipesWithoutEnhancementSlots()
        {
            var plain = RecipeCatalog.From(BenchRecipes.Catalog(AtLevel).Recipes.Select(r => new RecipeEntry(r.Type, r.Skill, r.Level, r.Resref, r.Category, r.Components)));
            var context = new GateContext(plain, SharedData.Gear, 6, 1, AttributeSweep.Default, enhancements: SharedData.Enhancements);

            Run(Constant(100), context).Verdict.Should().Be(GateVerdict.NotRun);
        }

        [Test]
        public void IsNotRunWhenTheScenarioDoesNotApplyEnhancementsAndSaysHowToFixIt()
        {
            var result = Run(new BenchScenario { Enhancements = EnhancementSupport.NotModeled }, Context(AtLevel, runs: 6));

            result.Verdict.Should().Be(GateVerdict.NotRun);
            result.Summary.Should().Contain("CraftEngineRequest.Enhancements").And.Contain("ICraftScenario.Enhancements").And.Contain("Modeled");
            Run(new ScenarioThatDoesNothingSpecial(), Context(AtLevel, runs: 6)).Verdict.Should().Be(GateVerdict.NotRun, "a scenario that never heard of enhancements is not modeled by default");
        }

        [Test]
        public void IsNotApplicableToAMinigameWithoutEnhancementSlots()
        {
            var result = Run(new BenchScenario { Enhancements = EnhancementSupport.NoSlots }, Context(AtLevel, runs: 6));

            result.Verdict.Should().Be(GateVerdict.NotApplicable);
            GateReport.AllPassed(new[] { result }.Concat(GateNames.All.Where(n => n != GateNames.EnhancementParity).Select(n => new GateResult(n, GateVerdict.Passed, ""))).ToList())
                .Should().BeTrue("a minigame with no enhancements ships without gate 7");
        }

        [Test]
        public void IsNotRunWithoutEnhancementItemsInTheContext()
        {
            var context = new GateContext(BenchRecipes.Catalog(AtLevel), SharedData.Gear, 6, 1, AttributeSweep.Default);

            var result = Run(Constant(100), context);

            result.Verdict.Should().Be(GateVerdict.NotRun);
            result.Summary.Should().Contain("no enhancement items");
            GateReport.AllPassed(new[] { result }).Should().BeFalse();
        }

        [Test]
        public void FailsWhenTheScenarioSaysItModelsEnhancementsButItsEngineIgnoresThem()
        {
            var result = Run(new BenchScenario { IgnoreEnhancements = true }, Context(AtLevel, runs: 6));

            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Summary.Should().Contain("progress maximum does not rise").And.Contain("as easy as plain ones").And.Contain("progress maximum 100 without enhancements and 100 with 3 x");
        }

        [Test]
        public void AFailedCraftCountsNoEnhancementsSoAMinigameThatBreaksPiecesFallsShort()
        {
            // The bench breaks its piece when Press costs this much integrity; the failed attempts transfer nothing.
            var brittle = new BenchScenario { PressIntegrityCost = 30 };

            var result = Run(brittle, Context(AtLevel, runs: 16));

            var rows = Table(result, "Rank 50, best gear, at level").Rows;
            rows.Select(row => double.Parse(row[8].TrimEnd('%'))).Should().Contain(share => share < 100, "some attempts break the piece");
            result.Verdict.Should().Be(GateVerdict.Failed);
        }

        [Test]
        public void TheInputsOfTheSuiteStateTheScenarioSEnhancementSupport()
        {
            var inputs = string.Join(Environment.NewLine, Suite(new BenchScenario { Enhancements = EnhancementSupport.NotModeled }).Inputs(Context(AtLevel)));

            inputs.Should().Contain("does not apply enhancements yet, so gate 7 is not run");
        }

        private sealed class ScenarioThatDoesNothingSpecial : ICraftScenario
        {
            public string Name => "plain";
            public string Title => "Plain";
            public SkillType Skill => SkillType.Smithery;
            public CraftMinigameType Minigame => CraftMinigameType.Forge;
            public double SecondsPerAction => 6;

            public ICraftMinigameEngine CreateEngine(CraftEngineRequest request) => new TableEngine(request.Random, 100);
        }
    }
}
