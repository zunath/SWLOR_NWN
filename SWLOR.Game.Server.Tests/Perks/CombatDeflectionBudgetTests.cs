using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Perks;

public class CombatDeflectionBudgetTests
{
    [Test]
    public void PermanentWeaponAttackDeflectionSources_StayBelowDefaultCap()
    {
        const int UnbreakableWillMaximumAttackDeflection = 8;

        var permanentAttackDeflection =
            GetStaticStatBonus<StaffPerkDefinition>("StaffParry", PerkType.StaffParry, 4, StatType.AttackDeflection) +
            GetStaticStatBonus<LightsaberPerkDefinition>("DeflectionTraining", PerkType.DeflectionTraining, 3, StatType.AttackDeflection) +
            GetStaticStatBonus<SaberstaffPerkDefinition>("SpinningDeflection", PerkType.SpinningDeflection, 2, StatType.AttackDeflection) +
            GetStaticStatBonus<TwinBladePerkDefinition>("CenterlineGuard", PerkType.CenterlineGuard, 1, StatType.AttackDeflection) +
            UnbreakableWillMaximumAttackDeflection;

        permanentAttackDeflection.Should().Be(49);
        permanentAttackDeflection.Should().BeLessThan(50);
    }

    [Test]
    public void AlwaysOnWeaponCriticalRateSources_StayWellBelowCritCapBeforeStancesAndSupport()
    {
        var alwaysOnWeaponCriticalRate =
            GetStaticStatBonus<StaffPerkDefinition>("CrushingStyle", PerkType.CrushingStyle, 1, StatType.CriticalRatePercentAdjustment) +
            GetStaticStatBonus<StaffPerkDefinition>("CrushingMastery", PerkType.CrushingMastery, 3, StatType.CriticalRatePercentAdjustment) +
            GetStaticStatBonus<SpearPerkDefinition>("ForcePiercing", PerkType.ForcePiercing, 1, StatType.CriticalRatePercentAdjustment) +
            GetStaticStatBonus<SpearPerkDefinition>("RestorationStrike", PerkType.RestorationStrike, 1, StatType.CriticalRatePercentAdjustment);

        alwaysOnWeaponCriticalRate.Should().Be(25);
        alwaysOnWeaponCriticalRate.Should().BeLessThan(50);
    }

    [Test]
    public void ShieldDeflectionGuardAndAttackDeflection_BudgetsRemainMechanicallySeparate()
    {
        var bulwarkStats = GetStaticStatTypes<VibrobladePerkDefinition>("Bulwark", PerkType.Bulwark, 3);
        bulwarkStats.Should().Contain(StatType.ShieldDeflection);
        bulwarkStats.Should().NotContain(StatType.AttackDeflection);
        bulwarkStats.Should().NotContain(StatType.Guard);

        var guardTrainingStats = GetStaticStatTypes<KatarPerkDefinition>("GuardTraining", PerkType.GuardTraining, 3);
        guardTrainingStats.Should().Contain(StatType.Guard);
        guardTrainingStats.Should().Contain(StatType.GuardDamageReductionPercentAdjustment);
        guardTrainingStats.Should().NotContain(StatType.AttackDeflection);
        guardTrainingStats.Should().NotContain(StatType.ShieldDeflection);
    }

    private static int GetStaticStatBonus<TDefinition>(
        string methodName,
        PerkType perkType,
        int perkLevel,
        StatType statType)
        where TDefinition : new()
    {
        var definition = new TDefinition();
        typeof(TDefinition)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(definition, null);

        var builder = typeof(TDefinition)
            .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition);

        var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(builder)!;

        return perks[perkType]
            .PerkLevels[perkLevel]
            .StatBonuses
            .Should()
            .ContainSingle(x => x.Stat == statType)
            .Which
            .Calculate(0);
    }

    private static IReadOnlyCollection<StatType> GetStaticStatTypes<TDefinition>(
        string methodName,
        PerkType perkType,
        int perkLevel)
        where TDefinition : new()
    {
        var definition = new TDefinition();
        typeof(TDefinition)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(definition, null);

        var builder = typeof(TDefinition)
            .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition);

        var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(builder)!;

        return perks[perkType]
            .PerkLevels[perkLevel]
            .StatBonuses
            .Select(x => x.Stat)
            .ToArray();
    }
}
