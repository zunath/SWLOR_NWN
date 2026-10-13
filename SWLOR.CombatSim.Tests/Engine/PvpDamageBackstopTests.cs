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
        [TestCase(230, 100)]
        [TestCase(240, 110)]
        [TestCase(270, 120)]
        [TestCase(300, 130)]
        [TestCase(330, 140)]
        [TestCase(360, 150)]
        [TestCase(900, 150)]
        public void Wave2_DamageGainsTenPercentEveryThirtySecondsFromFourMinutes_CappedAtFiftyPercent(int secondsOfCombat, double expected)
        {
            var fight = new PvpFight(_catalog, RuleSets.Wave2());
            fight.FightUntil(secondsOfCombat);

            fight.DamageAt(secondsOfCombat).Should().BeApproximately(expected, Tolerance);
        }

        [Test]
        public void Wave2_TheBackstopIsAppliedBeforeTheTargetsDamageReduction()
        {
            var fight = new PvpFight(_catalog, RuleSets.Wave2()) { TargetDamageReductionPercent = 10f };
            fight.FightUntil(360);

            fight.DamageAt(360).Should().BeApproximately(100 * 1.5 * 0.9, Tolerance);
        }

        [Test]
        public void Wave2_NoBackstopWhenTheScenarioIsNotPvp()
        {
            var fight = new PvpFight(_catalog, RuleSets.Wave2()) { IsPvp = false };

            fight.FightUntil(420);

            fight.DamageAt(420).Should().BeApproximately(100, Tolerance);
        }

        [Test]
        public void Wave2_NoBackstopWhenTheAttackerIsAnNpc()
        {
            var fight = new PvpFight(_catalog, RuleSets.Wave2()) { AttackerIsNpc = true };

            fight.FightUntil(420);

            fight.DamageAt(420).Should().BeApproximately(100, Tolerance);
        }

        [Test]
        public void Wave2_NoBackstopWhenTheTargetIsAnNpc()
        {
            var fight = new PvpFight(_catalog, RuleSets.Wave2()) { TargetIsNpc = true };

            fight.FightUntil(420);

            fight.DamageAt(420).Should().BeApproximately(100, Tolerance);
        }

        [TestCase("current")]
        [TestCase("wave1")]
        public void RuleSetsBeforeWave2_HaveNoBackstop(string ruleSetName)
        {
            var fight = new PvpFight(_catalog, RuleSets.Get(ruleSetName));

            fight.FightUntil(420);

            fight.DamageAt(420).Should().BeApproximately(100, Tolerance);
        }

        [Test]
        public void Wave2_TheBackstopClockRestartsAfterThePvpCombatLapses()
        {
            var fight = new PvpFight(_catalog, RuleSets.Wave2());
            fight.FightUntil(360);
            fight.DamageAt(360).Should().BeApproximately(150, Tolerance);

            fight.Hit(360 + 40);

            fight.DamageAt(360 + 40).Should().BeApproximately(100, Tolerance);
        }
    }
}
