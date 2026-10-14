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
            var summary = BatchRunner.Run(DecisiveScenario(), RuleSets.Tuned(), _catalog, runs, seed: 1);

            summary.Runs.Should().Be(runs);
            summary.FightEndTimes.Should().HaveCount(runs);
            summary.RuleSetName.Should().Be("tuned");
        }

        [Test]
        public void Run_KeepsSameNamedCombatantsOnOpposingTeamsApart()
        {
            var scenario = TestScenarios.TwoTeams(
                true,
                new[] { TestCombatants.Striker("Fighter", attacksPerSecond: 2f, damage: 10f) },
                new[] { TestCombatants.Striker("Fighter", attacksPerSecond: 1f, damage: 10f) },
                durationSeconds: 30f);

            var summary = BatchRunner.Run(scenario, RuleSets.Current(), _catalog, 3, seed: 1);

            summary.Combatants.Should().HaveCount(2);
            summary.Combatants.Select(c => c.TeamIndex).Should().BeEquivalentTo(new[] { 0, 1 });
            var fast = summary.Combatants.Single(c => c.TeamIndex == 0);
            var slow = summary.Combatants.Single(c => c.TeamIndex == 1);
            fast.AverageDamageDealt.Should().BeGreaterThan(slow.AverageDamageDealt);
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
        public void Run_MeasuresResourceRatesOverTheTimeEachCombatantWasInTheFight()
        {
            var scenario = TestScenarios.TwoTeams(
                false,
                new[] { TestCombatants.Medic("Early", Burst()), TestCombatants.Medic("Late", Burst()) },
                new[] { TestCombatants.Dummy("Enemy", true) },
                durationSeconds: 60f);
            scenario.Teams[0].Members[1].JoinAtSeconds = 30f;

            var summary = BatchRunner.Run(scenario, RuleSets.Current(), _catalog, 5, seed: 1);

            var early = summary.Combatants.Single(c => c.Name == "Early").AverageFpSpentPerMinute;
            var late = summary.Combatants.Single(c => c.Name == "Late").AverageFpSpentPerMinute;
            early.Should().BeGreaterThan(0);
            late.Should().BeApproximately(early, early * 0.2, "both cast at the same cadence while they are in the fight");

            static AbilityUseSpec Burst()
            {
                var use = TestCombatants.Use("ForceBurst1", AbilityTarget.Enemy, minIntervalSeconds: 10f);
                use.Damage = new DamageSpec { Min = 1f, Max = 1f, HitChance = 1f };
                return use;
            }
        }

        [Test]
        public void Run_RejectsMoreCombatantStepsThanABatchCanFinishPromptly()
        {
            var scenario = TestScenarios.TwoTeams(false, new[] { TestCombatants.Dummy("Player") }, new[] { TestCombatants.Dummy("Enemy", true) },
                durationSeconds: 86_400f);
            scenario.TimeStepSeconds = 0.001f;

            var act = () => BatchRunner.Run(scenario, RuleSets.Current(), _catalog, 3, seed: 1);

            act.Should().Throw<ArgumentException>().WithMessage("*combatant-steps*");
        }

        [Test]
        public void Run_RejectsMoreDamageRollsThanABatchCanFinishPromptly()
        {
            var crowd = Horde("Crowd");
            var foes = Horde("Foes");
            var scenario = TestScenarios.TwoTeams(false, new[] { crowd }, new[] { foes }, durationSeconds: 1f);

            var act = () => BatchRunner.Run(scenario, RuleSets.Current(), _catalog, 1, seed: 1);

            act.Should().Throw<ArgumentException>().WithMessage("*simulated events*");
        }

        [Test]
        public void Run_CountsRecurringTicksThatACoarseTimeStepCatchesUp()
        {
            // One step a run, but each step catches up a day of heartbeats for both combatants.
            var scenario = TestScenarios.TwoTeams(false, new[] { TestCombatants.Dummy("Player") }, new[] { TestCombatants.Dummy("Enemy", true) },
                durationSeconds: 86_400f);
            scenario.TimeStepSeconds = 86_400f;

            var act = () => BatchRunner.Run(scenario, RuleSets.Current(), _catalog, BatchRunner.MaximumRuns, seed: 1);

            act.Should().Throw<ArgumentException>().WithMessage("*simulated events*");
        }

        [Test]
        public void Run_CountsTheStepsSpentScanningOverlappingZones()
        {
            // A zone cast every millisecond keeps thousands of pulse schedules alive, and each step looks at all of them.
            var medic = TestCombatants.Medic("Medic", TestCombatants.Use("ForceSanctuary1", AbilityTarget.Self));
            medic.ReactionSeconds = 0.001f;
            var scenario = TestScenarios.TwoTeams(false, new[] { medic }, new[] { TestCombatants.Dummy("Enemy", true) }, durationSeconds: 2.9f);
            scenario.TimeStepSeconds = 0.001f;
            var rules = RuleSets.Current();
            rules.RecastOverrides["ForceSanctuary"] = 0f;

            var act = () => BatchRunner.Run(scenario, rules, _catalog, 1000, seed: 1);

            act.Should().Throw<ArgumentException>().WithMessage("*simulated events*");
        }

        [Test]
        public void Run_CountsEachCombatantsFirstUse_EvenWithALongRecast()
        {
            // Every caster's first Epicenter lands within its reaction time, whatever the 90-second recast.
            var burst = TestCombatants.Use("Epicenter1", AbilityTarget.Self);
            burst.Damage = new DamageSpec { Min = 1f, Max = 1f, Hits = 1000, Area = true };
            var casters = TestCombatants.Medic("Casters", burst);
            casters.Count = 200;
            casters.ReactionSeconds = 0.001f;
            var crowd = TestCombatants.Dummy("Crowd", true);
            crowd.Count = 200;
            var scenario = TestScenarios.TwoTeams(false, new[] { casters }, new[] { crowd }, durationSeconds: 0.001f);
            scenario.TimeStepSeconds = 0.001f;

            var act = () => BatchRunner.Run(scenario, RuleSets.Current(), _catalog, 100, seed: 1);

            act.Should().Throw<ArgumentException>().WithMessage("*simulated events*");
        }

        [Test]
        public void Run_RejectsMoreCombatantPairChecksThanABatchCanFinishPromptly()
        {
            // Two huge sides that barely swing still compare each other every step.
            var left = TestCombatants.Striker("Left", attacksPerSecond: 0.001f, damage: 1f);
            left.Count = 200;
            var right = TestCombatants.Striker("Right", attacksPerSecond: 0.001f, damage: 1f);
            right.Count = 200;
            var scenario = TestScenarios.TwoTeams(false, new[] { left }, new[] { right });

            var act = () => BatchRunner.Run(scenario, RuleSets.Current(), _catalog, 200, seed: 1);

            act.Should().Throw<ArgumentException>().WithMessage("*combatant-pair checks*");
        }

        [Test]
        public void Run_RejectsMorePriorityListChecksThanABatchCanFinishPromptly()
        {
            // Thousands of entries that never fire are each checked on every step.
            var hoarder = TestCombatants.Medic("Hoarder");
            for (var entry = 0; entry < 5000; entry++)
                hoarder.Abilities.Add(TestCombatants.Use("Benevolence1", AbilityTarget.Self, belowHpPercent: 0, minIntervalSeconds: 600));
            var scenario = TestScenarios.TwoTeams(false, new[] { hoarder }, new[] { TestCombatants.Dummy("Enemy", true) });

            var act = () => BatchRunner.Run(scenario, RuleSets.Current(), _catalog, 200, seed: 1);

            act.Should().Throw<ArgumentException>().WithMessage("*priority-list checks*");
        }

        [Test]
        public void Run_RejectsMorePartyShieldApplicationsThanABatchCanFinishPromptly()
        {
            // 200 shouters each shielding all 200 allies every second for a day: about 3.5 billion applications.
            var shouters = TestCombatants.Medic("Shouters", TestCombatants.Use("CleanseOrder2", AbilityTarget.Self));
            shouters.Count = 200;
            var crowd = TestCombatants.Dummy("Crowd", true);
            crowd.Count = 200;
            var scenario = TestScenarios.TwoTeams(false, new[] { shouters }, new[] { crowd }, durationSeconds: 86_400f);
            scenario.TimeStepSeconds = 1f;
            var rules = RuleSets.Current();
            rules.RecastOverrides["CleanseOrder"] = 0f;

            var act = () => BatchRunner.Run(scenario, rules, _catalog, 1, seed: 1);

            act.Should().Throw<ArgumentException>().WithMessage("*simulated events*");
        }

        // 200 strikers whose every swing hits all 200 enemies 1,000 times: about 1.6 billion rolls a second.
        private static CombatantSpec Horde(string name)
        {
            var horde = TestCombatants.Striker(name, attacksPerSecond: 20f, damage: 1f);
            horde.Count = 200;
            horde.AutoAttack.Damage.Hits = 1000;
            horde.AutoAttack.Damage.Area = true;
            return horde;
        }

        [Test]
        public void Run_RejectsMoreCombatantRunsThanItCanSummarise()
        {
            var crowd = TestCombatants.Dummy("Crowd");
            crowd.Count = 200;
            var scenario = TestScenarios.TwoTeams(false, new[] { crowd }, new[] { TestCombatants.Dummy("Enemy", true) });

            var act = () => BatchRunner.Run(scenario, RuleSets.Current(), _catalog, 100_000, seed: 1);

            act.Should().Throw<ArgumentException>().WithMessage("*combatant-runs*");
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
        public void RecoveryRatio_IsHealingPlusAbsorbedOverHpDamage_WithHpCostsCountedAsHpLost()
        {
            var summary = new BatchSummary { TotalDamageToHp = 200, TotalHealing = 100, TotalAbsorbed = 50 };

            summary.RecoveryRatio.Should().BeApproximately(0.75, 1e-9);
            new BatchSummary { TotalDamageToHp = 200, TotalHitPointsSpent = 100, TotalHealing = 100, TotalAbsorbed = 50 }
                .RecoveryRatio.Should().BeApproximately(0.5, 1e-9);
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
