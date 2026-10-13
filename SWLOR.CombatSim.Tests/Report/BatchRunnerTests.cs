using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Report;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;

namespace SWLOR.CombatSim.Tests.Report
{
    [TestFixture]
    public class BatchRunnerTests
    {
        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [TestCase(1)]
        [TestCase(7)]
        [TestCase(25)]
        public void Run_ProducesOneFightEndTimePerRun(int runs)
        {
            var summary = BatchRunner.Run(DecisiveScenario(), RuleSets.Wave1(), _catalog, runs, seed: 1);

            summary.Runs.Should().Be(runs);
            summary.FightEndTimes.Should().HaveCount(runs);
            summary.RuleSetName.Should().Be("wave1");
        }

        [Test]
        public void Run_CountsDecisiveRunsDeathsAndWinsForAFightTeamOneAlwaysWins()
        {
            const int runs = 10;

            var summary = BatchRunner.Run(DecisiveScenario(), RuleSets.Current(), _catalog, runs, seed: 1);

            summary.DecisiveRuns.Should().Be(runs);
            summary.WinsByTeam.Should().ContainSingle().Which.Should().Be(new KeyValuePair<int, int>(0, runs));
            summary.RunsWithDeath.Should().Be(runs);
            summary.FirstDeathTimes.Should().HaveCount(runs);
            summary.AverageDeaths.Should().BeApproximately(1.0, 1e-9);
            summary.FightEndTimes.Should().OnlyContain(t => t > 0 && t < 10);
        }

        [Test]
        public void Run_SummarisesEachCombatantAcrossRuns()
        {
            var summary = BatchRunner.Run(DecisiveScenario(), RuleSets.Current(), _catalog, 10, seed: 1);

            summary.Combatants.Select(c => c.Name).Should().Equal("Striker", "Target");
            var striker = summary.Combatants.Single(c => c.Name == "Striker");
            var target = summary.Combatants.Single(c => c.Name == "Target");
            striker.TeamIndex.Should().Be(0);
            target.TeamIndex.Should().Be(1);
            striker.AverageDeaths.Should().Be(0);
            target.AverageDeaths.Should().Be(1);
            target.AverageDamageTakenToHp.Should().BeApproximately(100, 1e-6, "overkill past the target's 100 HP is not counted");
            striker.AverageDamageDealt.Should().BeApproximately(100, 1e-6);
        }

        [Test]
        public void Run_WhenNobodyCanHurtAnybody_ReportsFightsEndedByTheTimeLimit()
        {
            var scenario = TestScenarios.TwoTeams(true,
                new[] { TestCombatants.Dummy("Left") }, new[] { TestCombatants.Dummy("Right") }, durationSeconds: 5f);

            var summary = BatchRunner.Run(scenario, RuleSets.Current(), _catalog, 3, seed: 1);

            summary.Runs.Should().Be(3);
            summary.FightEndTimes.Should().HaveCount(3).And.OnlyContain(t => Math.Abs(t - 5.0) < 1e-6);
            summary.DecisiveRuns.Should().Be(0);
            summary.WinsByTeam.Should().BeEmpty();
            summary.RunsWithDeath.Should().Be(0);
            summary.FirstDeathTimes.Should().BeEmpty();
        }

        [Test]
        public void Run_IsDeterministicForASeed_AndVariesWithTheSeed()
        {
            var first = BatchRunner.Run(DecisiveScenario(), RuleSets.Current(), _catalog, 20, seed: 42);
            var second = BatchRunner.Run(DecisiveScenario(), RuleSets.Current(), _catalog, 20, seed: 42);
            var otherSeed = BatchRunner.Run(DecisiveScenario(), RuleSets.Current(), _catalog, 20, seed: 43);

            second.FightEndTimes.Should().Equal(first.FightEndTimes);
            first.FightEndTimes.Distinct().Should().HaveCountGreaterThan(1, "each run starts its attacks at a different random moment");
            otherSeed.FightEndTimes.Should().NotEqual(first.FightEndTimes);
        }

        [Test]
        public void RecoveryRatio_IsHealingPlusAbsorbedOverHpDamage()
        {
            var summary = new BatchSummary { TotalDamageToHp = 200, TotalHealing = 100, TotalAbsorbed = 50 };

            summary.RecoveryRatio.Should().BeApproximately(0.75, 1e-9);
            new BatchSummary().RecoveryRatio.Should().Be(0);
        }

        /// <summary>A striker that kills a 100 HP target within about three seconds in every run.</summary>
        private static ScenarioSpec DecisiveScenario() =>
            TestScenarios.TwoTeams(
                true,
                new[]
                {
                    new CombatantSpec
                    {
                        Name = "Striker",
                        MaxHp = 1000,
                        AutoAttack = new AutoAttackSpec
                        {
                            AttacksPerSecond = 2f,
                            Damage = new DamageSpec { Min = 50f, Max = 50f, HitChance = 1f }
                        }
                    }
                },
                new[] { new CombatantSpec { Name = "Target", MaxHp = 100 } },
                durationSeconds: 60f);
    }
}
