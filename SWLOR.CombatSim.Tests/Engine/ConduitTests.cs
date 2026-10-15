using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Effects;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Engine
{
    [TestFixture]
    public class ConduitTests
    {
        private const double LimitSeconds = 30.0;

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [Test]
        public void TestAbilities_HaveTheSkillsAndCostsTheseTestsRelyOn()
        {
            _catalog.Get(FeatType.FocusedArc4).SkillType.Should().Be(SkillType.Saberstaff);
            _catalog.Get(FeatType.FocusedArc4).STM.Should().Be(12);
            _catalog.Get(FeatType.ForceLightning3).SkillType.Should().Be(SkillType.Force);
            _catalog.Get(FeatType.ForceLightning3).FP.Should().Be(10);
            _catalog.Get(FeatType.CrossCut4).SkillType.Should().NotBe(SkillType.Saberstaff);
            _catalog.Get(FeatType.CrossCut4).STM.Should().Be(12);
        }

        [Test]
        public void ConduitTraining_ReturnsPartOfASaberstaffAbilitysStaminaAsFp()
        {
            var (simulation, caster) = Spawn(new[] { "ConduitTraining" }, Hostile("FocusedArc4"));
            caster.Fp = 0;

            UseAbilities(simulation, caster, 1);

            caster.Stats.FpRefunded.Should().Be(2, "20% of 12 STM, rounded down");
        }

        [Test]
        public void ConduitTraining_ReturnsPartOfAForceAbilitysFpAsStamina()
        {
            var (simulation, caster) = Spawn(new[] { "ConduitTraining" }, Hostile("ForceLightning3"));
            caster.Stm = 0;

            UseAbilities(simulation, caster, 1);

            caster.Stats.StmRefunded.Should().Be(2, "20% of 10 FP");
        }

        [Test]
        public void ConduitTraining_ReturnsNothingForAnotherWeaponsStamina()
        {
            var (simulation, caster) = Spawn(new[] { "ConduitTraining" }, Hostile("CrossCut4"));
            caster.Fp = 0;

            UseAbilities(simulation, caster, 1);

            caster.Stats.FpRefunded.Should().Be(0);
        }

        [Test]
        public void WithoutConduit_NothingIsReturned()
        {
            var (simulation, caster) = Spawn(Array.Empty<string>(), Hostile("FocusedArc4"), Hostile("ForceLightning3"));
            caster.Fp = 100;
            caster.Stm = 100;

            UseAbilities(simulation, caster, 2);

            caster.Stats.FpRefunded.Should().Be(0);
            caster.Stats.StmRefunded.Should().Be(0);
        }

        [Test]
        public void ConduitStance_AddsItsShareToThePassive()
        {
            var (simulation, caster) = Spawn(new[] { "ConduitTraining" }, Stance(), Hostile("FocusedArc4"));
            caster.Fp = 0;

            UseAbilities(simulation, caster, 2);

            caster.Statuses.Should().ContainKey("ConduitStanceStatusEffect");
            caster.Stats.FpRefunded.Should().Be(7, "20% + 40% of 12 STM, rounded down");
        }

        [Test]
        public void InfiniteConduit_StacksToTheCapForFortyFiveSeconds()
        {
            var (simulation, caster) = Spawn(new[] { "ConduitTraining" },
                Stance(), TestCombatants.Use("InfiniteConduit1", AbilityTarget.Self, keepUp: true), Hostile("FocusedArc4"));
            caster.Fp = 0;

            UseAbilities(simulation, caster, 3);

            // The capstone's own 15 STM is paid before its status lands, at 60%; Focused Arc then gets the 95% cap.
            caster.Stats.FpRefunded.Should().Be(9 + 11);

            SimulationClock.RunUntil(simulation, () => !caster.Statuses.ContainsKey("InfiniteConduitStatusEffect"), 60.0,
                "Infinite Conduit to end");
            var refunded = caster.Stats.FpRefunded;
            var used = caster.Stats.AbilitiesUsed;
            SimulationClock.RunUntil(simulation, () => caster.Stats.AbilitiesUsed > used, 120.0, "the next Focused Arc");
            (caster.Stats.FpRefunded - refunded).Should().Be(7, "back to the passive and the stance");
        }

        [Test]
        public void ConduitRestore_FollowsTheOrderTheAbilityPaysItsCosts()
        {
            _catalog.Get(FeatType.ThrowLightsaber3).PaysFpFirst.Should().BeTrue();
            _catalog.Get(FeatType.ThrowLightsaber3).SkillType.Should().Be(SkillType.Force);
            var (simulation, caster) = Spawn(new[] { "ConduitTraining" }, Hostile("ThrowLightsaber3"));

            UseAbilities(simulation, caster, 1);

            caster.Stats.StmRefunded.Should().Be(0,
                "the FP cost is paid and its STM restored while STM is still full; the STM cost comes after");
        }

        [Test]
        public void UsingAnActiveStanceAgain_TurnsItOffWithoutPaying()
        {
            var (simulation, caster) = Spawn(new[] { "ConduitTraining" }, TestCombatants.Use("ConduitStance1", AbilityTarget.Self));

            SimulationClock.RunUntil(simulation, () => caster.Statuses.ContainsKey("ConduitStanceStatusEffect"), LimitSeconds, "the stance to go up");
            SimulationClock.RunUntil(simulation, () => !caster.Statuses.ContainsKey("ConduitStanceStatusEffect"), 60.0,
                "the stance to be used again once off recast");

            caster.Stats.AbilitiesUsed.Should().Be(1, "turning a stance off is not a cast");
            SimulationClock.RunUntil(simulation, () => caster.Statuses.ContainsKey("ConduitStanceStatusEffect"), 60.0,
                "the stance to go back up on the next action, as nothing put it on recast");
        }

        [Test]
        public void TurningAStanceOff_CountsTowardItsMinimumInterval()
        {
            var (simulation, caster) = Spawn(new[] { "ConduitTraining" },
                TestCombatants.Use("ConduitStance1", AbilityTarget.Self, minIntervalSeconds: 60f));
            SimulationClock.RunUntil(simulation, () => caster.Statuses.ContainsKey("ConduitStanceStatusEffect"), LimitSeconds,
                "the stance to go up");
            SimulationClock.RunUntil(simulation, () => !caster.Statuses.ContainsKey("ConduitStanceStatusEffect"), 120.0,
                "the stance to be turned off once its interval has passed");
            var offAt = simulation.Now;

            SimulationClock.RunUntil(simulation, offAt + 50);

            caster.Statuses.Should().NotContainKey("ConduitStanceStatusEffect", "turning it off started a new 60-second interval");
        }

        [Test]
        public void AnUnaffordableUse_LeavesAnActiveStatusOn()
        {
            // A Capstone recast shorter than Infinite Conduit's 45 seconds lets it be used again while active.
            var rules = RuleSets.Current();
            rules.RecastOverrides["Capstone"] = 10f;
            var (simulation, caster) = Spawn(new[] { "ConduitTraining" }, rules, TestCombatants.Use("InfiniteConduit1", AbilityTarget.Self));
            SimulationClock.RunUntil(simulation, () => caster.Statuses.ContainsKey("InfiniteConduitStatusEffect"), LimitSeconds,
                "Infinite Conduit to go up");
            var upAt = simulation.Now;

            caster.Stm = 0;
            SimulationClock.RunUntil(simulation, upAt + 30);

            caster.Statuses.Should().ContainKey("InfiniteConduitStatusEffect",
                "the game checks its 15 STM before the toggle, so a use it can't afford doesn't turn it off");
        }

        [Test]
        public void TakingUpConduitStance_EndsAnyOtherStance()
        {
            var (simulation, caster) = Spawn(new[] { "ConduitTraining" }, Stance());
            // No other stance is modeled yet, so one stands in here alongside an ordinary status.
            caster.Statuses["OtherStance"] = new ActiveStatus
            {
                Stats = new Dictionary<StatType, int>(), ExpiresAt = double.PositiveInfinity, IsStance = true
            };
            caster.Statuses["OtherBuff"] = new ActiveStatus
            {
                Stats = new Dictionary<StatType, int>(), ExpiresAt = double.PositiveInfinity
            };

            SimulationClock.RunUntil(simulation, () => caster.Statuses.ContainsKey("ConduitStanceStatusEffect"), LimitSeconds,
                "Conduit Stance to go up");

            caster.Statuses.Should().NotContainKey("OtherStance", "a creature holds one stance at a time");
            caster.Statuses.Should().ContainKey("OtherBuff");
            caster.Statuses["ConduitStanceStatusEffect"].IsStance.Should().BeTrue();
        }

        [Test]
        public void InfiniteConduit_IsNotAStance()
        {
            StatusStats.IsStance(typeof(InfiniteConduitStatusEffect)).Should().BeFalse();
        }

        [Test]
        public void ASelfStatus_MustTargetItsCaster()
        {
            var scenario = TestScenarios.TwoTeams(false,
                new[] { TestCombatants.Medic("Caster", TestCombatants.Use("ConduitStance1", AbilityTarget.LowestAlly, keepUp: true)) },
                new[] { TestCombatants.Dummy("Enemy", true) });
            var simulation = new Simulation(scenario, RuleSets.Current(), _catalog, 1);

            var act = () => simulation.Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*Target must be Self*");
        }

        private static AbilityUseSpec Stance() => TestCombatants.Use("ConduitStance1", AbilityTarget.Self, keepUp: true);

        private static AbilityUseSpec Hostile(string feat)
        {
            var use = TestCombatants.Use(feat, AbilityTarget.Enemy);
            use.Damage = new DamageSpec { Min = 1f, Max = 1f, HitChance = 1f };
            return use;
        }

        private static void UseAbilities(Simulation simulation, Combatant caster, int count) =>
            SimulationClock.RunUntil(simulation, () => caster.Stats.AbilitiesUsed >= count, LimitSeconds, $"{count} abilities to be used");

        private (Simulation Simulation, Combatant Caster) Spawn(string[] perks, params AbilityUseSpec[] abilities) =>
            Spawn(perks, RuleSets.Current(), abilities);

        private (Simulation Simulation, Combatant Caster) Spawn(string[] perks, RuleSet rules, params AbilityUseSpec[] abilities)
        {
            // Large pools with no regeneration, so every point restored is Conduit's.
            var caster = new CombatantSpec
            {
                Name = "Caster",
                MaxHp = 1000,
                MaxFp = 1000,
                MaxStm = 1000,
                Perks = perks.ToList(),
                Abilities = abilities.ToList()
            };
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { caster }, new[] { TestCombatants.Dummy("Enemy", true) }),
                rules,
                _catalog);
            return (simulation, simulation.Get("Caster"));
        }
    }
}
