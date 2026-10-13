using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Tests.Support;

namespace SWLOR.CombatSim.Tests.Engine
{
    [TestFixture]
    public class PvpHealingTests
    {
        private const int RawHeal = 1000;
        private const double Tolerance = 0.01;

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [TestCase(0, 700)]
        [TestCase(110, 700)]
        [TestCase(120, 630)]
        [TestCase(150, 490)]
        [TestCase(180, 350)]
        [TestCase(210, 210)]
        [TestCase(240, 70)]
        [TestCase(300, 70)]
        [TestCase(1200, 70)]
        public void Wave2_HealingReceivedInContinuousPvpCombat_IsSeventyPercentThenRampsDown(int secondsOfCombat, double expected)
        {
            var fight = new PvpFight(_catalog, RuleSets.Wave2());

            fight.FightUntil(secondsOfCombat);

            fight.HealAt(secondsOfCombat).Should().BeApproximately(expected, Tolerance);
        }

        [Test]
        public void Wave2_HealingReceivedPercentIsAppliedBeforeThePvpReduction()
        {
            var fight = new PvpFight(_catalog, RuleSets.Wave2()) { TargetHealingReceivedPercent = 10f };
            fight.FightUntil(0);

            fight.HealAt(0).Should().BeApproximately(RawHeal * 1.1 * 0.7, Tolerance);
        }

        [Test]
        public void Wave2_RegenerationAndOverTimeHealsAreReducedToo()
        {
            var fight = new PvpFight(_catalog, RuleSets.Wave2());
            fight.FightUntil(120);

            fight.HealAt(120, HealKind.OverTime).Should().BeApproximately(630, Tolerance);
            fight.HealAt(120, HealKind.NaturalRegeneration).Should().BeApproximately(630, Tolerance);
        }

        [Test]
        public void Wave2_NoReductionWhenTheScenarioIsNotPvp()
        {
            var fight = new PvpFight(_catalog, RuleSets.Wave2()) { IsPvp = false };
            fight.HealAt(0).Should().BeApproximately(RawHeal, Tolerance);

            fight.FightUntil(300);

            fight.HealAt(300).Should().BeApproximately(RawHeal, Tolerance);
        }

        [Test]
        public void Wave2_NoReductionWhenTheAttackerIsAnNpc()
        {
            var fight = new PvpFight(_catalog, RuleSets.Wave2()) { AttackerIsNpc = true };

            fight.FightUntil(300);

            fight.HealAt(300).Should().BeApproximately(RawHeal, Tolerance);
        }

        [Test]
        public void Wave2_NoReductionWhenTheHealedCombatantIsAnNpc()
        {
            var fight = new PvpFight(_catalog, RuleSets.Wave2()) { TargetIsNpc = true };

            fight.FightUntil(300);

            fight.HealAt(300).Should().BeApproximately(RawHeal, Tolerance);
        }

        [TestCase("current")]
        [TestCase("wave1")]
        public void RuleSetsBeforeWave2_DoNotReducePvpHealing(string ruleSetName)
        {
            var fight = new PvpFight(_catalog, RuleSets.Get(ruleSetName));

            fight.FightUntil(300);

            fight.HealAt(300).Should().BeApproximately(RawHeal, Tolerance);
        }

        [Test]
        public void Wave2_ThirtyOneQuietSecondsEndTheCombat_AndTheNextOneStartsOverAtSeventyPercent()
        {
            var fight = new PvpFight(_catalog, RuleSets.Wave2());
            fight.FightUntil(240);
            fight.HealAt(240).Should().BeApproximately(70, Tolerance);

            fight.HealAt(240 + 31).Should().BeApproximately(RawHeal, Tolerance, "31 quiet seconds ended the PvP combat");

            fight.Hit(240 + 40);
            fight.HealAt(240 + 40).Should().BeApproximately(700, Tolerance, "a new PvP combat starts at the first level");
        }

        [Test]
        public void Wave2_ActivityWithinThirtySeconds_KeepsTheSameCombatGoing()
        {
            var fight = new PvpFight(_catalog, RuleSets.Wave2());
            fight.FightUntil(240);

            fight.Hit(240 + 29);

            fight.HealAt(240 + 29).Should().BeApproximately(70, Tolerance);
        }
    }
}
