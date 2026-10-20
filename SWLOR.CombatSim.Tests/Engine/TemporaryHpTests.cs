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
        public void Current_CapsTotalTemporaryHpAtHalfOfMaxHp_EvenAfterSeveralDifferentShields()
        {
            var simulation = Spawn(RuleSets.Current(), new[] { TestCombatants.Medic("Medic", SelfKeepUp(DifferentShields)) });
            var medic = simulation.Get("Medic");

            var peak = RunAndTrackPeakTemporaryHp(simulation, medic, ShieldingWindowSeconds);

            peak.Should().Be(500.0, "the 50% cap of a 1,000 HP target is reached by the first few shields and never exceeded");
            medic.Stats.AbilitiesUsed.Should().BeGreaterThan(3, "the medic kept casting shields after the cap was full");
        }

        [Test]
        public void NaturalRegeneration_StartsAtAnyPointOfItsCycle()
        {
            // Heartbeats come every 6 seconds and HP returns every fifth; a fresh cycle would wait 24 seconds or more.
            var earlyRegeneration = Enumerable.Range(1, 20).Count(seed =>
            {
                var wounded = new CombatantSpec { Name = "Wounded", MaxHp = 1000, HpRegenPer30Seconds = 30f };
                var simulation = new Simulation(
                    TestScenarios.TwoTeams(false, new[] { wounded }, new[] { TestCombatants.Dummy("Enemy", true) }),
                    RuleSets.PreTuning(),
                    _catalog,
                    seed);
                simulation.Spawn();
                simulation.Get("Wounded").Hp = 500;
                SimulationClock.RunUntil(simulation, 18);
                return simulation.Get("Wounded").Stats.HealingReceived > 0;
            });

            earlyRegeneration.Should().BeGreaterThan(0, "a player joins partway through the game's regeneration cycle");
        }

        [TestCase(0, true)]
        [TestCase(200, false)]
        public void NaturalRegeneration_WaitsWhileTemporaryHpFillsTheHealthBar(int shield, bool expectHealing)
        {
            var wounded = new CombatantSpec { Name = "Wounded", MaxHp = 1000, HpRegenPer30Seconds = 30f };
            var simulation = Spawn(RuleSets.PreTuning(), new[] { wounded });
            var combatant = simulation.Get("Wounded");
            combatant.Hp = 900;
            if (shield > 0)
                combatant.TemporaryHp.Add(new TemporaryHpPool { Key = "Shield", Amount = shield, ExpiresAt = 600 });

            SimulationClock.RunUntil(simulation, 40);

            (combatant.Stats.HealingReceived > 0).Should().Be(expectHealing,
                "the game's tick needs current HP, which counts temporary HP, below max HP");
        }

        [Test]
        public void Current_CapUsesTheRecipientsMaxHp()
        {
            var medic = TestCombatants.Medic("Medic", DifferentShields
                .Select(feat => TestCombatants.Use(feat, AbilityTarget.NamedAlly, keepUp: true, targetName: "Tank"))
                .ToArray());
            var simulation = Spawn(RuleSets.Current(), new[] { medic, new CombatantSpec { Name = "Tank", MaxHp = 400 } });

            var peak = RunAndTrackPeakTemporaryHp(simulation, simulation.Get("Tank"), ShieldingWindowSeconds);

            peak.Should().Be(200.0);
        }

        [Test]
        public void PreTuning_StackShieldsFromDifferentKeysPastHalfOfMaxHp()
        {
            var simulation = Spawn(RuleSets.PreTuning(), new[] { TestCombatants.Medic("Medic", SelfKeepUp(DifferentShields)) });
            var medic = simulation.Get("Medic");

            var peak = RunAndTrackPeakTemporaryHp(simulation, medic, ShieldingWindowSeconds);

            peak.Should().BeGreaterThan(500.0);
            medic.TemporaryHp.Select(p => p.Key).Distinct().Should().HaveCount(DifferentShields.Length);
        }

        [TestCase("pre-tuning")]
        [TestCase("current")]
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
        public void Current_ReplacingAShieldIsNotShrunkByTheShieldItReplaces()
        {
            // On a 700 HP medic the cap is 350. Pain Suppressant (119) and Deflector Shield (175) fill all but 56 of it;
            // Guardian Ward (149 uncapped) takes that remainder. Recasting Guardian Ward must get the same 56 back, not 0
            // because its old pool still counted against the cap.
            var medicSpec = TestCombatants.Medic("Medic",
                TestCombatants.Use("PainSuppressant2", AbilityTarget.Self, keepUp: true),
                TestCombatants.Use("DeflectorShield3", AbilityTarget.Self, keepUp: true),
                TestCombatants.Use("GuardianWard4", AbilityTarget.Self));
            medicSpec.MaxHp = 700;
            var simulation = Spawn(RuleSets.Current(), new[] { medicSpec });
            var medic = simulation.Get("Medic");
            SimulationClock.RunUntil(simulation, () => medic.TemporaryHp.Count == 3, 15, "three shields applied");
            var ward = medic.TemporaryHp.Single(p => p.Key == "GuardianWard");
            ward.Amount.Should().Be(56.0);
            medic.TemporaryHpTotal.Should().Be(350.0);

            RequestImmediateRecast(simulation, medic);
            SimulationClock.RunUntil(simulation, () => medic.Stats.AbilitiesUsed == 4, 10, "fourth cast resolved");

            var replacement = medic.TemporaryHp.Single(p => p.Key == "GuardianWard");
            replacement.Should().NotBeSameAs(ward);
            replacement.Amount.Should().Be(56.0);
            medic.TemporaryHpTotal.Should().Be(350.0);
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
