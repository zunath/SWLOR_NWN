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
    public class HitPointCostTests
    {
        private const int BladeMaxHp = 1000;
        private const int BladeMaxStm = 1000;

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [TestCase(10, 300.0)]
        [TestCase(35, 100.0)]
        public void SoulBurst_PaysMaxHpPercentLessMight_DownToItsFloor(int might, double expectedCost)
        {
            var simulation = Spawn(RuleSets.Current(), might);
            var blade = simulation.Get("Blade");

            CastOnce(simulation, blade);

            blade.Hp.Should().BeApproximately(BladeMaxHp - expectedCost, 1e-6);
            blade.Stats.HitPointsSpent.Should().BeApproximately(expectedCost, 1e-6);
            blade.Stats.DamageTakenToHp.Should().Be(0, "an HP cost is not damage taken");
        }

        [Test]
        public void TheCost_RoundsUpLikeTheGamesPercentOf()
        {
            var simulation = Spawn(RuleSets.Current(), 10, maxHp: 1001);
            var blade = simulation.Get("Blade");

            CastOnce(simulation, blade);

            blade.Stats.HitPointsSpent.Should().Be(301, "30% of 1,001 is 300.3, which rounds up");
        }

        [Test]
        public void TemporaryHp_PaysFirst()
        {
            var simulation = Spawn(RuleSets.Current(), 10);
            var blade = simulation.Get("Blade");
            blade.TemporaryHp.Add(new TemporaryHpPool { Key = "Shield", Amount = 100, ExpiresAt = 600 });

            CastOnce(simulation, blade);

            blade.TemporaryHp.Should().BeEmpty();
            blade.Hp.Should().BeApproximately(BladeMaxHp - 200, 1e-6);
            blade.Stats.HitPointsSpent.Should().BeApproximately(200, 1e-6);
        }

        [Test]
        public void CurrentRules_LandTheAbilityUnpaidAtOneHp()
        {
            var simulation = Spawn(RuleSets.Current(), 10);
            var blade = simulation.Get("Blade");
            blade.Hp = 1;

            CastOnce(simulation, blade);

            blade.Hp.Should().Be(1);
            blade.Stats.HitPointsSpent.Should().Be(0);
        }

        [Test]
        public void TunedRules_RefuseTheAbilityAtOneHp()
        {
            var simulation = Spawn(RuleSets.Tuned(), 10);
            var blade = simulation.Get("Blade");
            blade.Hp = 1;

            SimulationClock.RunUntil(simulation, 5);

            blade.Stats.AbilitiesUsed.Should().Be(0);
        }

        // Bloodlust at Might 10: 15% + 10 = 25% of 1,000 STM. The cost is 300 HP.
        [TestCase(RuleSets.CurrentName, 0, 250)]
        [TestCase(RuleSets.CurrentName, 100, 250)]
        [TestCase(RuleSets.CurrentName, 400, 250)]
        [TestCase(RuleSets.TunedName, 0, 250)]
        [TestCase(RuleSets.TunedName, 100, 167)]
        [TestCase(RuleSets.TunedName, 400, 0)]
        public void Bloodlust_RestoresStamina_ScaledByHpLostOnlyUnderTheTunedRules(string rules, int shield, int expectedStamina)
        {
            var simulation = Spawn(RuleSets.Get(rules), 10, withBloodlust: true);
            var blade = simulation.Get("Blade");
            blade.Stm = BladeMaxStm / 2.0;
            if (shield > 0)
                blade.TemporaryHp.Add(new TemporaryHpPool { Key = "Shield", Amount = shield, ExpiresAt = 600 });

            CastOnce(simulation, blade);

            blade.Stats.StmRefunded.Should().Be(expectedStamina);
        }

        private Simulation Spawn(RuleSet rules, int might, bool withBloodlust = false, int maxHp = BladeMaxHp)
        {
            var burst = TestCombatants.Use("SoulBurst1", AbilityTarget.Enemy, minIntervalSeconds: 600);
            burst.Damage = new DamageSpec { Min = 1f, Max = 1f, HitChance = 1f };
            var blade = new CombatantSpec
            {
                Name = "Blade",
                MaxHp = maxHp,
                Might = might,
                MaxFp = TestCombatants.EffectivelyUnlimited,
                MaxStm = BladeMaxStm,
                Perks = withBloodlust ? new List<string> { nameof(PerkType.Bloodlust) } : new List<string>(),
                Abilities = new List<AbilityUseSpec> { burst }
            };
            return TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { blade }, new[] { TestCombatants.Dummy("Enemy", true) }),
                rules,
                _catalog);
        }

        private static void CastOnce(Simulation simulation, Combatant blade) =>
            SimulationClock.RunUntil(simulation, () => blade.Stats.AbilitiesUsed == 1, 10, "Soul Burst resolved");
    }
}
