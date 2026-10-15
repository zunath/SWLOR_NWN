using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Rules;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Rules
{
    [TestFixture]
    public class RuleSetCostTests
    {
        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [Test]
        public void Current_ChargesTheGameDataCost_BenevolenceIIIAtEightFp()
        {
            var benevolence3 = _catalog.Get(FeatType.Benevolence3);

            benevolence3.SkillRequirement.Should().Be(38);
            benevolence3.FP.Should().Be(8);
            RuleSets.Current().GetFpCost(benevolence3).Should().Be(8);
        }

        [Test]
        public void PreTuning_PutsBenevolenceIIIBackAtSevenFp()
        {
            RuleSets.PreTuning().GetFpCost(_catalog.Get(FeatType.Benevolence3)).Should().Be(7);
        }

        [Test]
        public void PreTuning_ExactlyUndoesTheRoundedTwentyPercentRise()
        {
            var preTuning = RuleSets.PreTuning();

            for (var cost = 1; cost <= 200; cost++)
            {
                var raised = preTuning.AdjustCost(cost, 40, 1.2f);
                preTuning.AdjustCost(raised, 40, preTuning.FpCostMultiplier).Should().Be(cost, $"{cost} FP was raised to {raised}");
            }
        }

        [Test]
        public void Current_LeavesStaminaCostsAlone_QuickDrawIVStaysAtTwelve()
        {
            var quickDraw4 = _catalog.Get(FeatType.QuickDraw4);

            quickDraw4.SkillRequirement.Should().Be(40);
            quickDraw4.STM.Should().Be(12);
            RuleSets.Current().GetStmCost(quickDraw4).Should().Be(12);
        }

        [Test]
        public void Current_LeavesMedKitIAtFourStaminaBecauseItHasNoSkillRequirement()
        {
            var medKit1 = _catalog.Get(FeatType.MedKit1);

            medKit1.SkillRequirement.Should().Be(0);
            medKit1.STM.Should().Be(4);
            RuleSets.Current().GetStmCost(medKit1).Should().Be(4);
        }

        [Test]
        public void Current_DoesNotChargeFpForAStaminaAbilityOrStaminaForAnFpAbility()
        {
            var current = RuleSets.Current();

            current.GetFpCost(_catalog.Get(FeatType.QuickDraw4)).Should().Be(0);
            current.GetStmCost(_catalog.Get(FeatType.Benevolence3)).Should().Be(0);
        }

        [TestCase(FeatType.Benevolence3)]
        [TestCase(FeatType.QuickDraw4)]
        [TestCase(FeatType.MedKit1)]
        [TestCase(FeatType.MedKit4)]
        [TestCase(FeatType.Resuscitation2)]
        public void Current_ChargesEveryCostAsTheGameDataHasIt(FeatType feat)
        {
            var ability = _catalog.Get(feat);
            var current = RuleSets.Current();

            current.GetFpCost(ability).Should().Be(ability.FP);
            current.GetStmCost(ability).Should().Be(ability.STM);
        }

        [TestCase(27, 10, 10)]
        [TestCase(28, 10, 12)]
        [TestCase(50, 10, 12)]
        [TestCase(0, 10, 10)]
        public void AdjustCost_AppliesTheMultiplierOnlyFromTheSkillThreshold(int skillRequirement, int baseCost, int expected)
        {
            RuleSets.Current().AdjustCost(baseCost, skillRequirement, 1.2f).Should().Be(expected);
        }

        [TestCase(7, 8)]
        [TestCase(12, 14)]
        [TestCase(5, 6)]
        [TestCase(2, 2)]
        [TestCase(3, 4)]
        public void AdjustCost_RoundsToTheNearestWholeNumber(int baseCost, int expected)
        {
            RuleSets.Current().AdjustCost(baseCost, 40, 1.2f).Should().Be(expected);
        }

        [Test]
        public void AdjustCost_NeverChargesForAFreeAbility()
        {
            RuleSets.Current().AdjustCost(0, 40, 1.2f).Should().Be(0);
        }
    }
}
