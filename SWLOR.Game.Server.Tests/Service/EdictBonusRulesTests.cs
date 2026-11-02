using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// What Edicts and City Affairs outcomes add to the calculations they reach (Player City Revamp, "Bonuses at a glance"): the math of
/// each hook point, from its numbers in the plan.
/// </summary>
public class EdictBonusRulesTests
{
    #region Festival Season

    [TestCase(false, false, 0)]
    [TestCase(true, false, 150)]
    [TestCase(false, true, 75)]
    [TestCase(true, true, 225)]
    public void Festival_AddsTenAndFivePercentOfTheBase_Together(bool edict, bool situation, int expected)
    {
        EdictBonusRules.GetFestivalXp(1500, edict, situation).Should().Be(expected);
    }

    [Test]
    public void Festival_IsAShareOfTheBase_NotOfTheCantinaOrSocialBonuses()
    {
        // The award is 1,500 base + RP points + Social + Cantina; the festival reads the base alone.
        EdictBonusRules.GetFestivalXp(1500, true, false).Should().Be(1500 * 10 / 100);
    }

    #endregion

    #region Medical Center

    [TestCase(0, false, 0f)]
    [TestCase(1, false, 0.05f)]
    [TestCase(5, false, 0.25f)]
    [TestCase(4, true, 0.10f)]
    [TestCase(5, true, 0.125f)]
    public void MedicalCenter_RemovesFivePercentPerLevel_AndHalvesWhileRationing(int level, bool halved, float expected)
    {
        EdictBonusRules.GetMedicalCenterReduction(level, halved).Should().BeApproximately(expected, 1e-6f);
    }

    [Test]
    public void MedicalCenter_NotRationing_KeepsTodaysValueExactly()
    {
        for (var level = 0; level <= 5; level++)
            EdictBonusRules.GetMedicalCenterReduction(level, false).Should().Be(level * 0.05f);
    }

    #endregion

    #region Guild Charter

    [Test]
    public void GuildCharter_AddsTenPercent_OnlyWhileHeld()
    {
        EdictBonusRules.GetGuildCharterBonus(true).Should().Be(0.10f);
        EdictBonusRules.GetGuildCharterBonus(false).Should().Be(0f);
    }

    [TestCase(1000, 100)]
    [TestCase(250, 25)]
    [TestCase(9, 0)]
    public void GuildCharter_AddsToTheBaseBeforeTheOtherBonusesStack(int baseAmount, int expectedExtra)
    {
        // Guild.CalculateGPReward and Quest.CalculateQuestGoldReward add the charter as one more share of the base, as Guild Relations
        // and Social do, so the shares add together and none scales another.
        ((int)(baseAmount * EdictBonusRules.GetGuildCharterBonus(true))).Should().Be(expectedExtra);
    }

    #endregion

    #region Prospectors' Charter and Home Ground

    [Test]
    public void Harvest_RollsEachChanceSeparately_InOrder()
    {
        var rolls = new Queue<int>(new[] { 10, 5 });

        EdictBonusRules.RollHarvestBonusItems(10, 5, rolls.Dequeue).Should().Be(2);
        rolls.Should().BeEmpty();
    }

    [TestCase(10, 5, 1)]
    [TestCase(11, 5, 0)]
    [TestCase(1, 5, 1)]
    [TestCase(100, 6, 0)]
    public void Harvest_ARollOfTheChanceOrLowerWins(int firstRoll, int secondRoll, int expectedFromFirst)
    {
        var first = EdictBonusRules.RollHarvestBonusItems(10, 0, () => firstRoll);
        var second = EdictBonusRules.RollHarvestBonusItems(0, 5, () => secondRoll);

        first.Should().Be(expectedFromFirst);
        second.Should().Be(secondRoll <= 5 ? 1 : 0);
    }

    [Test]
    public void Harvest_AChanceThatDoesNotApplyMakesNoRoll()
    {
        var rolls = 0;

        EdictBonusRules.RollHarvestBonusItems(0, 0, () => { rolls++; return 1; }).Should().Be(0);

        rolls.Should().Be(0);
    }

    [Test]
    public void Harvest_AddsAboutAFifteenthOfAnItemToEveryHarvest()
    {
        // The plan: on average the two rolls add 0.15 items to every harvest. Every d100 value once, for each roll.
        long total = 0;
        for (var first = 1; first <= 100; first++)
        {
            for (var second = 1; second <= 100; second++)
            {
                var rolls = new Queue<int>(new[] { first, second });
                total += EdictBonusRules.RollHarvestBonusItems(
                    EdictBonusRules.ProspectorsCharterChancePercent,
                    EdictBonusRules.HomeGroundChancePercent,
                    rolls.Dequeue);
            }
        }

        (total / 10000.0).Should().BeApproximately(0.15, 1e-9);
    }

    #endregion

    #region Hunting Grounds and Drydock Subsidy

    [Test]
    public void HuntingGrounds_AddsTenPoints()
    {
        EdictBonusRules.HuntingGroundsBonusPoints.Should().Be(10);
    }

    [Test]
    public void DrydockSubsidy_AddsFifteenPointsToTheRepairDiscount_StillCappedAtNinetyPercent()
    {
        ShipRepairBill.GetDiscount(3, 20, EdictBonusRules.DrydockSubsidyDiscount).Should().BeApproximately(0.50, 1e-9);
        ShipRepairBill.GetDiscount(5, 40, EdictBonusRules.DrydockSubsidyDiscount).Should().BeApproximately(0.60, 1e-9);
        ShipRepairBill.GetDiscount(20, 40, EdictBonusRules.DrydockSubsidyDiscount).Should().Be(0.90, "the 90% cap stays");
        ShipRepairBill.GetDiscount(13, 40, EdictBonusRules.DrydockSubsidyDiscount).Should().Be(0.90, "65% + 20% + 15% is over the cap");
        ShipRepairBill.GetDiscount(0, 10, EdictBonusRules.DrydockSubsidyDiscount).Should().BeApproximately(0.15, 1e-9);
    }

    [Test]
    public void DrydockSubsidy_WithoutIt_BillsExactlyAsBefore()
    {
        ShipRepairBill.GetDiscount(3, 20).Should().BeApproximately(0.35, 1e-9);
        ShipRepairBill.Calculate(10, 10, 1.0, 3, 20, 0).Should().Be(ShipRepairBill.Calculate(10, 10, 1.0, 3, 20));
    }

    [Test]
    public void DrydockSubsidy_LowersTheBillByFifteenPercentOfTheUndiscountedPrice()
    {
        // 100 shield (5,000) + 100 hull (10,000) = 15,000 before discounts; a level 3 Starport and Social 20 give 35%.
        ShipRepairBill.Calculate(100, 100, 1.0, 3, 20).Should().Be(9750);
        ShipRepairBill.Calculate(100, 100, 1.0, 3, 20, EdictBonusRules.DrydockSubsidyDiscount).Should().Be(7500);
    }

    #endregion

    #region Artisans' Charter

    [TestCase(1000, 10, 1100)]
    [TestCase(1050, 10, 1155)]
    [TestCase(15, 10, 17)]
    [TestCase(1000, 0, 1000)]
    [TestCase(0, 10, 0)]
    public void Artisans_AddsTenPointsToTheCraftXp_RoundedToTheNearestXp(int xp, int points, int expected)
    {
        EdictBonusRules.ScaleCraftXp(xp, points).Should().Be(expected);
    }

    #endregion

    #region Breeders' Charter

    [TestCase(73, 7)]
    [TestCase(15, 1)]
    [TestCase(8, 0)]
    [TestCase(0, 0)]
    [TestCase(100, 10)]
    [TestCase(120, 10)]
    [TestCase(-5, 0)]
    public void Breeders_AddsATenthOfTheStageGainAfterTheStageCap_RoundedDownToTenthsOfAPercent(int stageGain, int expected)
    {
        EdictBonusRules.GetBreedersCharterBonus(stageGain, 100).Should().Be(expected);
    }

    [Test]
    public void Breeders_PlansExampleStages()
    {
        // A stage whose enzymes add 7.3% Attack, 1.5% Accuracy and 0.8% Evasion gets 0.7%, 0.1% and nothing more.
        EdictBonusRules.GetBreedersCharterBonus(73, 100).Should().Be(7);
        EdictBonusRules.GetBreedersCharterBonus(15, 100).Should().Be(1);
        EdictBonusRules.GetBreedersCharterBonus(8, 100).Should().Be(0);

        // 6.0% becomes 6.6%: a stage gain of 60 adds 6.
        (60 + EdictBonusRules.GetBreedersCharterBonus(60, 100)).Should().Be(66);

        // A stage that would add 12.0% is capped at 10.0% and becomes 11.0%.
        (100 + EdictBonusRules.GetBreedersCharterBonus(120, 100)).Should().Be(110);
    }

    [Test]
    public void Breeders_TheBonusSurvivesTheResistanceCalculator()
    {
        var resistances = Resistance.GetAllResistanceTypes().ToDictionary(type => type, _ => 110);

        var purities = BeastResistanceCalculator.CreateResistancePurities(resistances);

        purities.Values.Should().OnlyContain(value => value == 110, "a resistance purity above the 10% stage cap is kept as it was saved");
    }

    #endregion
}
