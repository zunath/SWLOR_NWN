using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Perks;

public class LightsaberPerkBehaviorTests
{
    [Test]
    public void LightsaberTraitPerks_WireRedesignedStatBonuses()
    {
        // Mental Fortress
        AssertPerkStat(StatType.ForceDefensePercentAdjustment, "10");
        AssertPerkStat(StatType.ForceDefensePercentAdjustment, "12");

        // Weak Points
        AssertPerkStat(StatType.CriticalRateAgainstSunderedTargetPercentAdjustment, "10");

        // Overpower (FP-spend -> Force Attack)
        AssertPerkStat(StatType.HostileAbilityFPSpendForceAttackPercent, "3");
        AssertPerkStat(StatType.HostileAbilityFPSpendForceAttackPercent, "10");
        AssertPerkStat(StatType.HostileAbilityFPSpendForceAttackPercent, "15");
        AssertPerkStat(StatType.HostileAbilityFPSpendForceAttackMaxPercent, "15");
        AssertPerkStat(StatType.HostileAbilityFPSpendForceAttackMinFPCost, "5");
        AssertPerkStat(StatType.HostileAbilityFPSpendForceAttackDurationSeconds, "30");

        // Deflecting Return (bounded reflection)
        AssertPerkStat(StatType.RangedDeflectionReflectionPercent, "8");
        AssertPerkStat(StatType.RangedDeflectionReflectionPercent, "12");
        AssertPerkStat(StatType.RangedDeflectionReflectionPercent, "16");
        AssertPerkStat(StatType.RangedDeflectionReflectionCapPercent, "25");
        AssertPerkStat(StatType.RangedDeflectionReflectionCapPercent, "40");
        AssertPerkStat(StatType.RangedDeflectionReflectionCapPercent, "50");

        // Surrounded, Not Outmatched
        AssertPerkStat(StatType.SoresuPressureStackDefensePercent, "2");
        AssertPerkStat(StatType.SoresuPressureStackForceDefensePercent, "2");
        AssertPerkStat(StatType.SoresuPressureMaxStacks, "5");

        // Center of the Storm
        AssertPerkStat(StatType.SoresuPressureHighStackThreshold, "3");
        AssertPerkStat(StatType.SoresuPressureHighStackMobilityResistance, "10");
        AssertPerkStat(StatType.SoresuPressureHighStackDeflectionReflectionBonusPercent, "4");

        // High Ground
        AssertPerkStat(StatType.AutoAttackSunderedTargetFPRestore, "2");

        // Focus Shift
        AssertPerkStat(StatType.LowFPAttackPercentAdjustment, "15");
        AssertPerkStat(StatType.LowFPAttackThresholdPercent, "30");
    }

    [Test]
    public void RedesignedLightsaberStatTypes_DeclarePolarity()
    {
        Stat.GetStatTypeCategory(StatType.IncomingPhysicalToForceConversionPercent)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.RangedDeflectionReflectionPercent)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.RangedDeflectionReflectionCapPercent)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.SoresuPressureStackDefensePercent)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.SoresuPressureMaxStacks)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.HostileAbilityFPSpendForceAttackPercent)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.AutoAttackSunderedTargetFPRestore)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.LowFPAttackPercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.StanceHostileAutoAttackForceDamage)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
    }

    [Test]
    public void SaberWard_SelfAppliesPerRankConversionAndReplacesPerfectSoresu()
    {
        var source = AbilitySource("SaberWardAbilityDefinition.cs");
        source.Should().Contain("new SaberWardStatusEffect(conversionPercent, defensePercent, forceDefensePercent)");
        source.Should().Contain("SelfStatusEffectsToReplace = new[] { typeof(PerfectSoresuStatusEffect) }");
        source.Should().Contain("Build(builder, FeatType.SaberWard1, \"Saber Ward I\", 1, 8, 3, 2, 15, 3, 4)");
        source.Should().Contain("Build(builder, FeatType.SaberWard4, \"Saber Ward IV\", 4, 38, 12, 5, 30, 6, 9)");
    }

    [Test]
    public void MasterOfSoresu_SelfAppliesPerfectSoresuReplacingSaberWard()
    {
        var source = AbilitySource("MasterOfSoresuAbilityDefinition.cs");
        source.Should().Contain("SelfStatusEffectFactory = () => new PerfectSoresuStatusEffect()");
        source.Should().Contain("SelfStatusEffectsToReplace = new[] { typeof(SaberWardStatusEffect) }");
    }

    [Test]
    public void ForceLink_ReusesWardBondRedirectAtLinkRangeAndShare()
    {
        var source = AbilitySource("ForceLinkAbilityDefinition.cs");
        source.Should().Contain("FriendlyTargetStatusEffectFactory = () => new WardBondStatusEffect(45, 0, 0, 0, 20.0f)");
    }

    [Test]
    public void GuardiansChallenge_UsesTwoRanksAndDamagedYouEnmity()
    {
        var source = AbilitySource("GuardiansChallengeAbilityDefinition.cs");
        source.Should().Contain("SelfEnmityPercentIfTargetRecentlyDamagedActivator = enmityPercent");
        source.Should().Contain("ProtectedTargetHitWindowSeconds = 30");
        source.Should().Contain("Build(builder, FeatType.GuardiansChallenge1, \"Guardian's Challenge I\", 1, 12, 4, 1, 20)");
        source.Should().Contain("Build(builder, FeatType.GuardiansChallenge2, \"Guardian's Challenge II\", 2, 24, 8, 2, 30)");
        source.Should().NotContain("RequiresRecentWardHitTarget");
        source.Should().NotContain("GuardiansChallenge3");
    }

    [Test]
    public void Reprisal_DazesOnlyWhenTargetRecentlyDamagedYou()
    {
        var source = AbilitySource("ReprisalAbilityDefinition.cs");
        source.Should().Contain("ConditionalTargetStatusEffect = typeof(DazedStatusEffect)");
        source.Should().Contain("ConditionalTargetStatusDurationSeconds = 15");
        source.Should().Contain("RequireTargetRecentlyDamagedActivatorForConditionalStatus = true");
    }

    [Test]
    public void ShatteringStrike_InflictsScaledSunder()
    {
        var source = AbilitySource("ShatteringStrikeAbilityDefinition.cs");
        source.Should().Contain("StatusEffectFactory = () => new SunderStatusEffect(10)");
        source.Should().Contain("StatusEffectFactory = () => new SunderStatusEffect(12)");
    }

    [Test]
    public void SaberWardStatusEffect_ConvertsPhysicalAndGrantsDefenses()
    {
        var rankOne = new SaberWardStatusEffect(15, 3, 4);
        rankOne.StatGroup.Stats[StatType.IncomingPhysicalToForceConversionPercent].Should().Be(15);
        rankOne.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(3);
        rankOne.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(4);

        var rankFour = new SaberWardStatusEffect(30, 6, 9);
        rankFour.StatGroup.Stats[StatType.IncomingPhysicalToForceConversionPercent].Should().Be(30);
        rankFour.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(6);
        rankFour.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(9);
    }

    [Test]
    public void PerfectSoresuStatusEffect_AppliesCapstoneWardBundle()
    {
        var soresu = new PerfectSoresuStatusEffect();
        soresu.StatGroup.Stats[StatType.IncomingPhysicalToForceConversionPercent].Should().Be(40);
        soresu.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(18);
        soresu.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(22);
        soresu.StatGroup.Stats[StatType.EnmityPercentAdjustment].Should().Be(25);
        soresu.StatGroup.Stats[StatType.RangedDeflectionReflectionPercent].Should().Be(24);
        soresu.StatGroup.Stats[StatType.RangedDeflectionReflectionCapPercent].Should().Be(75);
    }

    [Test]
    public void SoresuPressureStatusEffect_ScalesDefensesAndMobilityByStacks()
    {
        var pressure = new SoresuPressureStatusEffect(3, 6, 6, 10);
        pressure.Stacks.Should().Be(3);
        pressure.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(6);
        pressure.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(6);
        pressure.StatGroup.Stats[StatType.MobilityResistance].Should().Be(10);
    }

    [Test]
    public void LightsaberStances_EmitRedesignedStatusStats()
    {
        var imbuement = new ImbuementStanceStatusEffect();
        imbuement.StatGroup.Stats[StatType.StanceHostileAutoAttackForceDamage].Should().Be(8);
        imbuement.StatGroup.Stats[StatType.StanceHostileAutoAttackFPCost].Should().Be(2);

        var immovable = new ImmovableStanceStatusEffect();
        immovable.StatGroup.Stats[StatType.EnmityPercentAdjustment].Should().Be(30);
        immovable.StatGroup.Stats[StatType.MobilityResistance].Should().Be(8);
        immovable.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-25);
        immovable.StatGroup.Stats[StatType.ForceAttackPercentAdjustment].Should().Be(-25);
    }

    private static void AssertPerkStat(StatType statType, string valueExpression)
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Feature",
            "PerkDefinition",
            "LightsaberPerkDefinition.cs"));

        source.Should().Contain($".IncreasesStat(StatType.{statType}, {valueExpression})");
    }

    private static string AbilitySource(string fileName)
    {
        return File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "Lightsaber",
            fileName));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("repository root should be discoverable from the test directory");
        return directory!;
    }
}
