using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Effects;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;

namespace SWLOR.CombatSim.Tests.Engine
{
    [TestFixture]
    public class DamageReductionTests
    {
        private const double Hit = 100;

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [Test]
        public void PainSuppressant_CutsAllDamageTakenUntilItEnds()
        {
            var simulation = Spawn(TestCombatants.Use("PainSuppressant2", AbilityTarget.NamedAlly, targetName: "Ally", minIntervalSeconds: 600));
            var ally = simulation.Get("Ally");
            CastOnce(simulation);

            DamageTaken(simulation, ally, DamageType.Physical).Should().BeApproximately(85, 1e-6);
            DamageTaken(simulation, ally, DamageType.Other).Should().BeApproximately(85, 1e-6);

            SimulationClock.RunUntil(simulation, simulation.Now + 31);
            DamageTaken(simulation, ally, DamageType.Physical).Should().BeApproximately(Hit, 1e-6, "the reduction ends with the effect");
        }

        [Test]
        public void PainSuppressantRanks_StackLikeTheGamesSummedDamageTakenStat()
        {
            var ally = new CombatantSpec { Name = "Ally", MaxHp = TestCombatants.EffectivelyUnlimited };
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(
                    false,
                    new[]
                    {
                        TestCombatants.Medic("Medic", TestCombatants.Use("PainSuppressant1", AbilityTarget.NamedAlly, targetName: "Ally", minIntervalSeconds: 600)),
                        TestCombatants.Medic("Second Medic", TestCombatants.Use("PainSuppressant2", AbilityTarget.NamedAlly, targetName: "Ally", minIntervalSeconds: 600)),
                        ally
                    },
                    new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.Current(),
                _catalog);
            var target = simulation.Get("Ally");
            SimulationClock.RunUntil(
                simulation,
                () => simulation.Get("Medic").Stats.AbilitiesUsed == 1 && simulation.Get("Second Medic").Stats.AbilitiesUsed == 1,
                10,
                "both ranks cast");

            DamageTaken(simulation, target, DamageType.Physical).Should().BeApproximately(75, 1e-6, "10% and 15% add up to 25%");
        }

        [Test]
        public void RousingShoutRanks_KeepTheStrongestLeadershipReduction()
        {
            var late = TestCombatants.Medic("Second Medic", TestCombatants.Use("RousingShout1", AbilityTarget.NamedAlly, targetName: "Ally", minIntervalSeconds: 600));
            late.JoinAtSeconds = 3f;
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(
                    false,
                    new[]
                    {
                        TestCombatants.Medic("Medic", TestCombatants.Use("RousingShout3", AbilityTarget.NamedAlly, targetName: "Ally", minIntervalSeconds: 600)),
                        late,
                        new CombatantSpec { Name = "Ally", MaxHp = TestCombatants.EffectivelyUnlimited }
                    },
                    new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.Current(),
                _catalog);
            var ally = simulation.Get("Ally");
            ally.Hp = ally.Spec.MaxHp * 0.30;
            SimulationClock.RunUntil(simulation, () => simulation.Get("Medic").Stats.AbilitiesUsed == 1, 10, "rank III cast");
            // Without its shield the ally is still in danger when rank I lands, so rank I's reduction applies too.
            ally.TemporaryHp.Clear();
            SimulationClock.RunUntil(simulation, () => simulation.Get("Second Medic").Stats.AbilitiesUsed == 1, 10, "rank I cast");
            ally.TemporaryHp.Clear();

            DamageTaken(simulation, ally, DamageType.Physical).Should().BeApproximately(75, 1e-6,
                "rank III's 25% stays while it outlasts rank I's 12%, as the game keeps the strongest Leadership cut");
        }

        [Test]
        public void AFullReduction_StillLetsOnePointOfALandedHitThrough()
        {
            var simulation = Spawn(TestCombatants.Use("PainSuppressant2", AbilityTarget.NamedAlly, targetName: "Ally", minIntervalSeconds: 600));
            var ally = simulation.Get("Ally");
            ally.DamageReductions["All"] = new DamageReductionInstance { Percent = 100, Scope = DamageReductionScope.All, ExpiresAt = 600 };

            DamageTaken(simulation, ally, DamageType.Physical).Should().Be(1, "the game keeps at least 1 damage after its percentage modifiers");
        }

        [Test]
        public void TypedReductions_MultiplyWithTheSummedAllDamageCut()
        {
            var simulation = Spawn(TestCombatants.Use("PainSuppressant2", AbilityTarget.NamedAlly, targetName: "Ally", minIntervalSeconds: 600));
            var ally = simulation.Get("Ally");
            ally.DamageReductions["All"] = new DamageReductionInstance { Percent = 20, Scope = DamageReductionScope.All, ExpiresAt = 600 };
            ally.DamageReductions["Force"] = new DamageReductionInstance { Percent = 50, Scope = DamageReductionScope.ForceOnly, ExpiresAt = 600 };

            DamageTaken(simulation, ally, DamageType.Force).Should().BeApproximately(40, 1e-6);
            DamageTaken(simulation, ally, DamageType.Physical).Should().BeApproximately(80, 1e-6);
        }

        [TestCase(30, 0, 75)]
        [TestCase(50, 0, Hit)]
        [TestCase(30, 10, Hit)]
        public void RousingShout_CutsPhysicalAndForceDamageOnlyForATargetInDanger(int hpPercent, int shieldPercent, double expected)
        {
            var simulation = Spawn(TestCombatants.Use("RousingShout3", AbilityTarget.NamedAlly, targetName: "Ally", minIntervalSeconds: 600));
            var ally = simulation.Get("Ally");
            ally.Hp = ally.Spec.MaxHp * hpPercent / 100.0;
            if (shieldPercent > 0)
            {
                // The game's danger check reads current HP, which counts an existing shield.
                ally.TemporaryHp.Add(new TemporaryHpPool { Key = "Existing", Amount = ally.Spec.MaxHp * shieldPercent / 100.0, ExpiresAt = 600 });
            }
            CastOnce(simulation);

            DamageTaken(simulation, ally, DamageType.Physical).Should().BeApproximately(expected, 1e-6);
            DamageTaken(simulation, ally, DamageType.Force).Should().BeApproximately(expected, 1e-6);
            DamageTaken(simulation, ally, DamageType.Other).Should().BeApproximately(Hit, 1e-6, "other damage types are not reduced");
        }

        [Test]
        public void ForceSanctuary_CutsOnlyForceDamage_FromTheMomentItLandsUntilItsZoneEnds()
        {
            var simulation = Spawn(TestCombatants.Use("ForceSanctuary1", AbilityTarget.Self, minIntervalSeconds: 600));
            var ally = simulation.Get("Ally");
            CastOnce(simulation);
            var landedAt = simulation.Now;

            DamageTaken(simulation, ally, DamageType.Force).Should().BeApproximately(95, 1e-6, "the zone's status applies as it lands, before the first heal");
            DamageTaken(simulation, ally, DamageType.Physical).Should().BeApproximately(Hit, 1e-6);

            SimulationClock.RunUntil(simulation, landedAt + 29.5);
            DamageTaken(simulation, ally, DamageType.Force).Should().BeApproximately(95, 1e-6, "the last refresh lasts to the end of the zone");
            SimulationClock.RunUntil(simulation, landedAt + 31);
            DamageTaken(simulation, ally, DamageType.Force).Should().BeApproximately(Hit, 1e-6, "refreshes stop with the zone at 30 seconds");
        }

        private Simulation Spawn(AbilityUseSpec use)
        {
            var ally = new CombatantSpec { Name = "Ally", MaxHp = TestCombatants.EffectivelyUnlimited };
            return TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { TestCombatants.Medic("Medic", use), ally }, new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.Current(),
                _catalog);
        }

        private static void CastOnce(Simulation simulation)
        {
            var medic = simulation.Get("Medic");
            SimulationClock.RunUntil(simulation, () => medic.Stats.AbilitiesUsed == 1, 10, "the ability resolved");
        }

        /// <summary>Damage one hit does after reductions, counting what temporary HP absorbed.</summary>
        private static double DamageTaken(Simulation simulation, Combatant target, DamageType type)
        {
            var before = target.Stats.DamageTakenToHp + target.Stats.DamageAbsorbedByTemporaryHp;
            simulation.ApplyDamage(simulation.Get("Enemy"), target, Hit, type);
            return target.Stats.DamageTakenToHp + target.Stats.DamageAbsorbedByTemporaryHp - before;
        }
    }
}
