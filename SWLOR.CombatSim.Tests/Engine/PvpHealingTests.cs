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

        // The tuned-pvp preset has no flat cut; these tests exercise the multiplier itself.
        private static RuleSet TunedPvpWithFlatCut()
        {
            var rules = RuleSets.TunedPvp();
            rules.PvpHealingMultiplier = 0.7f;
            rules.PvpTemporaryHpMultiplier = 0.7f;
            return rules;
        }

        [TestCase(0, 1000)]
        [TestCase(119, 1000)]
        [TestCase(120, 900)]
        [TestCase(240, 100)]
        public void TunedPvpPreset_HasNoFlatCut_OnlyTheRamp(int secondsOfCombat, double expected)
        {
            var fight = new PvpFight(_catalog, RuleSets.TunedPvp());

            fight.FightUntil(secondsOfCombat);

            fight.HealAt(secondsOfCombat).Should().BeApproximately(expected, Tolerance);
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
        public void TunedPvp_HealingReceivedInContinuousPvpCombat_IsSeventyPercentThenRampsDown(int secondsOfCombat, double expected)
        {
            var fight = new PvpFight(_catalog, TunedPvpWithFlatCut());

            fight.FightUntil(secondsOfCombat);

            fight.HealAt(secondsOfCombat).Should().BeApproximately(expected, Tolerance);
        }

        [Test]
        public void TunedPvp_HealingReceivedPercentIsAppliedBeforeThePvpReduction()
        {
            var fight = new PvpFight(_catalog, TunedPvpWithFlatCut()) { TargetHealingReceivedPercent = 10f };
            fight.FightUntil(0);

            fight.HealAt(0).Should().BeApproximately(RawHeal * 1.1 * 0.7, Tolerance);
        }

        [Test]
        public void TunedPvp_RegenerationAndOverTimeHealsAreReducedToo()
        {
            var fight = new PvpFight(_catalog, TunedPvpWithFlatCut());
            fight.FightUntil(120);

            fight.HealAt(120, HealKind.OverTime).Should().BeApproximately(630, Tolerance);
            fight.HealAt(120, HealKind.NaturalRegeneration).Should().BeApproximately(630, Tolerance);
        }

        [Test]
        public void TunedPvp_NoReductionWhenTheScenarioIsNotPvp()
        {
            var fight = new PvpFight(_catalog, TunedPvpWithFlatCut()) { IsPvp = false };
            fight.HealAt(0).Should().BeApproximately(RawHeal, Tolerance);

            fight.FightUntil(300);

            fight.HealAt(300).Should().BeApproximately(RawHeal, Tolerance);
        }

        [Test]
        public void TunedPvp_NoReductionWhenTheAttackerIsAnNpc()
        {
            var fight = new PvpFight(_catalog, TunedPvpWithFlatCut()) { AttackerIsNpc = true };

            fight.FightUntil(300);

            fight.HealAt(300).Should().BeApproximately(RawHeal, Tolerance);
        }

        [Test]
        public void TunedPvp_NoReductionWhenTheHealedCombatantIsAnNpc()
        {
            var fight = new PvpFight(_catalog, TunedPvpWithFlatCut()) { TargetIsNpc = true };

            fight.FightUntil(300);

            fight.HealAt(300).Should().BeApproximately(RawHeal, Tolerance);
        }

        [TestCase("current")]
        [TestCase("tuned")]
        public void RuleSetsWithoutPvpRules_DoNotReducePvpHealing(string ruleSetName)
        {
            var fight = new PvpFight(_catalog, RuleSets.Get(ruleSetName));

            fight.FightUntil(300);

            fight.HealAt(300).Should().BeApproximately(RawHeal, Tolerance);
        }

        [Test]
        public void TunedPvp_ThirtyOneQuietSecondsEndTheCombat_AndTheNextOneStartsOverAtSeventyPercent()
        {
            var fight = new PvpFight(_catalog, TunedPvpWithFlatCut());
            fight.FightUntil(240);
            fight.HealAt(240).Should().BeApproximately(70, Tolerance);

            fight.HealAt(240 + 31).Should().BeApproximately(RawHeal, Tolerance, "31 quiet seconds ended the PvP combat");

            fight.Hit(240 + 40);
            fight.HealAt(240 + 40).Should().BeApproximately(700, Tolerance, "a new PvP combat starts at the first level");
        }

        [Test]
        public void TunedPvp_ActivityWithinThirtySeconds_KeepsTheSameCombatGoing()
        {
            var fight = new PvpFight(_catalog, TunedPvpWithFlatCut());
            fight.FightUntil(240);

            fight.Hit(240 + 29);

            fight.HealAt(240 + 29).Should().BeApproximately(70, Tolerance);
        }
    }
}
