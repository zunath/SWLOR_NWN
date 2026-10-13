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
    public class PartyTargetLimitTests
    {
        private const string ZoneKey = "ForceSanctuary";

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [Test]
        public void ZoneHeal_WithMaxTargets_PulsesOnlyTheLowestHpAllies()
        {
            var simulation = SpawnTeam(ZoneHeal(maxTargets: 2), "Wounded 1", "Wounded 2", "Wounded 3", "Wounded 4");
            SetHpPercent(simulation, ("Wounded 1", 40), ("Wounded 2", 10), ("Wounded 3", 30), ("Wounded 4", 20));
            var before = HpByName(simulation);

            RunUntilPulses(simulation, 1);

            HealedNames(simulation, before).Should().BeEquivalentTo("Wounded 2", "Wounded 4");
        }

        [Test]
        public void ZoneHeal_WithoutMaxTargets_PulsesEveryLivingAlly()
        {
            var simulation = SpawnTeam(ZoneHeal(maxTargets: null), "Wounded 1", "Wounded 2", "Wounded 3", "Wounded 4");
            SetHpPercent(simulation, ("Wounded 1", 40), ("Wounded 2", 10), ("Wounded 3", 30), ("Wounded 4", 20));
            var before = HpByName(simulation);

            RunUntilPulses(simulation, 1);

            HealedNames(simulation, before).Should().BeEquivalentTo("Wounded 1", "Wounded 2", "Wounded 3", "Wounded 4");
        }

        [Test]
        public void ZoneHeal_WithMoreTargetsThanAllies_ReachesEveryone()
        {
            var simulation = SpawnTeam(ZoneHeal(maxTargets: 9), "Wounded 1", "Wounded 2");
            SetHpPercent(simulation, ("Wounded 1", 40), ("Wounded 2", 10));
            var before = HpByName(simulation);

            RunUntilPulses(simulation, 1);

            HealedNames(simulation, before).Should().BeEquivalentTo("Wounded 1", "Wounded 2");
        }

        [Test]
        public void ZoneHeal_BreaksHpTiesRandomly()
        {
            var healed = new List<string>();
            for (var seed = 1; seed <= 40; seed++)
            {
                var simulation = SpawnTeam(ZoneHeal(maxTargets: 1), seed, "Wounded 1", "Wounded 2", "Wounded 3", "Wounded 4");
                SetHpPercent(simulation, ("Wounded 1", 50), ("Wounded 2", 50), ("Wounded 3", 50), ("Wounded 4", 50));
                var before = HpByName(simulation);

                RunUntilPulses(simulation, 1);

                healed.Add(HealedNames(simulation, before).Should().ContainSingle().Subject);
            }

            healed.Distinct().Count().Should().BeGreaterThanOrEqualTo(3, "no single ally is always first in line when HP is tied");
        }

        [Test]
        public void ZoneHeal_PicksAgainOnEveryPulse()
        {
            var simulation = SpawnTeam(ZoneHeal(maxTargets: 1), "First", "Second", "Third");
            SetHpPercent(simulation, ("First", 10), ("Second", 50), ("Third", 50));
            RunUntilPulses(simulation, 1);
            var firstAfterPulse = simulation.Get("First").Hp;
            firstAfterPulse.Should().BeGreaterThan(100);
            simulation.Get("Second").Hp.Should().Be(500);

            simulation.Get("Second").Hp = 1;
            RunUntilPulses(simulation, 2);

            simulation.Get("Second").Hp.Should().BeGreaterThan(1, "the second pulse went to whoever was lowest then");
            simulation.Get("First").Hp.Should().Be(firstAfterPulse);
            simulation.Get("Third").Hp.Should().Be(500);
        }

        [Test]
        public void InstantPartyHeal_WithMaxTargets_ReachesOnlyTheLowestHpAlly()
        {
            var use = TestCombatants.Use("PurifyingWave1", AbilityTarget.Self, maxTargets: 1);
            var simulation = SpawnTeam(use, "Wounded 1", "Wounded 2");
            SetHpPercent(simulation, ("Wounded 1", 60), ("Wounded 2", 20));
            var before = HpByName(simulation);

            SimulationClock.RunUntil(simulation, () => simulation.Get("Medic").Stats.AbilitiesUsed == 1, 10, "party heal cast");

            HealedNames(simulation, before).Should().BeEquivalentTo("Wounded 2");
        }

        [Test]
        public void PartyShield_WithMaxTargets_ShieldsOnlyTheLowestHpAllies()
        {
            var simulation = SpawnTeam(TestCombatants.Use("GroupDeflector1", AbilityTarget.Self, maxTargets: 2), "Wounded 1", "Wounded 2", "Wounded 3", "Wounded 4");
            SetHpPercent(simulation, ("Wounded 1", 40), ("Wounded 2", 10), ("Wounded 3", 30), ("Wounded 4", 20));

            SimulationClock.RunUntil(simulation, () => simulation.Get("Medic").Stats.AbilitiesUsed == 1, 10, "party shield cast");

            ShieldedNames(simulation).Should().BeEquivalentTo("Wounded 2", "Wounded 4");
        }

        [Test]
        public void PartyShield_WithoutMaxTargets_ShieldsEveryAlly()
        {
            var simulation = SpawnTeam(TestCombatants.Use("GroupDeflector1", AbilityTarget.Self), "Wounded 1", "Wounded 2", "Wounded 3");

            SimulationClock.RunUntil(simulation, () => simulation.Get("Medic").Stats.AbilitiesUsed == 1, 10, "party shield cast");

            ShieldedNames(simulation).Should().BeEquivalentTo("Medic", "Wounded 1", "Wounded 2", "Wounded 3");
        }

        [TestCase("MedKit4", 2, TestName = "Spawn_RejectsMaxTargetsOnASingleTargetHeal")]
        [TestCase("GroupDeflector1", 0, TestName = "Spawn_RejectsMaxTargetsBelowOne")]
        public void Spawn_RejectsInvalidMaxTargets(string feat, int maxTargets)
        {
            var scenario = TestScenarios.TwoTeams(
                false,
                new[] { TestCombatants.Medic("Medic", TestCombatants.Use(feat, AbilityTarget.Self, maxTargets: maxTargets)) },
                new[] { TestCombatants.Dummy("Enemy", true) });
            var simulation = new Simulation(scenario, RuleSets.Current(), _catalog, 1);

            var act = () => simulation.Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*MaxTargets*");
        }

        [Test]
        public void Spawn_RejectsMaxTargetsOnADamageProfile()
        {
            var use = TestCombatants.Use("PurifyingWave1", AbilityTarget.Enemy, maxTargets: 3);
            use.Damage = new DamageSpec { Min = 1f, Max = 1f };
            var scenario = TestScenarios.TwoTeams(false, new[] { TestCombatants.Medic("Medic", use) }, new[] { TestCombatants.Dummy("Enemy", true) });
            var simulation = new Simulation(scenario, RuleSets.Current(), _catalog, 1);

            var act = () => simulation.Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*MaxTargets*");
        }

        private static AbilityUseSpec ZoneHeal(int? maxTargets) =>
            TestCombatants.Use("ForceSanctuary1", AbilityTarget.Self, maxTargets: maxTargets);

        private Simulation SpawnTeam(AbilityUseSpec use, params string[] allies) => SpawnTeam(use, 1, allies);

        private Simulation SpawnTeam(AbilityUseSpec use, int seed, params string[] allies)
        {
            var members = new List<CombatantSpec> { TestCombatants.Medic("Medic", use) };
            members.AddRange(allies.Select(name => new CombatantSpec { Name = name, MaxHp = 1000 }));
            return TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, members, new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.Current(),
                _catalog,
                seed);
        }

        private static void SetHpPercent(Simulation simulation, params (string Name, int Percent)[] values)
        {
            foreach (var (name, percent) in values)
                simulation.Get(name).Hp = percent * 10;
        }

        private static Dictionary<string, double> HpByName(Simulation simulation) =>
            simulation.Combatants.ToDictionary(c => c.Name, c => c.Hp);

        private static IEnumerable<string> HealedNames(Simulation simulation, Dictionary<string, double> before) =>
            simulation.Combatants.Where(c => c.Hp > before[c.Name]).Select(c => c.Name).ToList();

        private static IEnumerable<string> ShieldedNames(Simulation simulation) =>
            simulation.Combatants.Where(c => c.TemporaryHp.Count > 0).Select(c => c.Name).ToList();

        private static void RunUntilPulses(Simulation simulation, int pulses)
        {
            var medic = simulation.Get("Medic");
            SimulationClock.RunUntil(
                simulation,
                () => medic.PartyPulses.TryGetValue(ZoneKey, out var pulse) && pulse.TicksLeft <= pulse.Effect.Ticks - pulses,
                30,
                $"{pulses} zone pulse(s) applied");
        }
    }
}
