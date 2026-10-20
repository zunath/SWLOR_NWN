using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Tests.Support;

namespace SWLOR.CombatSim.Tests.Engine
{
    [TestFixture]
    public class PvpDamageBackstopTests
    {
        private const double Tolerance = 0.01;

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [TestCase(0, 100)]
        [TestCase(170, 100)]
        [TestCase(180, 120)]
        [TestCase(210, 140)]
        [TestCase(240, 160)]
        [TestCase(270, 180)]
        [TestCase(300, 200)]
        [TestCase(900, 200)]
        public void Current_DamageGainsTwentyPercentEveryThirtySecondsFromThreeMinutes_UpToDouble(int secondsOfCombat, double expected)
        {
            var fight = new PvpFight(_catalog, RuleSets.Current());
            fight.FightUntil(secondsOfCombat);

            fight.DamageAt(secondsOfCombat).Should().BeApproximately(expected, Tolerance);
        }

        [Test]
        public void Current_TheBackstopIsAppliedBeforeTheTargetsDamageReduction()
        {
            var fight = new PvpFight(_catalog, RuleSets.Current()) { TargetDamageReductionPercent = 10f };
            fight.FightUntil(360);

            fight.DamageAt(360).Should().BeApproximately(100 * 2.0 * 0.9, Tolerance);
        }

        [Test]
        public void Current_NoBackstopWhenTheScenarioIsNotPvp()
        {
            var fight = new PvpFight(_catalog, RuleSets.Current()) { IsPvp = false };

            fight.FightUntil(420);

            fight.DamageAt(420).Should().BeApproximately(100, Tolerance);
        }

        [Test]
        public void Current_NoBackstopWhenTheAttackerIsAnNpc()
        {
            var fight = new PvpFight(_catalog, RuleSets.Current()) { AttackerIsNpc = true };

            fight.FightUntil(420);

            fight.DamageAt(420).Should().BeApproximately(100, Tolerance);
        }

        [Test]
        public void Current_NoBackstopWhenTheTargetIsAnNpc()
        {
            var fight = new PvpFight(_catalog, RuleSets.Current()) { TargetIsNpc = true };

            fight.FightUntil(420);

            fight.DamageAt(420).Should().BeApproximately(100, Tolerance);
        }

        [TestCase("pre-tuning")]
        public void RuleSetsWithoutPvpRules_HaveNoBackstop(string ruleSetName)
        {
            var fight = new PvpFight(_catalog, RuleSets.Get(ruleSetName));

            fight.FightUntil(420);

            fight.DamageAt(420).Should().BeApproximately(100, Tolerance);
        }

        [Test]
        public void Current_TheBackstopClockRestartsAfterThePvpCombatLapses()
        {
            var fight = new PvpFight(_catalog, RuleSets.Current());
            fight.FightUntil(360);
            fight.DamageAt(360).Should().BeApproximately(200, Tolerance);

            fight.Hit(360 + 40);

            fight.DamageAt(360 + 40).Should().BeApproximately(100, Tolerance);
        }

        [TestCase(1, 180, 20)] [TestCase(7, 210, 40)] [TestCase(29, 240, 60)] [TestCase(33, 270, 80)] [TestCase(17, 300, 100)]
        public void Current_SmallHitsUseProductionRoundedUpIncrease(int damage, int seconds, int percent)
        {
            var simulation = TestScenarios.Spawn(TestScenarios.TwoTeams(true,
                new[] { TestCombatants.Dummy("Attacker") }, new[] { TestCombatants.Dummy("Target") }), RuleSets.Current(), _catalog);
            var attacker = simulation.Get("Attacker");
            var target = simulation.Get("Target");
            target.PvpCombatStartedAt = -seconds;
            target.LastPvpActivityAt = 0;
            simulation.ApplyDamage(attacker, target, damage).Should().Be(
                SWLOR.Game.Server.Service.PvpCombatService.PvpCombatRules.IncreaseDamage(damage, percent));
        }
    }
}
