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
        AssertPerkStat(StatType.HostileAbilityFPSpendForceAttackMaxPercent, "9");
        AssertPerkStat(StatType.HostileAbilityFPSpendForceAttackMaxPercent, "10");
        AssertPerkStat(StatType.HostileAbilityFPSpendForceAttackMaxPercent, "15");
        AssertPerkStat(StatType.HostileAbilityFPSpendForceAttackMinFPCost, "5");
        AssertPerkStat(StatType.HostileAbilityFPSpendForceAttackDurationSeconds, "30");

        // Deflecting Return (bounded reflection)
        AssertPerkStat(StatType.RangedDeflection, "5");
        AssertPerkStat(StatType.RangedDeflection, "8");
        AssertPerkStat(StatType.RangedDeflection, "12");
        AssertPerkStat(StatType.RangedDeflectionReflectionPercent, "20");
        AssertPerkStat(StatType.RangedDeflectionReflectionPercent, "30");
        AssertPerkStat(StatType.RangedDeflectionReflectionPercent, "40");
        AssertPerkStat(StatType.RangedDeflectionReflectionCapPercent, "50");
        AssertPerkStat(StatType.RangedDeflectionReflectionCapPercent, "75");
        AssertPerkStat(StatType.RangedDeflectionReflectionCapPercent, "100");

        // Surrounded, Not Outmatched
        AssertPerkStat(StatType.EmbattledStackDefensePercent, "2");
        AssertPerkStat(StatType.EmbattledStackForceDefensePercent, "2");
        AssertPerkStat(StatType.EmbattledMaxStacks, "5");

        // Center of the Storm
        AssertPerkStat(StatType.EmbattledHighStackThreshold, "3");
        AssertPerkStat(StatType.EmbattledHighStackMobilityResistance, "10");
        AssertPerkStat(StatType.EmbattledHighStackDeflectionReflectionBonusPercent, "4");

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
        Stat.GetStatTypeCategory(StatType.EmbattledStackDefensePercent)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.EmbattledMaxStacks)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.HostileAbilityFPSpendForceAttackPercent)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.AutoAttackSunderedTargetFPRestore)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.LowFPAttackPercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.StanceHostileAutoAttackForceConversion)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
    }

    [Test]
    public void SaberWard_SelfAppliesPerRankConversionAndReplacesPerfectAegis()
    {
        var source = AbilitySource("SaberWardAbilityDefinition.cs");
        source.Should().Contain("new SaberWardStatusEffect(conversionPercent, defensePercent, forceDefensePercent)");
        source.Should().Contain("SelfStatusEffectsToReplace = new[] { typeof(PerfectAegisStatusEffect) }");
        source.Should().Contain("Build(builder, FeatType.SaberWard1, \"Saber Ward I\", 1, 8, 3, 2, 15, 3, 4)");
        source.Should().Contain("Build(builder, FeatType.SaberWard4, \"Saber Ward IV\", 4, 38, 12, 5, 30, 6, 9)");
    }

    [Test]
    public void AegisEternal_SelfAppliesPerfectAegisReplacingSaberWard()
    {
        var source = AbilitySource("AegisEternalAbilityDefinition.cs");
        source.Should().Contain("SelfStatusEffectFactory = () => new PerfectAegisStatusEffect()");
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
        source.Should().Contain("Build(builder, FeatType.GuardiansChallenge1, \"Guardian's Challenge I\", 1, 12, 4, 1, 20, Spell.GuardiansChallenge1)");
        source.Should().Contain("Build(builder, FeatType.GuardiansChallenge2, \"Guardian's Challenge II\", 2, 24, 8, 2, 30, Spell.GuardiansChallenge2)");
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
    public void Epicenter_IsSelfCenteredForceAreaWithPreExistingSunderBonus()
    {
        var source = AbilitySource("EpicenterAbilityDefinition.cs");
        source.Should().Contain("Spell.Epicenter1");
        source.Should().Contain("AbilityTargetingShapeType.Sphere");
        source.Should().Contain("6.0f");
        source.Should().Contain("AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf");
        source.Should().Contain("DamageType = CombatDamageType.Force");
        source.Should().Contain("ExtraDamageTargetStatusEffect = typeof(SunderStatusEffect)");
        source.Should().Contain("ExtraDamageIfTargetStatusEffect = 15");
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
    public void PerfectAegisStatusEffect_AppliesCapstoneWardBundle()
    {
        var aegis = new PerfectAegisStatusEffect();
        aegis.StatGroup.Stats[StatType.IncomingPhysicalToForceConversionPercent].Should().Be(40);
        aegis.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(18);
        aegis.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(22);
        aegis.StatGroup.Stats[StatType.EnmityPercentAdjustment].Should().Be(25);
        aegis.StatGroup.Stats[StatType.RangedDeflectionReflectionOverridePercent].Should().Be(50);
        aegis.StatGroup.Stats[StatType.RangedDeflectionReflectionCapOverridePercent].Should().Be(125);
    }

    [Test]
    public void ForceConversionSplit_RoundsAndClampsConvertedPortion()
    {
        Combat.GetIncomingPhysicalToForceConversionPortion(100, 15).Should().Be(15);
        Combat.GetIncomingPhysicalToForceConversionPortion(100, 30).Should().Be(30);
        Combat.GetIncomingPhysicalToForceConversionPortion(10, 25).Should().Be(3); // 2.5 rounds away from zero
        Combat.GetIncomingPhysicalToForceConversionPortion(0, 30).Should().Be(0);
        Combat.GetIncomingPhysicalToForceConversionPortion(50, 0).Should().Be(0);
        Combat.GetIncomingPhysicalToForceConversionPortion(50, 150).Should().Be(50); // percent clamps to 100
        Combat.GetIncomingPhysicalToForceConversionPortion(7, 100).Should().Be(7);
    }

    [Test]
    public void SaberWardConversion_RetypesPhysicalShareToRealForceDamage()
    {
        var root = FindRepositoryRoot().FullName;

        // The converted share is dealt as a real Force damage instance (Force resistance + combat-log
        // visibility) via ApplyTriggeredDamage, not merely approximated by blending defense.
        var combat = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Service", "Combat.cs"));
        combat.Should().Contain("ApplyTriggeredDamage(attacker, defender, forcePortion, CombatDamageType.Force)");

        // The Force portion must be deferred off the native damage-roll hook (DelayCommand), not applied
        // synchronously mid-hook, or it re-enters the damage/AI chain and cascades with reflect effects.
        combat.Should().Contain("DelayCommand");

        // The native auto-attack path splits the physical hit before physical resistance.
        var native = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Native", "GetDamageRoll.cs"));
        native.Should().Contain("Combat.ApplyIncomingPhysicalToForceConversion(attacker.m_idSelf, target.m_idSelf, damageType, ref damage)");

        // Both ability damage paths do the same before their physical resistance stage.
        var ability = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Service", "Ability.cs"));
        ability.Should().Contain("Combat.ApplyIncomingPhysicalToForceConversion(activator, target, damageType, ref calculatedDamage)");
    }

    [Test]
    public void EmbattledStatusEffect_ScalesDefensesAndMobilityByStacks()
    {
        var pressure = new EmbattledStatusEffect(3, 6, 6, 10);
        pressure.Stacks.Should().Be(3);
        pressure.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(6);
        pressure.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(6);
        pressure.StatGroup.Stats[StatType.MobilityResistance].Should().Be(10);
    }

    [Test]
    public void Embattled_RefreshesOnEveryAttemptedAttackNotJustLandedHits()
    {
        var resolve = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName, "SWLOR.Game.Server", "Native", "ResolveAttackRoll.cs"));

        // Attack resolution refreshes Embattled regardless of hit/miss/deflect outcome.
        resolve.Should().Contain("EmbattledStatusEffect.Refresh(defender.m_idSelf, attacker.m_idSelf)");
    }

    [Test]
    public void DeflectingReturn_ReflectsCappedShareOfWeaponDamage()
    {
        // 20% of a 50-DMG attack = 10, under the 50% cap of a 60-DMG saber (30).
        Combat.GetRangedDeflectionReflectionAmount(50, 20, 60, 50).Should().Be(10);
        // Cap binds: 40% of 100 = 40, capped at 100% of 20 = 20.
        Combat.GetRangedDeflectionReflectionAmount(100, 40, 20, 100).Should().Be(20);
        // No reflection without a percent or without incoming damage.
        Combat.GetRangedDeflectionReflectionAmount(50, 0, 60, 50).Should().Be(0);
        Combat.GetRangedDeflectionReflectionAmount(0, 20, 60, 50).Should().Be(0);
        // No cap applied when the cap percent is 0.
        Combat.GetRangedDeflectionReflectionAmount(100, 40, 20, 0).Should().Be(40);
    }

    [Test]
    public void DeflectingReturn_UsesEmbattledBonusAndPerfectAegisFinalOverrides()
    {
        Combat.GetRangedDeflectionReflectionRates(40, 100, 2, 3, 4, 0, 0)
            .Should().Be((40, 100));
        Combat.GetRangedDeflectionReflectionRates(40, 100, 3, 3, 4, 0, 0)
            .Should().Be((44, 100));

        // Stat totals remain additive globally, so Perfect Aegis uses separate final-override stats.
        // This prevents its documented 50% / 125% ceiling from becoming 94% / 225% when the
        // permanent Deflecting Return III values are also present.
        Combat.GetRangedDeflectionReflectionRates(40, 100, 5, 3, 4, 50, 125)
            .Should().Be((50, 125));
    }

    [Test]
    public void DeflectingReturn_WiredIntoRangedDeflectionResolution()
    {
        var resolve = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName, "SWLOR.Game.Server", "Native", "ResolveAttackRoll.cs"));

        // Only Ranged Deflection reflects. A shield deflecting a ranged auto-attack cannot trigger it.
        resolve.Should().Contain("deflectionSource == DeflectionSource.Ranged");
        resolve.Should().Contain("Combat.ApplyRangedDeflectionReflection(defender.m_idSelf, attacker.m_idSelf, weaponSkillType)");
        File.ReadAllText(Path.Combine(
                FindRepositoryRoot().FullName, "SWLOR.Game.Server", "Service", "Combat.cs"))
            .Should().Contain("GetCombatImpactWeaponDamage(defender, GetEquippedWeaponSkillType(defender))");
    }

    [Test]
    public void LightsaberStances_EmitRedesignedStatusStats()
    {
        var imbuement = new ImbuementStanceStatusEffect();
        imbuement.StatGroup.Stats[StatType.StanceHostileAutoAttackForceConversion].Should().Be(1);
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
