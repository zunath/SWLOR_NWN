using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Tests.Support;

namespace SWLOR.CraftSim.Tests.Gates
{
    [TestFixture]
    public class GateReportTests
    {
        private static GateContext Context() => new(SharedData.Recipes, SharedData.Gear, 100, 7, AttributeSweep.Default);

        [Test]
        public void TheSixGatesOfThePlanAreNamed()
        {
            GateNames.All.Should().Equal("No fixed rotation", "Style balance", "Perks optional", "Length", "Economy parity", "XP rate");
        }

        [Test]
        public void Run_EvaluatesEveryGateWithTheContext()
        {
            var seen = new List<int>();
            var gates = new IBalanceGate[]
            {
                new FakeGate(GateNames.Length, GateVerdict.Passed, seen),
                new FakeGate(GateNames.StyleBalance, GateVerdict.Failed, seen)
            };

            var results = GateReport.Run(gates, Context());

            results.Select(r => r.Gate).Should().Equal(GateNames.Length, GateNames.StyleBalance);
            seen.Should().Equal(7, 7);
        }

        [Test]
        public void Build_ListsVerdictsAndEachGatesEvidence()
        {
            var table = new GateTable("Actions per craft", new[] { "Levels", "Actions" }, new[] { (IReadOnlyList<string>)new[] { "1-10", "12.5" } });
            var results = new[]
            {
                new GateResult(GateNames.Length, GateVerdict.Passed, "Every band is 10-20 actions.", new[] { table }),
                GateResult.NotRun(GateNames.EconomyParity, "No engine to drive yet.")
            };

            var report = GateReport.Build("Plasma Forge gates", Context(), results, "SWLOR.CraftSim gates --seed 7");

            report.Should().Contain("# Plasma Forge gates");
            report.Should().Contain("Seed 7; 100 runs per case.");
            report.Should().Contain("| Length | Passed | Every band is 10-20 actions. |");
            report.Should().Contain("| Economy parity | NotRun | No engine to drive yet. |");
            report.Should().Contain("## Length").And.Contain("### Actions per craft").And.Contain("| 1-10 | 12.5 |");
            report.Should().NotContain("## Economy parity");
        }

        private static List<GateResult> EveryGate(GateVerdict verdict) =>
            GateNames.All.Select(name => new GateResult(name, verdict, "")).ToList();

        [Test]
        public void AllPassed_NeedsEachNamedGateToHaveOnePassingResult()
        {
            GateReport.AllPassed(EveryGate(GateVerdict.Passed)).Should().BeTrue();
            GateReport.AllPassed(Array.Empty<GateResult>()).Should().BeFalse();
            GateReport.AllPassed(EveryGate(GateVerdict.Passed).Take(2).ToList()).Should().BeFalse("the other gates were never evaluated");
            GateReport.AllPassed(EveryGate(GateVerdict.Failed)).Should().BeFalse();

            var oneNotRun = EveryGate(GateVerdict.Passed);
            oneNotRun[3] = GateResult.NotRun(GateNames.Length, "no engine");
            GateReport.AllPassed(oneNotRun).Should().BeFalse();
        }

        [Test]
        public void Build_WritesTheInputsAndLimitationsAroundTheVerdicts()
        {
            var results = new[] { new GateResult(GateNames.Length, GateVerdict.Passed, "ok") };

            var report = GateReport.Build(
                "Title", Context(), results, "cmd", new[] { "- Scenario: Bench.", "- Gear: none." }, new[] { "- Sampled recipes." });

            report.Should().Contain("## Inputs").And.Contain("- Scenario: Bench.").And.Contain("## Limitations").And.Contain("- Sampled recipes.");
            report.IndexOf("## Inputs", StringComparison.Ordinal).Should().BeLessThan(report.IndexOf("## Verdicts", StringComparison.Ordinal));
            report.IndexOf("## Verdicts", StringComparison.Ordinal).Should().BeLessThan(report.IndexOf("## Limitations", StringComparison.Ordinal));
        }

        [Test]
        public void Build_LeavesOutEmptyInputsAndLimitations()
        {
            var report = GateReport.Build("Title", Context(), new[] { new GateResult(GateNames.Length, GateVerdict.Passed, "ok") }, "cmd");

            report.Should().NotContain("## Inputs").And.NotContain("## Limitations");
        }

        [Test]
        public void AllPassed_LetsAGateThatDoesNotApplyThroughButNotOneThatWasNotRun()
        {
            var results = EveryGate(GateVerdict.Passed);
            results[1] = GateResult.NotApplicable(GateNames.StyleBalance, "no styles");

            GateReport.AllPassed(results).Should().BeTrue();
            results[1].Verdict.Should().Be(GateVerdict.NotApplicable);
            results[1].Summary.Should().Be("no styles");

            results[2] = GateResult.NotRun(GateNames.PerksOptional, "not played");
            GateReport.AllPassed(results).Should().BeFalse();
        }

        [Test]
        public void AllPassed_RejectsADuplicateResultForAGate()
        {
            var results = EveryGate(GateVerdict.Passed);
            results.Add(new GateResult(GateNames.Length, GateVerdict.Passed, "again"));

            GateReport.AllPassed(results).Should().BeFalse();
        }

        [Test]
        public void AllPassed_IgnoresAGateTheSimulatorDoesNotName()
        {
            var results = EveryGate(GateVerdict.Passed);
            results.Add(new GateResult("Extra", GateVerdict.Failed, ""));

            GateReport.AllPassed(results).Should().BeTrue();
        }

        private sealed class FakeGate : IBalanceGate
        {
            private readonly GateVerdict _verdict;
            private readonly List<int> _seen;

            public FakeGate(string name, GateVerdict verdict, List<int> seen)
            {
                Name = name;
                _verdict = verdict;
                _seen = seen;
            }

            public string Name { get; }

            public GateResult Evaluate(GateContext context)
            {
                _seen.Add(context.Seed);
                return new GateResult(Name, _verdict, "fake");
            }
        }
    }
}
