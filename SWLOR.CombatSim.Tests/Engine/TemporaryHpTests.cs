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
    public class TemporaryHpTests
    {
        // Six different shield keys: Guardian Ward, Pain Suppressant, Deflector Shield, Group Deflector, Rousing Shout, Cleanse Order.
        private static readonly string[] DifferentShields =
        {
            "GuardianWard4", "PainSuppressant2", "DeflectorShield3", "GroupDeflector1", "RousingShout3", "CleanseOrder2"
        };

        private const double ShieldingWindowSeconds = 25.0;

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [Test]
        public void Wave1_CapsTotalTemporaryHpAtHalfOfMaxHp_EvenAfterSeveralDifferentShields()
        {
            var simulation = Spawn(RuleSets.Wave1(), new[] { TestCombatants.Medic("Medic", SelfKeepUp(DifferentShields)) });
            var medic = simulation.Get("Medic");

            var peak = RunAndTrackPeakTemporaryHp(simulation, medic, ShieldingWindowSeconds);

            peak.Should().Be(500.0, "the 50% cap of a 1,000 HP target is reached by the first few shields and never exceeded");
            medic.Stats.AbilitiesUsed.Should().BeGreaterThan(3, "the medic kept casting shields after the cap was full");
        }

        [Test]
        public void Wave1_CapUsesTheRecipientsMaxHp()
        {
            var medic = TestCombatants.Medic("Medic", DifferentShields
                .Select(feat => TestCombatants.Use(feat, AbilityTarget.NamedAlly, keepUp: true, targetName: "Tank"))
                .ToArray());
            var simulation = Spawn(RuleSets.Wave1(), new[] { medic, new CombatantSpec { Name = "Tank", MaxHp = 400 } });

            var peak = RunAndTrackPeakTemporaryHp(simulation, simulation.Get("Tank"), ShieldingWindowSeconds);

            peak.Should().Be(200.0);
        }

        [Test]
        public void CurrentRules_StackShieldsFromDifferentKeysPastHalfOfMaxHp()
        {
            var simulation = Spawn(RuleSets.Current(), new[] { TestCombatants.Medic("Medic", SelfKeepUp(DifferentShields)) });
            var medic = simulation.Get("Medic");

            var peak = RunAndTrackPeakTemporaryHp(simulation, medic, ShieldingWindowSeconds);

            peak.Should().BeGreaterThan(500.0);
            medic.TemporaryHp.Select(p => p.Key).Distinct().Should().HaveCount(DifferentShields.Length);
        }

        [TestCase("current")]
        [TestCase("wave1")]
        public void CastingTheSameShieldAgain_ReplacesTheOldOneInsteadOfStacking(string ruleSetName)
        {
            var simulation = Spawn(RuleSets.Get(ruleSetName), new[]
            {
                TestCombatants.Medic("Medic", TestCombatants.Use("PainSuppressant1", AbilityTarget.Self))
            });
            var medic = simulation.Get("Medic");
            SimulationClock.RunUntil(simulation, () => medic.TemporaryHp.Count == 1, 10, "first shield applied");
            var first = medic.TemporaryHp.Single();

            RequestImmediateRecast(simulation, medic);
            SimulationClock.RunUntil(simulation, () => medic.Stats.AbilitiesUsed == 2, 10, "second cast resolved");

            var shield = medic.TemporaryHp.Should().ContainSingle().Subject;
            shield.Should().NotBeSameAs(first);
            shield.Key.Should().Be(first.Key);
            shield.Amount.Should().Be(first.Amount, "the new shield replaced the old one rather than adding to it");
            shield.ExpiresAt.Should().BeGreaterThan(first.ExpiresAt, "the replacement restarts the duration");
        }

        [Test]
        public void Wave1_ReplacingAShieldIsNotShrunkByTheShieldItReplaces()
        {
            // Pain Suppressant and Deflector Shield fill all but 92 of the cap; Guardian Ward takes that remainder.
            // Recasting Guardian Ward must get the same 92 back, not 0 because its old pool still counted against the cap.
            var simulation = Spawn(RuleSets.Wave1(), new[]
            {
                TestCombatants.Medic("Medic",
                    TestCombatants.Use("PainSuppressant2", AbilityTarget.Self, keepUp: true),
                    TestCombatants.Use("DeflectorShield3", AbilityTarget.Self, keepUp: true),
                    TestCombatants.Use("GuardianWard4", AbilityTarget.Self))
            });
            var medic = simulation.Get("Medic");
            SimulationClock.RunUntil(simulation, () => medic.TemporaryHp.Count == 3, 15, "three shields applied");
            var ward = medic.TemporaryHp.Single(p => p.Key == "GuardianWard");
            ward.Amount.Should().Be(92.0);
            medic.TemporaryHpTotal.Should().Be(500.0);

            RequestImmediateRecast(simulation, medic);
            SimulationClock.RunUntil(simulation, () => medic.Stats.AbilitiesUsed == 4, 10, "fourth cast resolved");

            var replacement = medic.TemporaryHp.Single(p => p.Key == "GuardianWard");
            replacement.Should().NotBeSameAs(ward);
            replacement.Amount.Should().Be(92.0);
            medic.TemporaryHpTotal.Should().Be(500.0);
        }

        private Simulation Spawn(RuleSet rules, IEnumerable<CombatantSpec> firstTeam) =>
            TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, firstTeam, new[] { TestCombatants.Dummy("Enemy", true) }),
                rules,
                _catalog);

        private static AbilityUseSpec[] SelfKeepUp(IEnumerable<string> feats) =>
            feats.Select(feat => TestCombatants.Use(feat, AbilityTarget.Self, keepUp: true)).ToArray();

        private static void RequestImmediateRecast(Simulation simulation, Combatant combatant)
        {
            combatant.RecastReadyAt.Clear();
            combatant.NextActionAt = simulation.Now;
        }

        private static double RunAndTrackPeakTemporaryHp(Simulation simulation, Combatant target, double seconds)
        {
            var peak = 0.0;
            while (simulation.Now < seconds)
            {
                SimulationClock.RunUntil(simulation, simulation.Now + 0.1);
                peak = Math.Max(peak, target.TemporaryHpTotal);
            }

            return peak;
        }
    }
}
