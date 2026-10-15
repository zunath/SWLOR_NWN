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
        public void TunedPvp_DamageGainsTwentyPercentEveryThirtySecondsFromThreeMinutes_UpToDouble(int secondsOfCombat, double expected)
        {
            var fight = new PvpFight(_catalog, RuleSets.TunedPvp());
            fight.FightUntil(secondsOfCombat);

            fight.DamageAt(secondsOfCombat).Should().BeApproximately(expected, Tolerance);
        }

        [Test]
        public void TunedPvp_TheBackstopIsAppliedBeforeTheTargetsDamageReduction()
        {
            var fight = new PvpFight(_catalog, RuleSets.TunedPvp()) { TargetDamageReductionPercent = 10f };
            fight.FightUntil(360);

            fight.DamageAt(360).Should().BeApproximately(100 * 2.0 * 0.9, Tolerance);
        }

        [Test]
        public void TunedPvp_NoBackstopWhenTheScenarioIsNotPvp()
        {
            var fight = new PvpFight(_catalog, RuleSets.TunedPvp()) { IsPvp = false };

            fight.FightUntil(420);

            fight.DamageAt(420).Should().BeApproximately(100, Tolerance);
        }

        [Test]
        public void TunedPvp_NoBackstopWhenTheAttackerIsAnNpc()
        {
            var fight = new PvpFight(_catalog, RuleSets.TunedPvp()) { AttackerIsNpc = true };

            fight.FightUntil(420);

            fight.DamageAt(420).Should().BeApproximately(100, Tolerance);
        }

        [Test]
        public void TunedPvp_NoBackstopWhenTheTargetIsAnNpc()
        {
            var fight = new PvpFight(_catalog, RuleSets.TunedPvp()) { TargetIsNpc = true };

            fight.FightUntil(420);

            fight.DamageAt(420).Should().BeApproximately(100, Tolerance);
        }

        [TestCase("pre-tuning")]
        [TestCase("current")]
        public void RuleSetsWithoutPvpRules_HaveNoBackstop(string ruleSetName)
        {
            var fight = new PvpFight(_catalog, RuleSets.Get(ruleSetName));

            fight.FightUntil(420);

            fight.DamageAt(420).Should().BeApproximately(100, Tolerance);
        }

        [Test]
        public void TunedPvp_TheBackstopClockRestartsAfterThePvpCombatLapses()
        {
            var fight = new PvpFight(_catalog, RuleSets.TunedPvp());
            fight.FightUntil(360);
            fight.DamageAt(360).Should().BeApproximately(200, Tolerance);

            fight.Hit(360 + 40);

            fight.DamageAt(360 + 40).Should().BeApproximately(100, Tolerance);
        }
    }
}
