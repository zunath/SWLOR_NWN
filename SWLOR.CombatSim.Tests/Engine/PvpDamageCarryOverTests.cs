using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Tests.Support;

namespace SWLOR.CombatSim.Tests.Engine
{
    /// <summary>
    /// Two players who build the PvP damage ramp on each other must not be able to carry it anywhere else.
    /// </summary>
    [TestFixture]
    public class PvpDamageCarryOverTests
    {
        private const int RampedSeconds = 300;
        private const double Hit = 100;

        private AbilityCatalog _catalog;
        private Simulation _simulation;
        private Combatant _rampedPlayer;
        private Combatant _sparringPartner;
        private Combatant _freshPlayer;
        private Combatant _npc;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [SetUp]
        public void SparForFiveMinutes()
        {
            _simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(
                    true,
                    new[] { TestCombatants.Dummy("Ramped player") },
                    new[] { TestCombatants.Dummy("Sparring partner"), TestCombatants.Dummy("Fresh player"), TestCombatants.Dummy("NPC", isNpc: true) }),
                RuleSets.Current(),
                _catalog);
            _rampedPlayer = _simulation.Get("Ramped player");
            _sparringPartner = _simulation.Get("Sparring partner");
            _freshPlayer = _simulation.Get("Fresh player");
            _npc = _simulation.Get("NPC");

            for (var second = 0; second <= RampedSeconds; second += 10)
            {
                SimulationClock.Set(_simulation, second);
                _simulation.ApplyDamage(_rampedPlayer, _sparringPartner, 1);
                _simulation.ApplyDamage(_sparringPartner, _rampedPlayer, 1);
            }
        }

        [Test]
        public void TheSparringPartnersTakeDoubleDamageFromEachOther()
        {
            DamageTaken(_rampedPlayer, _sparringPartner).Should().BeApproximately(Hit * 2, 1e-6);
        }

        [Test]
        public void TheRampedPlayerDealsNormalDamageToAnNpc()
        {
            DamageTaken(_rampedPlayer, _npc).Should().BeApproximately(Hit, 1e-6);
        }

        [Test]
        public void AnNpcDealsNormalDamageToTheRampedPlayer()
        {
            DamageTaken(_npc, _rampedPlayer).Should().BeApproximately(Hit, 1e-6);
        }

        [Test]
        public void TheRampedPlayerDealsNormalDamageToAPlayerWhoJustJoined()
        {
            DamageTaken(_rampedPlayer, _freshPlayer).Should().BeApproximately(Hit, 1e-6, "the ramp follows the time the player being hit has spent in PvP combat");
        }

        private double DamageTaken(Combatant attacker, Combatant target)
        {
            SimulationClock.Set(_simulation, RampedSeconds + 1);
            var before = target.Hp;
            _simulation.ApplyDamage(attacker, target, Hit);
            return before - target.Hp;
        }
    }
}
