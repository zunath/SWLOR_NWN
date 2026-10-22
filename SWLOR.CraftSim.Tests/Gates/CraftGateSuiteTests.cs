using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Tests.Support;
using static SWLOR.CraftSim.Tests.Support.BenchHarness;

namespace SWLOR.CraftSim.Tests.Gates
{
    [TestFixture]
    public class CraftGateSuiteTests
    {
        // Levels the old minigame is reliable at, with the at-level band of the perks gate moved to them so every gate can be played on one cohort.
        private static readonly int[] Levels = { 5, 15 };

        private static readonly GateThresholds ReliableBands = GateThresholds.Default with
        {
            PerksOptionalBands = new[] { new RecipeBand("1-20", 1, 20) },
            EnhancementParityBands = new[] { new RecipeBand("1-20", 1, 20) }
        };

        private static BenchScenario EverythingPasses() => new() { SecondsPerAction = 2 };

        private static BenchScenario WithStyles() => new()
        {
            SecondsPerAction = 2,
            StylePairs = new[] { new CraftStylePair(BenchScenario.PressStyle, BenchScenario.PolishStyle) }
        };

        [Test]
        public void TheGateKeysNameTheSevenGatesInOrder()
        {
            CraftGateSuite.GateKeys.Should().Equal("rotation", "styles", "perks", "length", "materials", "xp", "enhancements");
            Suite(new BenchScenario()).Gates.Select(gate => gate.Name).Should().Equal(GateNames.All);
        }

        [Test]
        public void SelectReturnsTheNamedGatesInSuiteOrderAndRejectsUnknownNames()
        {
            var suite = Suite(new BenchScenario());

            suite.Select(new[] { "XP", "length", "length" }).Select(gate => gate.Name).Should().Equal(GateNames.Length, GateNames.XpRate);
            var act = () => suite.Select(new[] { "balance" });
            act.Should().Throw<ArgumentException>().WithMessage("*--gates names rotation, styles, perks, length, materials, xp, enhancements (or a gate number, 1 to 7), not 'balance'*");
            var none = () => suite.Select(Array.Empty<string>());
            none.Should().Throw<ArgumentException>().WithMessage("*--gates needs at least one of rotation*");
        }

        [Test]
        public void TheGatesCanBeNamedByNumber()
        {
            var suite = Suite(new BenchScenario());

            suite.Select(new[] { "7" }).Select(gate => gate.Name).Should().Equal(GateNames.EnhancementParity);
            suite.Select(new[] { "7", "4", "length", "1" }).Select(gate => gate.Name)
                .Should().Equal(GateNames.NoFixedRotation, GateNames.Length, GateNames.EnhancementParity);
            suite.Select(new[] { "enhancements" }).Single().Name.Should().Be(GateNames.EnhancementParity);

            foreach (var bad in new[] { "0", "8", "-1", "+3", "7.0" })
            {
                var act = () => suite.Select(new[] { bad });
                act.Should().Throw<ArgumentException>().WithMessage($"*not '{bad}'*");
            }
        }

        [Test]
        public void AScenarioThatPassesEveryGateShipsAndAFailingOneDoesNot()
        {
            var context = Context(Levels, runs: 24, perLevel: 2);
            var suite = Suite(EverythingPasses(), recipesPerBand: 2, thresholds: ReliableBands);
            var results = GateReport.Run(suite.Gates, context);

            results.Select(r => r.Verdict).Should().Equal(
                new[] { GateVerdict.Passed, GateVerdict.NotApplicable, GateVerdict.Passed, GateVerdict.Passed, GateVerdict.Passed, GateVerdict.Passed, GateVerdict.Passed },
                string.Join("; ", results.Select(r => r.Gate + ": " + r.Summary)));
            GateReport.AllPassed(results).Should().BeTrue();

            var dominated = Suite(new BenchScenario { Dominant = true, SecondsPerAction = 2 }, recipesPerBand: 2, thresholds: ReliableBands);
            GateReport.AllPassed(GateReport.Run(dominated.Gates, context)).Should().BeFalse("a dominant rotation fails the no-fixed-rotation gate");
        }

        [Test]
        public void TheGatesSharePlaysSoARerunOfOneContextIsFree()
        {
            var context = Context(Levels, runs: 24);
            var suite = Suite(new BenchScenario());
            var first = GateReport.Run(suite.Select(new[] { "length", "materials", "xp" }), context);

            var watch = System.Diagnostics.Stopwatch.StartNew();
            var again = GateReport.Run(suite.Select(new[] { "length", "materials", "xp" }), context);

            watch.Elapsed.TotalSeconds.Should().BeLessThan(2, "every adaptive and legacy cell is already played");
            again.Select(r => r.Summary).Should().Equal(first.Select(r => r.Summary));
        }

        [Test]
        public void ANewContextStartsThePlaysOver()
        {
            var suite = Suite(new BenchScenario());

            var one = suite.Select(new[] { "length" }).Single().Evaluate(Context(Levels, runs: 12, seed: 1));
            var two = suite.Select(new[] { "length" }).Single().Evaluate(Context(Levels, runs: 12, seed: 2));

            one.Tables[0].Rows.Select(r => string.Join("|", r)).Should().NotEqual(two.Tables[0].Rows.Select(r => string.Join("|", r)));
        }

        [Test]
        public void TheInputsNameTheScenarioGearPoliciesTimesAndEveryThreshold()
        {
            var context = Context(Levels);
            var suite = Suite(WithStyles());

            var inputs = string.Join(Environment.NewLine, suite.Inputs(context));

            inputs.Should().Contain("Test Bench (`bench`)").And.Contain("Smithery recipes").And.Contain("Forge");
            inputs.Should().Contain("no gear (Control 0, Craftsmanship 0, CP 0)").And.Contain("best gear");
            inputs.Should().Contain("Adaptive player: depth 3").And.Contain("Fixed-rotation solver: beam 3");
            inputs.Should().Contain("2.0 s per action in the new window and 6.0 s in the old one");
            inputs.Should().Contain("Heavy Press vs Fine Polish (from rank 20)");
            inputs.Should().Contain("1-10: 1 of 2").And.Contain("11-20: 1 of 2");
            foreach (var (name, _, _) in GateThresholds.Default.Describe())
                inputs.Should().Contain($"`{name}`");

            inputs.Should().Contain("Enhancement thresholds in force (`CraftOutcomeRules`, read by gate 7): 50% / 85% / 85% for slots 1, 2 and 3")
                .And.Contain("The scenario applies enhancements to its engine's progress meter");

            string.Join(Environment.NewLine, Suite(new BenchScenario()).Inputs(context)).Should().Contain("style-balance gate is not applicable");
        }

        [Test]
        public void TheThresholdsCanOverrideTheScenariosSolverEffort()
        {
            var context = Context(Levels);

            var own = string.Join(Environment.NewLine, Suite(new BenchScenario()).Inputs(context));
            var overridden = string.Join(
                Environment.NewLine,
                Suite(new BenchScenario(), thresholds: GateThresholds.Default with { SolverOptions = FixedRotationSolverOptions.Thorough }).Inputs(context));

            own.Should().Contain("beam 3");
            overridden.Should().Contain("beam 12").And.NotContain("beam 3");
            GateThresholds.Default.Describe().Single(d => d.Name == "SolverOptions").Value.Should().Be("the scenario's");
        }

        [Test]
        public void TheLimitationsEndWithTheScenariosOwn()
        {
            var limitations = Suite(new BenchScenario()).Limitations();

            limitations.Should().Contain(line => line.Contains("floor on skilled play"));
            limitations.Should().Contain(line => line.Contains("matched rank, gear and effort"));
            limitations[^1].Should().Be("- The test bench is not a real minigame.");
        }

        [Test]
        public void EveryThresholdIsDescribedWithItsValue()
        {
            var described = GateThresholds.Default.Describe();

            described.Select(d => d.Name).Should().Contain(new[]
            {
                "RotationMarginPoints", "StyleMinimumWinShare", "StyleMaximumMeanDifferencePoints", "PerksOptionalRank",
                "PerksOptionalMinimumFullQualityShare", "MaterialTolerance", "MaterialQualityShortfallPoints", "XpRateMinimumRatio", "LegacySecondsPerAction", "RecipesPerBand",
                "EnhancementParityRank", "EnhancementParityBands", "EnhancementSlotCounts", "EnhancementParityMinimumRatio"
            });
            described.Single(d => d.Name == "RotationMarginPoints").Value.Should().Be("10");
            described.Single(d => d.Name == "StyleMinimumWinShare").Value.Should().Be("30%");
            described.Single(d => d.Name == "StyleMaximumMeanDifferencePoints").Value.Should().Be("5");
            described.Single(d => d.Name == "PerksOptionalRank").Value.Should().Be("50");
            described.Single(d => d.Name == "PerksOptionalMinimumFullQualityShare").Value.Should().Be("50%");
            described.Single(d => d.Name == "MaterialTolerance").Value.Should().Be("10%");
            described.Single(d => d.Name == "MaterialQualityShortfallPoints").Value.Should().Be("5");
            described.Single(d => d.Name == "XpRateMinimumRatio").Value.Should().Be("1");
        }

        [Test]
        public void AFullReportHasEverySectionTheStandardsAskFor()
        {
            var context = Context(Levels, runs: 16);
            var suite = Suite(new BenchScenario { SecondsPerAction = 2 }, thresholds: ReliableBands);
            var results = GateReport.Run(suite.Gates, context);

            var report = GateReport.Build("Bench gates", context, results, "cmd", suite.Inputs(context), suite.Limitations());

            report.Should().Contain("# Bench gates").And.Contain("Seed 1; 16 runs per case").And.Contain("## Inputs").And.Contain("## Verdicts");
            foreach (var name in GateNames.All)
                report.Should().Contain($"| {name} |");

            report.Should().Contain("## No fixed rotation").And.Contain("## Perks optional").And.Contain("## Length").And.Contain("## Economy parity").And.Contain("## XP rate").And.Contain("## Enhancement parity");
            report.Should().NotContain("## Style balance", "a gate that does not apply has no evidence tables");
            report.Should().Contain("## Limitations");
        }
    }
}
