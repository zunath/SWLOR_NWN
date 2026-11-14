using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum.Item;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// The pure rules run with no container, as the offline tools run them, and read every stat type's attribute.
/// The table of equipment actions is empty until the module load handler fills it, as it always was.
/// </summary>
[NonParallelizable]
public class StatRulesTests
{
    [Test]
    public void TheMetadata_MatchesTheAttributeOfEveryStatType()
    {
        foreach (var stat in Enum.GetValues<StatType>())
        {
            var attribute = stat.GetAttribute<StatType, StatTypeAttribute>();

            StatRules.GetStatTypeCategory(stat).Should().Be(attribute.Category, stat.ToString());
            StatRules.GetStatTypeAggregation(stat).Should().Be(attribute.Aggregation, stat.ToString());
            StatRules.GetStatTypeDeflectionSource(stat).Should().Be(attribute.DeflectionSource, stat.ToString());
        }
    }

    [Test]
    public void AnUndefinedStatType_HasTheDefaultMetadata()
    {
        const StatType Undefined = (StatType)int.MaxValue;

        StatRules.GetStatTypeCategory(Undefined).Should().Be(StatTypeCategory.NonBeneficial);
        StatRules.GetStatTypeAggregation(Undefined).Should().Be(StatTypeAggregation.Additive);
        StatRules.GetStatTypeDeflectionSource(Undefined).Should().Be(DeflectionSource.None);
    }

    [TestCase(StatType.MeleeDeflectionStaminaRestore, StatType.MeleeDeflection)]
    [TestCase(StatType.ShieldDeflectionStaminaRestore, StatType.ShieldDeflection)]
    [TestCase(StatType.DeflectionFPRestore, StatType.RangedDeflection)]
    [TestCase(StatType.Attack, StatType.Invalid)]
    public void GetGrantedDeflectionStatType_FollowsTheSource(StatType source, StatType expected) =>
        StatRules.GetGrantedDeflectionStatType(source).Should().Be(expected);

    [TestCase(StatType.Attack, 3, 4, 7)]
    [TestCase(StatType.OpeningAutoAttackIdleSeconds, 3, 4, 4)]
    [TestCase(StatType.OpeningAutoAttackIdleSeconds, 4, 3, 4)]
    [TestCase(StatType.StatusAppliedRequiredCategory, 3, 4, 7)]
    [TestCase(StatType.StatusAppliedRequiredCategory, 3, 1, 3)]
    public void AggregateStatAdjustment_CombinesByTheStatsAggregation(StatType stat, int current, int adjustment, int expected) =>
        StatRules.AggregateStatAdjustment(stat, current, adjustment).Should().Be(expected);

    [TestCase(StatType.Attack, 0, false)]
    [TestCase(StatType.AttackPercentAdjustment, 1, true)]
    [TestCase(StatType.AttackPercentAdjustment, -1, false)]
    [TestCase(StatType.FPCostPercentAdjustment, -1, true)]
    [TestCase(StatType.FPCostPercentAdjustment, 1, false)]
    [TestCase(StatType.Invalid, 1, false)]
    public void IsBeneficialStatAdjustment_FollowsTheStatsCategory(StatType stat, int value, bool expected) =>
        StatRules.IsBeneficialStatAdjustment(stat, value).Should().Be(expected);

    [TestCase(10, 10, 5, 35)]
    [TestCase(3, 7, 2, 21)]
    public void CalculateDefense_TruncatesAfterMultiplyingTheSkill(int stat, int skill, int bonus, int expected) =>
        StatRules.CalculateDefense(stat, skill, bonus).Should().Be(expected);

    [Test]
    public void TheRatingFormulas_AreEightPlusTwiceTheLevelPlusStatAndBonus()
    {
        StatRules.GetAttack(1, 2, 3).Should().Be(15);
        StatRules.GetAccuracy(1, 2, 3).Should().Be(15);
        StatRules.GetEvasion(1, 2, 3).Should().Be(15);
    }

    [Test]
    public void LoadStatTypeMetadata_CanRunAgainWithNoEffect()
    {
        StatRules.LoadStatTypeMetadata();
        StatRules.LoadStatTypeMetadata();

        StatRules.GetStatTypeCategory(StatType.AttackPercentAdjustment).Should().Be(StatTypeCategory.BeneficialWhenPositive);
    }

    [Test]
    public void EquipmentStatTotals_CraftSkillsKeepTheirSubtypeOrder()
    {
        EquipmentStatTotals.CraftSkills.Should().Equal(
            SkillType.Smithery, SkillType.Engineering, SkillType.Fabrication, SkillType.Agriculture);
    }

    [Test]
    public void TheEquipmentActions_AreEmptyUntilTheModuleLoadHandlerRegistersThemInItsOrder()
    {
        using var container = new FacadeContainer();
        using var installed = container.Install();

        EquipmentStats.HandledPropertyTypes.Should().BeEmpty();
        Stat.TryGetEquipmentStatAction(ItemPropertyType.Attack, out _).Should().BeFalse();

        EquipmentStats.RegisterStatActions();

        EquipmentStats.HandledPropertyTypes.Should().Equal(
            ItemPropertyType.HPBonus, ItemPropertyType.FP, ItemPropertyType.FPRegen, ItemPropertyType.Stamina,
            ItemPropertyType.STMRegen, ItemPropertyType.CombatReadiness, ItemPropertyType.Attack, ItemPropertyType.Accuracy,
            ItemPropertyType.ForceAttack, ItemPropertyType.Defense, ItemPropertyType.Resistance, ItemPropertyType.Evasion,
            ItemPropertyType.Control, ItemPropertyType.Craftsmanship, ItemPropertyType.CPBonus, ItemPropertyType.Stealth,
            ItemPropertyType.Detection, ItemPropertyType.TrapBonus, ItemPropertyType.Disarm, ItemPropertyType.PoisonBonus,
            ItemPropertyType.Lockpicking);
        Stat.TryGetEquipmentStatAction(ItemPropertyType.Attack, out var action).Should().BeTrue();
        action.Should().NotBeNull();
        Stat.TryGetEquipmentStatAction(ItemPropertyType.Invalid, out _).Should().BeFalse();
    }

    [Test]
    public void RegisteringAnActionTwice_KeepsTheLastAndOneEntry()
    {
        using var container = new FacadeContainer();
        using var installed = container.Install();
        StatEquipmentAction first = (_, _, _, _) => { };
        StatEquipmentAction second = (_, _, _, _) => { };

        Stat.RegisterEquipmentStatAction(ItemPropertyType.Attack, first);
        Stat.RegisterEquipmentStatAction(ItemPropertyType.Attack, second);

        Stat.EquipmentStatActionTypes.Should().ContainSingle();
        Stat.TryGetEquipmentStatAction(ItemPropertyType.Attack, out var found).Should().BeTrue();
        found.Should().BeSameAs(second);
    }

    [Test]
    public void CacheData_TouchesTheMetadataAndLeavesNoState()
    {
        using var container = new FacadeContainer();
        using var installed = container.Install();

        Stat.CacheData();
        Stat.CacheData();

        Stat.GetStatTypeAggregation(StatType.Attack).Should().Be(StatTypeAggregation.Additive);
    }
}
