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
        public void TheFiveGatesOfThePlanAreNamed()
        {
            GateNames.All.Should().Equal("No fixed rotation", "Style balance", "Perks optional", "Length", "Economy parity");
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

        [Test]
        public void AllPassed_NeedsEveryGateToPassAndAtLeastOne()
        {
            GateReport.AllPassed(Array.Empty<GateResult>()).Should().BeFalse();
            GateReport.AllPassed(new[] { new GateResult("a", GateVerdict.Passed, "") }).Should().BeTrue();
            GateReport.AllPassed(new[] { new GateResult("a", GateVerdict.Passed, ""), GateResult.NotRun("b", "") }).Should().BeFalse();
            GateReport.AllPassed(new[] { new GateResult("a", GateVerdict.Failed, "") }).Should().BeFalse();
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
