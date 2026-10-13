using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;

namespace SWLOR.CombatSim.Tests.Engine
{
    [TestFixture]
    public class DefensiveHarmonyTests
    {
        private const int HarmonyStamina = 8;
        private const int HealAmount = 100;
        private const int HealAttempts = 600;
        private const double WoundedHp = 1000;
        private const double StartingStamina = 500_000;

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [Test]
        public void Wave1_HealOverTimeTicksNeverRefundStamina()
        {
            var (simulation, tank, healer) = SpawnTank(RuleSets.Wave1());

            var refundTimes = HealOncePerSecond(simulation, tank, healer, HealKind.OverTime);

            refundTimes.Should().BeEmpty();
            tank.Stats.StmRefunded.Should().Be(0);
            tank.Stats.HealingReceived.Should().Be(HealAmount * HealAttempts, "every tick really healed, so the missing refunds are the rule and not an overheal");
        }

        [Test]
        public void CurrentRules_HealOverTimeTicksCanRefundStamina()
        {
            var (simulation, tank, healer) = SpawnTank(RuleSets.Current());

            var refundTimes = HealOncePerSecond(simulation, tank, healer, HealKind.OverTime);

            refundTimes.Should().NotBeEmpty();
            tank.Stats.StmRefunded.Should().Be(HarmonyStamina * refundTimes.Count);
        }

        [Test]
        public void Wave1_InstantAbilityHealsStillRefundStamina_AtMostOncePerSixSeconds()
        {
            var (simulation, tank, healer) = SpawnTank(RuleSets.Wave1());

            var refundTimes = HealOncePerSecond(simulation, tank, healer, HealKind.InstantAbility);

            refundTimes.Should().NotBeEmpty("instant ability heals are what Defensive Harmony is kept for");
            refundTimes.Count.Should().BeLessThanOrEqualTo(HealAttempts / 6);
            GapsBetween(refundTimes).Should().OnlyContain(gap => gap >= 6.0);
        }

        [Test]
        public void CurrentRules_InstantAbilityHealsCanRefundWithoutACooldown()
        {
            var (simulation, tank, healer) = SpawnTank(RuleSets.Current());

            var refundTimes = HealOncePerSecond(simulation, tank, healer, HealKind.InstantAbility);

            refundTimes.Count.Should().BeGreaterThan(HealAttempts / 6, "without the cooldown the 40% chance applies to every heal");
            GapsBetween(refundTimes).Min().Should().BeLessThan(6.0);
        }

        [TestCase("current")]
        [TestCase("wave1")]
        public void NaturalRegeneration_NeverRefundsStamina(string ruleSetName)
        {
            var (simulation, tank, healer) = SpawnTank(RuleSets.Get(ruleSetName));

            var refundTimes = HealOncePerSecond(simulation, tank, healer, HealKind.NaturalRegeneration);

            refundTimes.Should().BeEmpty();
        }

        [Test]
        public void WithoutThePerk_NoHealRefundsStamina()
        {
            var (simulation, tank, healer) = SpawnTank(RuleSets.Current(), withPerk: false);

            HealOncePerSecond(simulation, tank, healer, HealKind.InstantAbility).Should().BeEmpty();
            HealOncePerSecond(simulation, tank, healer, HealKind.OverTime).Should().BeEmpty();
        }

        [Test]
        public void Wave1_ARealInfusionNeverRefundsStaminaThroughItsTicks()
        {
            var (simulation, tank) = SpawnSelfInfusingTank(RuleSets.Wave1());

            RunKeepingTankWounded(simulation, tank, 300);

            tank.Stats.HealingReceived.Should().BeGreaterThan(50 * 30_000, "many Infusion ticks landed");
            tank.Stats.StmRefunded.Should().Be(0);
        }

        [Test]
        public void CurrentRules_ARealInfusionRefundsStaminaThroughItsTicks()
        {
            var (simulation, tank) = SpawnSelfInfusingTank(RuleSets.Current());

            RunKeepingTankWounded(simulation, tank, 300);

            tank.Stats.StmRefunded.Should().BeGreaterThan(0);
        }

        private (Simulation Simulation, Combatant Tank, Combatant Healer) SpawnTank(RuleSet rules, bool withPerk = true)
        {
            var tank = new CombatantSpec
            {
                Name = "Tank",
                MaxHp = TestCombatants.EffectivelyUnlimited,
                Might = 30,
                MaxStm = TestCombatants.EffectivelyUnlimited,
                Perks = withPerk ? new List<string> { Combatant.DefensiveHarmonyPerk } : new List<string>()
            };
            var healer = new CombatantSpec { Name = "Healer", MaxHp = 1000 };
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { tank, healer }, new[] { TestCombatants.Dummy("Enemy", true) }),
                rules,
                _catalog);

            var spawned = simulation.Get("Tank");
            spawned.Hp = WoundedHp;
            spawned.Stm = StartingStamina;
            return (simulation, spawned, simulation.Get("Healer"));
        }

        private (Simulation Simulation, Combatant Tank) SpawnSelfInfusingTank(RuleSet rules)
        {
            var tank = new CombatantSpec
            {
                Name = "Tank",
                MaxHp = TestCombatants.EffectivelyUnlimited,
                Might = 30,
                Willpower = 20,
                MaxStm = TestCombatants.EffectivelyUnlimited,
                MaxFp = TestCombatants.EffectivelyUnlimited,
                Perks = new List<string> { Combatant.DefensiveHarmonyPerk },
                Abilities = new List<AbilityUseSpec> { TestCombatants.Use("Infusion2", AbilityTarget.Self, keepUp: true) }
            };
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { tank }, new[] { TestCombatants.Dummy("Enemy", true) }),
                rules,
                _catalog);

            var spawned = simulation.Get("Tank");
            spawned.Stm = StartingStamina;
            return (simulation, spawned);
        }

        /// <summary>Heals the wounded tank once a second and returns the seconds at which Defensive Harmony refunded stamina.</summary>
        private static List<double> HealOncePerSecond(Simulation simulation, Combatant tank, Combatant healer, HealKind kind)
        {
            var refundTimes = new List<double>();
            for (var second = 0; second < HealAttempts; second++)
            {
                SimulationClock.Set(simulation, second);
                var refundedBefore = tank.Stats.StmRefunded;

                simulation.ApplyHealing(healer, tank, HealAmount, kind);

                if (tank.Stats.StmRefunded > refundedBefore)
                    refundTimes.Add(second);
            }

            return refundTimes;
        }

        private static void RunKeepingTankWounded(Simulation simulation, Combatant tank, double seconds)
        {
            while (simulation.Now < seconds)
            {
                tank.Hp = WoundedHp;
                SimulationClock.RunUntil(simulation, simulation.Now + 0.1);
            }
        }

        private static IEnumerable<double> GapsBetween(IReadOnlyList<double> times) =>
            times.Zip(times.Skip(1), (earlier, later) => later - earlier);
    }
}
