using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.CombatSim.Tests.Engine
{
    [TestFixture]
    public class ForceConvergenceTests
    {
        private const int MaxFp = 200;
        private const int FpPerTick = 8;
        private const int StartingFp = 10;
        private const int BenevolenceIFpCost = 3;
        private const double TickSeconds = 2.0;

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [Test]
        public void CurrentRules_RestoreSixtyPercentOfMaxFpOverThirtySeconds()
        {
            var (simulation, caster) = SpawnCaster(RuleSets.Current(), withPerk: true);

            var ticks = CastOnceAndCollectRefundTicks(simulation, caster, out var castAt);

            ticks.Should().HaveCount(15);
            caster.Stats.FpRefunded.Should().Be(0.6 * MaxFp);
            caster.Fp.Should().Be(StartingFp - BenevolenceIFpCost + 0.6 * MaxFp);
            (ticks[^1] - castAt).Should().BeInRange(29.9, 30.0 + TickSeconds);
        }

        [Test]
        public void Tuned_RestoresFortyPercentOfMaxFpOverTwentySeconds()
        {
            var (simulation, caster) = SpawnCaster(RuleSets.Tuned(), withPerk: true);

            var ticks = CastOnceAndCollectRefundTicks(simulation, caster, out var castAt);

            ticks.Should().HaveCount(10);
            caster.Stats.FpRefunded.Should().Be(0.4 * MaxFp);
            caster.Fp.Should().Be(StartingFp - BenevolenceIFpCost + 0.4 * MaxFp);
            (ticks[^1] - castAt).Should().BeInRange(19.9, 20.0 + TickSeconds);
        }

        [Test]
        public void RefundsArrive_EveryTwoSeconds()
        {
            var (simulation, caster) = SpawnCaster(RuleSets.Current(), withPerk: true);

            var ticks = CastOnceAndCollectRefundTicks(simulation, caster, out _);

            for (var i = 1; i < ticks.Count; i++)
                (ticks[i] - ticks[i - 1]).Should().BeInRange(TickSeconds - 0.15, TickSeconds + 0.15);
        }

        [TestCase("current")]
        [TestCase("tuned")]
        public void WithoutThePerk_NothingIsRestored(string ruleSetName)
        {
            var (simulation, caster) = SpawnCaster(RuleSets.Get(ruleSetName), withPerk: false);

            var ticks = CastOnceAndCollectRefundTicks(simulation, caster, out _);

            ticks.Should().BeEmpty();
            caster.Stats.FpRefunded.Should().Be(0);
            caster.Fp.Should().Be(StartingFp - BenevolenceIFpCost);
        }

        [Test]
        public void AbilityThatCostsOnlyStamina_DoesNotTriggerConvergence()
        {
            var (simulation, caster) = SpawnCaster(RuleSets.Current(), withPerk: true, feat: "MedKit1");

            var ticks = CastOnceAndCollectRefundTicks(simulation, caster, out _);

            ticks.Should().BeEmpty();
            caster.Stats.AbilitiesUsed.Should().Be(1);
        }

        [Test]
        public void CastingAgainDuringTheWindow_DoesNotExtendOrRepeatIt()
        {
            var (simulation, caster) = SpawnCaster(RuleSets.Current(), withPerk: true);
            SimulationClock.RunUntil(simulation, () => caster.Stats.AbilitiesUsed == 1, 10, "first cast resolved");

            // Wounded again 14 seconds in, so the same Benevolence is cast a second time inside the 45 second cooldown.
            SimulationClock.RunUntil(simulation, 14);
            caster.Hp = 940;
            SimulationClock.RunUntil(simulation, () => caster.Stats.AbilitiesUsed == 2, 30, "second cast resolved");
            SimulationClock.RunUntil(simulation, 40);

            caster.Stats.FpRefunded.Should().Be(0.6 * MaxFp, "the second cast did not start a second window or lengthen the first");
        }

        [Test]
        public void FpCostOnANonForceAbility_DoesNotTriggerConvergence()
        {
            var strike = TestCombatants.Use("ShatteringStrike1", AbilityTarget.Enemy, minIntervalSeconds: 600);
            strike.Damage = new DamageSpec { Min = 1, Max = 1 };
            var (simulation, caster) = SpawnAttacker(strike, withPerk: true);

            SimulationClock.RunUntil(simulation, 40);

            caster.Stats.FpSpent.Should().BeGreaterThan(0, "Shattering Strike costs FP");
            caster.Stats.FpRefunded.Should().Be(0, "only Force abilities trigger Force Convergence");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ForceCastsAfterTheTrigger_GetFivePercentMoreHitChance(bool withPerk)
        {
            // A profile that never hits on its own, so every landed hit comes from the bonus.
            var spark = TestCombatants.Use("ForceSpark1", AbilityTarget.Enemy);
            spark.Damage = new DamageSpec { Min = 1, Max = 1, HitChance = 0f, Hits = 200 };
            var (simulation, _) = SpawnAttacker(spark, withPerk);

            SimulationClock.RunUntil(simulation, 29);

            var landed = simulation.Get("Enemy").Stats.DamageTakenToHp;
            if (withPerk)
                landed.Should().BeGreaterThan(0, "casts after the first fall inside the 30-second accuracy window");
            else
                landed.Should().Be(0);
        }

        [Test]
        public void Death_EndsTheAccuracyWindowWithTheRestOfTheBuff()
        {
            var spark = TestCombatants.Use("ForceSpark1", AbilityTarget.Enemy, minIntervalSeconds: 600);
            spark.Damage = new DamageSpec { Min = 1, Max = 1 };
            var (simulation, caster) = SpawnAttacker(spark, withPerk: true);
            SimulationClock.RunUntil(simulation, () => caster.ConvergenceAccuracyUntil > simulation.Now, 10, "Convergence triggered");

            simulation.ApplyDamage(simulation.Get("Enemy"), caster, TestCombatants.EffectivelyUnlimited);

            caster.ConvergenceAccuracyUntil.Should().BeLessThan(simulation.Now);
        }

        [Test]
        public void AZeroFpCostMultiplier_StillTriggersConvergence_FromTheDeclaredRequirement()
        {
            var rules = RuleSets.Tuned();
            rules.FpCostMultiplier = 0f;
            var lightning = TestCombatants.Use("ForceLightning3", AbilityTarget.Enemy, minIntervalSeconds: 600);
            lightning.Damage = new DamageSpec { Min = 1f, Max = 1f, HitChance = 1f };
            var caster = new CombatantSpec
            {
                Name = "Caster",
                MaxHp = 1000,
                MaxFp = MaxFp,
                MaxStm = 100,
                Perks = new List<string> { nameof(PerkType.ForceConvergence) },
                Abilities = new List<AbilityUseSpec> { lightning }
            };
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { caster }, new[] { TestCombatants.Dummy("Enemy", true) }), rules, _catalog);
            var spawned = simulation.Get("Caster");

            SimulationClock.RunUntil(simulation, () => spawned.Stats.AbilitiesUsed == 1, 10, "Force Lightning cast");

            spawned.Stats.FpSpent.Should().Be(0, "the multiplier made the cast free");
            spawned.ConvergenceUntil.Should().BeGreaterThan(simulation.Now, "the ability still declares an FP requirement");
        }

        private (Simulation Simulation, Combatant Caster) SpawnAttacker(AbilityUseSpec use, bool withPerk)
        {
            var caster = new CombatantSpec
            {
                Name = "Caster",
                MaxHp = 1000,
                MaxFp = MaxFp,
                MaxStm = 100,
                Perks = withPerk ? new List<string> { nameof(PerkType.ForceConvergence) } : new List<string>(),
                Abilities = new List<AbilityUseSpec> { use }
            };
            var scenario = TestScenarios.TwoTeams(false, new[] { caster }, new[] { TestCombatants.Dummy("Enemy", true) });
            var simulation = TestScenarios.Spawn(scenario, RuleSets.Tuned(), _catalog);
            var spawned = simulation.Get("Caster");
            spawned.Fp = StartingFp + 100;
            return (simulation, spawned);
        }

        private (Simulation Simulation, Combatant Caster) SpawnCaster(RuleSet rules, bool withPerk, string feat = "Benevolence1")
        {
            var caster = new CombatantSpec
            {
                Name = "Caster",
                MaxHp = 1000,
                MaxFp = MaxFp,
                FpRegenPer30Seconds = 0,
                MaxStm = 100,
                StmRegenPer30Seconds = 0,
                Perks = withPerk ? new List<string> { nameof(PerkType.ForceConvergence) } : new List<string>(),
                Abilities = new List<AbilityUseSpec> { TestCombatants.Use(feat, AbilityTarget.Self, belowHpPercent: 99) }
            };
            var scenario = TestScenarios.TwoTeams(false, new[] { caster }, new[] { TestCombatants.Dummy("Enemy", true) });
            var simulation = TestScenarios.Spawn(scenario, rules, _catalog);

            // Wounded just enough that one heal tops the caster up, so exactly one ability is cast, and low on FP so refunds have room.
            var spawned = simulation.Get("Caster");
            spawned.Hp = 985;
            spawned.Fp = StartingFp;
            spawned.Stm = 50;
            return (simulation, spawned);
        }

        /// <summary>Runs for a minute after the first cast and returns the times at which FP was refunded.</summary>
        private static List<double> CastOnceAndCollectRefundTicks(Simulation simulation, Combatant caster, out double castAt)
        {
            SimulationClock.RunUntil(simulation, () => caster.Stats.AbilitiesUsed == 1, 10, "first cast resolved");
            castAt = simulation.Now;

            var ticks = new List<double>();
            var lastRefunded = caster.Stats.FpRefunded;
            while (simulation.Now < castAt + 60)
            {
                SimulationClock.RunUntil(simulation, simulation.Now + 0.1);
                if (caster.Stats.FpRefunded > lastRefunded)
                {
                    ticks.Add(simulation.Now);
                    lastRefunded = caster.Stats.FpRefunded;
                }
            }

            return ticks;
        }
    }
}
