using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class CrossSkillPerkInteractionSafetyTests
{
    [Test]
    public void ChargedBlows_NextAttackBonusSupportsAbilitiesAndAutoAttacks()
    {
        var root = FindRepositoryRoot();
        var staff = Read(root, "SWLOR.Game.Server", "Feature", "PerkDefinition", "StaffPerkDefinition.cs");
        staff.Should().Contain("StatType.StatusAppliedNextAttackDamageBonus");
        staff.Should().Contain("StatType.StatusAppliedNextAttackWindowSeconds");
        staff.Should().NotContain("StatType.StatusAppliedNextSkillAbilitySkillType",
            "Charged Blows says next attack, not next Staff ability");

        Stat.GetStatTypeAggregation(StatType.StatusAppliedRequiredCategory)
            .Should().Be(StatTypeAggregation.BitwiseOr);
        Stat.AggregateStatAdjustment(
                StatType.StatusAppliedRequiredCategory,
                (int)StatusEffectCategory.Control,
                (int)StatusEffectCategory.Control)
            .Should().Be((int)StatusEffectCategory.Control,
                "owning Charged Blows and Skull Rattle must not turn two Control selectors into Bleeding");

        var perkSource = Read(root, "SWLOR.Game.Server", "Service", "Perk.cs");
        var getStatBonus = ExtractMethod(perkSource, "public static int GetStatBonus(uint creature, StatType stat)");
        getStatBonus.Should().Contain("Stat.AggregateStatAdjustment(stat, bonus, statBonus.Calculate(creature))");

        var combat = Read(root, "SWLOR.Game.Server", "Service", "Combat.cs");
        var statusApplied = ExtractMethod(combat, "private static void ApplyStatusAppliedEffects(");
        statusApplied.Should().Contain("GrantStatusAppliedNextAttackDamageBonus");

        var ability = Read(root, "SWLOR.Game.Server", "Service", "Ability.cs");
        var beginAbilityImpact = ExtractMethod(ability, "public static void BeginAbilityImpact(");
        beginAbilityImpact.Should().Contain("GetStatusAppliedNextAttackDamageBonus");
        beginAbilityImpact.Should().NotContain("ConsumeStatusAppliedNextAttackDamageBonus",
            "a miss or zero-damage hostile ability must preserve Charged Blows");
        beginAbilityImpact.Should().Contain("ability.IsHostileAbility");
        var hostileImpact = ExtractMethod(ability, "public static int ApplyHostileCombatImpact(");
        hostileImpact.Should().Contain("if (damage > 0)");
        hostileImpact.Should().Contain("trackedImpact?.ConsumeStatusAppliedNextAttackDamageBonus(activator);");
        hostileImpact.IndexOf(
                "trackedImpact?.ConsumeStatusAppliedNextAttackDamageBonus(activator);",
                StringComparison.Ordinal)
            .Should()
            .BeLessThan(hostileImpact.IndexOf("ApplyCombatImpactStatusEffect(", StringComparison.Ordinal),
                "an area control ability must consume the old proc before it can grant the next one");
        ability.Should().Contain("_statusAppliedNextAttackDamageBonusConsumed",
            "an area ability must not consume a newly granted proc on each additional target");
        ability.Should().Contain("public int NextAbilityDamageBonus { get; private set; }");
        var consumeStatusBonus = ExtractMethod(
            ability,
            "public void ConsumeStatusAppliedNextAttackDamageBonus(uint activator)");
        consumeStatusBonus.Should().Contain(
            "NextAbilityDamageBonus -= StatusAppliedNextAttackDamageBonus;",
            "the consumed Charged Blows value must be removed from the tracked aggregate before another hit or area target calculates damage");

        var nativeDamage = Read(root, "SWLOR.Game.Server", "Native", "GetDamageRoll.cs");
        var nextAttackBonusIndex = nativeDamage.IndexOf(
            "Combat.ConsumeStatusAppliedNextAttackDamageBonus(attacker.m_idSelf)",
            StringComparison.Ordinal);
        var formulaIndex = nativeDamage.IndexOf("CalculateDamageWithCriticalMitigation", StringComparison.Ordinal);
        nextAttackBonusIndex.Should().BeGreaterThanOrEqualTo(0);
        nextAttackBonusIndex.Should().BeLessThan(formulaIndex,
            "Charged Blows DMG must enter the attack-versus-defense formula");
        nativeDamage.Should().Contain("isLandedAttack",
            "a missed auto attack must not consume Charged Blows");
    }

    [Test]
    public void SecondaryDamage_CannotReenterDirectDamageProcOrReflectionChains()
    {
        var root = FindRepositoryRoot();
        var combat = Read(root, "SWLOR.Game.Server", "Service", "Combat.cs");
        var statusBase = Read(root, "SWLOR.Game.Server", "Service", "StatusEffectService", "StatusEffectBase.cs");
        var blazingSpikes = Read(root, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition", "BlazingSpikesStatusEffect.cs");
        var markedForDeath = Read(root, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition", "MarkedForDeathStatusEffect.cs");

        var damageEffects = ExtractMethod(combat, "public static void ApplyDamageDealtEffects(");
        var directGate = damageEffects.IndexOf("if (!appliesDirectDamageEffects)", StringComparison.Ordinal);
        directGate.Should().BeGreaterThanOrEqualTo(0);
        directGate.Should().BeLessThan(
            damageEffects.IndexOf("ApplyDamageDealtMimicryTraitProcs", StringComparison.Ordinal),
            "triggered damage and damage-over-time must exit before any direct-hit perk proc runs");
        directGate.Should().BeLessThan(
            damageEffects.IndexOf("ApplyDamageDerivedHealing", StringComparison.Ordinal),
            "secondary damage must not recursively sustain its source");

        var triggeredDamage = ExtractMethod(combat, "public static int ApplyTriggeredDamage(");
        triggeredDamage.Should().Contain(
            "ApplyDamageDealtEffects(activator, target, damage, skillType, damageType, CombatDamageDeliveryType.Triggered)");
        triggeredDamage.Should().Contain(
            "StatusEffect.NotifyDamageStatusEffects(activator, target, damage, damageType, CombatDamageDeliveryType.Triggered)");
        triggeredDamage.Should().NotContain("ApplyDamageReflectionEffects");

        statusBase.Should().Contain("if (deliveryType != CombatDamageDeliveryType.Direct)",
            "legacy and delivery-aware status hooks must ignore triggered and periodic damage");
        blazingSpikes.Should().Contain("if (deliveryType != CombatDamageDeliveryType.Direct)");
        blazingSpikes.Should().Contain("Combat.ApplyTriggeredDamage(defender, attacker, reflectedDamage, CombatDamageType.Fire)");
        markedForDeath.Should().Contain("Combat.ApplyTriggeredDamage(Source, defender, DamageBonus, damageType)");

        var statusDirectory = Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "StatusEffectDefinition");
        var deliveryAwareDamageHooks = Directory
            .EnumerateFiles(statusDirectory, "*.cs")
            .Select(path => (Path: path, Source: File.ReadAllText(path)))
            .Where(file => file.Source.Contains("protected override void OnDamage", StringComparison.Ordinal) &&
                           file.Source.Contains("CombatDamageDeliveryType deliveryType", StringComparison.Ordinal))
            .ToArray();
        deliveryAwareDamageHooks.Should().NotBeEmpty();
        foreach (var hook in deliveryAwareDamageHooks)
        {
            hook.Source.Should().Contain(
                "if (deliveryType != CombatDamageDeliveryType.Direct)",
                $"delivery-aware damage hook {Path.GetFileName(hook.Path)} must explicitly terminate secondary delivery chains");
        }

        var reflection = ExtractMethod(combat, "public static void ApplyDamageReflectionEffects(");
        reflection.Should().Contain("ApplyTriggeredDamage(defender, attacker, reflectedDamage, damageType)");
        reflection[(reflection.IndexOf('{') + 1)..].Should().NotContain("ApplyDamageReflectionEffects(");

        var reflectionDispatchFiles = Directory
            .EnumerateFiles(Path.Combine(root.FullName, "SWLOR.Game.Server"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(Path.Combine("Service", "Combat.cs"), StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains("Combat.ApplyDamageReflectionEffects(", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root.FullName, path).Replace('\\', '/'))
            .OrderBy(path => path)
            .ToArray();
        reflectionDispatchFiles.Should().Equal(
            "SWLOR.Game.Server/Native/GetDamageRoll.cs",
            "SWLOR.Game.Server/Service/Ability.cs");
    }

    [Test]
    public void DirectDamageRiders_RunOnlyForTheirIntendedDeliveryPath()
    {
        var root = FindRepositoryRoot();
        var combat = Read(root, "SWLOR.Game.Server", "Service", "Combat.cs");
        var ability = Read(root, "SWLOR.Game.Server", "Service", "Ability.cs");

        var damageEffects = ExtractMethod(combat, "public static void ApplyDamageDealtEffects(");
        damageEffects.Should().Contain("bool isAbilityDamage = false");
        damageEffects.Should().Contain("if (isAbilityDamage)");
        damageEffects.Should().Contain("ApplyPredatorsMarkEffects(attacker, defender, skillType);");
        damageEffects.Should().Contain("else");
        damageEffects.Should().Contain(
            "ApplyAutoAttackSuppressionStack(attacker, defender, skillType, damageType);");
        damageEffects.Should().Contain(
            "ApplyRangedHitSuppressionStack(attacker, defender, skillType, damageType);");
        damageEffects.Should().Contain(
            "ApplyBleedingTargetStaminaRestore(attacker, defender, skillType, isAbilityDamage);");
        damageEffects.Should().NotContain("ApplyBleedingTargetAbilityBleedRefresh(");
        damageEffects.Should().NotContain("ApplyBleedingTargetAbilityBleedSpread(");

        var abilityDamageRiders = ExtractMethod(combat, "private static void ApplyAbilityDamageRiders(");
        abilityDamageRiders.Should().Contain(
            "ApplyBleedingTargetAbilityBleedRefresh(activator, target, skillType);");
        abilityDamageRiders.Should().Contain(
            "ApplyBleedingTargetAbilityBleedSpread(activator, target, skillType, damageType);");
        abilityDamageRiders.Should().NotContain("ApplyRangedHitSuppressionStack(",
            "the shared direct-damage path already applies ranged-hit Suppression once");
        abilityDamageRiders.Should().NotContain("ApplyAutoAttackSuppressionStack(");

        var bleedingRestore = ExtractMethod(
            combat,
            "private static void ApplyBleedingTargetStaminaRestore(");
        bleedingRestore.Should().Contain("SkillDamageBleedingTargetStaminaRestoreSkillType");
        bleedingRestore.Should().Contain("SkillAbilityBleedingTargetStaminaRestoreSkillType");
        bleedingRestore.Should().Contain("if (!isAbilityDamage)");
        bleedingRestore.Should().Contain(
            "SkillAbilityBleedingTargetStaminaRestoreCooldownSeconds");

        var hostileImpact = ExtractMethod(ability, "public static int ApplyHostileCombatImpact(");
        hostileImpact.Should().Contain("isAbilityDamage: true",
            "ability impacts must select ability-only riders and exclude auto-attack procs");
    }

    [Test]
    public void DamageSharingAndRedirects_HaveNoRecursiveTransferCycle()
    {
        var root = FindRepositoryRoot();
        var combat = Read(root, "SWLOR.Game.Server", "Service", "Combat.cs");
        var modifiers = ExtractMethod(combat, "public static int ApplyDamageTakenModifiers(");
        var share = ExtractMethod(combat, "private static int ApplyDamageTakenShareToStatusSource(");
        var redirect = ExtractMethod(combat, "private static int ApplyDamageTakenRedirectToStatusSource(");

        modifiers.Should().Contain("if (deliveryType != CombatDamageDeliveryType.Transferred)");
        share.Should().Contain("CombatDamageDeliveryType.Transferred");
        share[(share.IndexOf('{') + 1)..].Should().NotContain("ApplyDamageTakenShareToStatusSource(");

        var consumeIndex = redirect.IndexOf(
            "StatusEffect.RemoveStatusEffectsWithStat(defender, StatType.DamageTakenRedirectToStatusSourcePercent, false)",
            StringComparison.Ordinal);
        var damageIndex = redirect.IndexOf("EffectDamage(finalRedirectedDamage", StringComparison.Ordinal);
        damageIndex.Should().BeGreaterThanOrEqualTo(0);
        consumeIndex.Should().BeGreaterThanOrEqualTo(0);
        consumeIndex.Should().BeLessThan(damageIndex,
            "a one-shot redirect must be consumed before its damage is dispatched");

        // The redirected portion runs the protector's own mitigation as a transfer, and the
        // one-shot consume above happens before that call - so the mutual-redirect case
        // terminates: each hop removes its own effect before recursing.
        redirect.Should().Contain("CombatDamageDeliveryType.Transferred");
        redirect.IndexOf("ApplyDamageTakenModifiers(", StringComparison.Ordinal)
            .Should().BeGreaterThan(consumeIndex,
                "the redirect must be consumed before the transfer re-enters the modifier pipeline");
    }

    [Test]
    public void CrossResourceConversions_CannotCreateAFreeResourceLoop()
    {
        var perks = BuildPerksWithout2daLookup();
        var conversionStats = new[]
        {
            StatType.AbilityStaminaCostFPRestorePercent,
            StatType.AbilityFPCostStaminaRestorePercent,
        };
        var totals = conversionStats.ToDictionary(stat => stat, _ => 0);

        foreach (var perk in perks.Where(perk => perk.IsActive))
        {
            var maxLevel = perk.PerkLevels.OrderByDescending(level => level.Key).First().Value;
            foreach (var bonus in maxLevel.StatBonuses.Where(bonus => conversionStats.Contains(bonus.Stat)))
                totals[bonus.Stat] += bonus.Calculate(0);
        }

        totals[StatType.AbilityStaminaCostFPRestorePercent].Should().Be(35);
        totals[StatType.AbilityFPCostStaminaRestorePercent].Should().Be(35);
        totals.Values.Should().OnlyContain(
            value => value > 0 && value < 100,
            "a paid ability may cross-convert part of its cost, but cannot restore its entire cost or create a self-feeding resource cycle");

        Stat.GetStatTypeAggregation(StatType.AbilityStaminaCostFPRestorePercentSkillType)
            .Should().Be(StatTypeAggregation.Maximum);
        Stat.GetStatTypeAggregation(StatType.AbilityFPCostStaminaRestorePercentSkillType)
            .Should().Be(StatTypeAggregation.Maximum);
        Stat.GetStatTypeAggregation(StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent)
            .Should().Be(StatTypeAggregation.Maximum);

        Stat.AggregateStatAdjustment(
                StatType.AbilityStaminaCostFPRestorePercentSkillType,
                (int)SkillType.Saberstaff,
                (int)SkillType.Saberstaff)
            .Should().Be((int)SkillType.Saberstaff,
                "Conduit Training and an active Conduit effect must keep the Saberstaff selector valid");
        Stat.AggregateStatAdjustment(
                StatType.AbilityFPCostStaminaRestorePercentSkillType,
                (int)SkillType.Force,
                (int)SkillType.Force)
            .Should().Be((int)SkillType.Force,
                "Conduit Training and an active Conduit effect must keep the Force selector valid");
        Stat.AggregateStatAdjustment(
                StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent,
                60,
                70)
            .Should().Be(70,
                "Balanced Current and Infinite Conduit must use the strictest active threshold, not add to 130%");

        var activeEffects = new CreatureStatusEffect();
        var lowerThreshold = new MaximumThresholdStatusEffect(60);
        var higherThreshold = new MaximumThresholdStatusEffect(70);
        activeEffects.Add(lowerThreshold);
        activeEffects.Add(higherThreshold);
        activeEffects.StatGroup.Stats[StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent]
            .Should().Be(70);
        activeEffects.Remove(higherThreshold);
        activeEffects.StatGroup.Stats[StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent]
            .Should().Be(60, "removing the stricter effect must reveal the remaining threshold");

        CalculateCrossResourceRestore(1, 35).Should().Be(0,
            "small costs must not be rounded up into guaranteed restoration");
        CalculateCrossResourceRestore(3, 35).Should().Be(1);
        CalculateCrossResourceRestore(4, 60).Should().Be(2,
            "Conduit Training plus Conduit Stance should remain useful without restoring the full cost");
        CalculateCrossResourceRestore(4, 85).Should().Be(3,
            "Conduit Training plus Infinite Conduit should still consume net resources");
        CalculateCrossResourceRestore(20, 110).Should().Be(19,
            "Training, Stance, and Infinite Conduit together must remain below full cost restoration");

        var root = FindRepositoryRoot();
        var stamina = Read(root, "SWLOR.Game.Server", "Service", "AbilityService", "AbilityRequirementStamina.cs");
        var force = Read(root, "SWLOR.Game.Server", "Service", "AbilityService", "AbilityRequirementFP.cs");
        var combat = Read(root, "SWLOR.Game.Server", "Service", "Combat.cs");

        stamina.IndexOf("Stat.ReduceStamina(player, requiredSTM)", StringComparison.Ordinal)
            .Should().BeLessThan(stamina.IndexOf("Combat.ApplyAbilityStaminaCostFPRestore", StringComparison.Ordinal));
        force.IndexOf("Stat.ReduceFP(player, requiredFP)", StringComparison.Ordinal)
            .Should().BeLessThan(force.IndexOf("Combat.ApplyAbilityFPCostStaminaRestore", StringComparison.Ordinal));

        ExtractMethod(combat, "public static void ApplyAbilityStaminaCostFPRestore(")
            .Should().NotContain("ApplyAbilityFPCostStaminaRestore");
        ExtractMethod(combat, "public static void ApplyAbilityFPCostStaminaRestore(")
            .Should().NotContain("ApplyAbilityStaminaCostFPRestore");
        ExtractMethod(combat, "public static void ApplyAbilityStaminaCostFPRestore(")
            .Should().Contain("ApplyAbilityRestoredFPEffects(creature)",
                "Energized Forms must trigger when Conduit restores FP");

        var hostileRestore = ExtractMethod(combat, "private static void ApplyHostileAbilityResourceRestoreEffects(");
        hostileRestore.Should().Contain("var restoredFP =");
        hostileRestore.Should().Contain("var restoredStamina =");
        hostileRestore.Should().Contain("if (restoredFP > 0)");
        hostileRestore.Should().Contain("if (restoredFP > 0 && restoredStamina > 0)");

        var deflectionRestore = ExtractMethod(combat, "public static void ApplyAbilityGrantedAttackDeflectionEffects(");
        deflectionRestore.Should().Contain("if (Stat.RestoreFP(activator, fpRestore) > 0)");

        var areaRestore = ExtractMethod(combat, "private static void ApplyAreaAbilityImpactEffects(");
        areaRestore.Should().Contain("var restoredFP =");
        areaRestore.Should().Contain("var restoredStamina =");
        areaRestore.Should().Contain("if (restoredFP > 0)");
        areaRestore.Should().Contain("if (restoredFP > 0 && restoredStamina > 0)");
    }

    [Test]
    public void AvoidedAttackRangedDeflectionRefresh_RunsAbilityGrantedDeflectionRiders()
    {
        var root = FindRepositoryRoot();
        var combat = Read(root, "SWLOR.Game.Server", "Service", "Combat.cs");
        var refresh = ExtractMethod(
            combat,
            "private static void ApplyAvoidedAttackAbilityUsedRangedDeflectionRefresh(");

        refresh.Should().Contain("if (StatusEffect.ApplyStatusEffect(",
            "deflection riders must run only when the refreshed status was actually applied");
        refresh.Should().Contain(
            "Stat.GetStatTypeDeflectionSource(StatType.AbilityUsedRangedDeflection)",
            "the refreshed deflection must retain the stat-declared ranged source");
        refresh.Should().Contain("ApplyAbilityGrantedAttackDeflectionEffects(creature, source)",
            "Last Word refreshes must trigger Force Gyre and any future stat-driven deflection riders");
    }

    private static int CalculateCrossResourceRestore(int cost, int percent)
    {
        return (int)typeof(Combat)
            .GetMethod("CalculateResourceRestoreFromCost", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { cost, percent })!;
    }

    [Test]
    public void CooldownReduction_CannotResetCapstonesOrRunPastReady()
    {
        var root = FindRepositoryRoot();
        var recast = Read(root, "SWLOR.Game.Server", "Service", "Recast.cs");
        var reduction = ExtractMethod(recast, "public static void ReduceRecastDelay(");

        reduction.Should().Contain("if (group == RecastGroup.Capstone)");
        reduction.Should().Contain("if (reducedDate <= now)");
        reduction.Should().Contain("ClearRecastDelay");
        reduction.Should().NotContain("ApplyRecastDelay(",
            "cooldown reduction may only shorten or clear an existing timer, never create another cooldown event");
    }

    [Test]
    public void CrossSkillDamageHealing_UsesOneAggregatePerHitCap()
    {
        var root = FindRepositoryRoot();
        var combat = Read(root, "SWLOR.Game.Server", "Service", "Combat.cs");
        var ability = Read(root, "SWLOR.Game.Server", "Service", "Ability.cs");
        var nativeDamage = Read(root, "SWLOR.Game.Server", "Native", "GetDamageRoll.cs");

        var healing = ExtractMethod(combat, "public static int ApplyDamageDerivedHealing(");
        healing.Should().Contain("state.HealingApplied");
        healing.Should().Contain("CalculateCappedDamageDerivedHealingAmount");

        var weaponScope = nativeDamage.IndexOf("using var damageDerivedHealing = Combat.BeginDamageDerivedHealing", StringComparison.Ordinal);
        weaponScope.Should().BeGreaterThanOrEqualTo(0);
        weaponScope.Should().BeLessThan(nativeDamage.IndexOf("Combat.ApplyCriticalHitEffects", weaponScope, StringComparison.Ordinal));
        weaponScope.Should().BeLessThan(nativeDamage.IndexOf("PublishDamageDealtEvent", weaponScope, StringComparison.Ordinal));

        var abilityScope = ability.IndexOf("using var damageDerivedHealing = Combat.BeginDamageDerivedHealing", StringComparison.Ordinal);
        abilityScope.Should().BeGreaterThanOrEqualTo(0);
        abilityScope.Should().BeLessThan(ability.IndexOf("ApplyDarkForceConversion", abilityScope, StringComparison.Ordinal));
        abilityScope.Should().BeLessThan(ability.IndexOf("Combat.ApplyDamageDealtEffects", abilityScope, StringComparison.Ordinal));
    }

    [Test]
    public void ReportedKatarAndSpearPerks_KeepIndependentTriggerChannelsAndBoundedDamage()
    {
        var perks = BuildPerksWithout2daLookup().ToDictionary(perk => perk.Type);

        static PerkLevel MaxLevel(PerkDetail perk) => perk.PerkLevels
            .OrderByDescending(level => level.Key)
            .First()
            .Value;
        static int StatValue(PerkLevel level, StatType stat) => level.StatBonuses
            .Single(bonus => bonus.Stat == stat)
            .Calculate(0);

        var redirectingCounter = MaxLevel(perks[PerkType.RedirectingCounter]);
        var retaliatoryFlow = MaxLevel(perks[PerkType.RetaliatoryFlow]);
        redirectingCounter.Description.Should().Be(
            "When you guard an attack, your next attack within 30 seconds gains +10% critical chance and deals +10 DMG.");
        StatValue(redirectingCounter, StatType.GuardedHitNextAttackDMGBonus)
            .Should().Be(10);
        StatValue(redirectingCounter, StatType.GuardedHitNextAttackCriticalRatePercentAdjustment)
            .Should().Be(10);
        StatValue(redirectingCounter, StatType.GuardedHitNextAttackWindowSeconds)
            .Should().Be(30);
        redirectingCounter.StatBonuses.Should().NotContain(
            bonus => bonus.Stat == StatType.GuardedHitNextSkillAbilitySkillType,
            "Redirecting Counter must work with hostile abilities and auto attacks from every skill line");
        retaliatoryFlow.Description.Should().Be(
            "After you guard a hit, your next attack within 30 seconds deals +8 DMG and generates +40 Enmity.");
        StatValue(retaliatoryFlow, StatType.GuardedHitSecondaryNextAttackDMGBonus)
            .Should().Be(8);
        StatValue(retaliatoryFlow, StatType.GuardedHitSecondaryNextAttackEnmityBonus)
            .Should().Be(40);
        StatValue(retaliatoryFlow, StatType.GuardedHitSecondaryNextAttackWindowSeconds)
            .Should().Be(30);
        retaliatoryFlow.StatBonuses.Should().NotContain(
            bonus => bonus.Stat == StatType.GuardedHitSecondaryNextSkillAbilitySkillType,
            "Retaliatory Flow must not be locked to Katar or any other weapon skill");

        var lateralFootwork = MaxLevel(perks[PerkType.LateralFootwork]);
        var mobileFootwork = MaxLevel(perks[PerkType.MobileFootwork]);
        var highGuard = MaxLevel(perks[PerkType.HighGuard]);
        var restorationStrike = MaxLevel(perks[PerkType.RestorationStrike]);
        StatValue(lateralFootwork, StatType.AbilityUsedEvasionPercentAdjustmentSkillType)
            .Should().Be((int)SkillType.Spear);
        StatValue(mobileFootwork, StatType.SecondaryAbilityUsedEvasionPercentAdjustmentSkillType)
            .Should().Be((int)SkillType.Pistol);
        mobileFootwork.StatBonuses.Should().NotContain(
            bonus => bonus.Stat == StatType.AbilityUsedEvasionPercentAdjustmentSkillType,
            "Pistol and Spear footwork selectors must not sum into an invalid skill ID");
        lateralFootwork.StatBonuses.Should().NotContain(
            bonus => bonus.Stat == StatType.SecondaryAbilityUsedEvasionPercentAdjustmentSkillType,
            "each cross-skill footwork trigger needs an independent selector channel");
        highGuard.StatBonuses.Should().NotContain(
            bonus => bonus.Stat == StatType.CostlyAbilityDamageBonusSkillType ||
                bonus.Stat == StatType.CostlyAbilityUsedEvasionPercentAdjustmentSkillType,
            "High Guard says hostile combat abilities and must work across skill lines");
        restorationStrike.StatBonuses.Should().NotContain(
            bonus => bonus.Stat == StatType.CostlyAbilityHitStaminaRestoreSkillType,
            "Restoration Strike uses the same global costly-ability contract as High Guard");
        StatValue(highGuard, StatType.CostlyAbilityDamageMinimumStaminaCost).Should().Be(8);
        StatValue(highGuard, StatType.CostlyAbilityUsedEvasionMinimumStaminaCost).Should().Be(8);
        StatValue(restorationStrike, StatType.CostlyAbilityHitStaminaRestoreMinimumStaminaCost).Should().Be(8);
        Enum.IsDefined(typeof(StatType), 778).Should().BeFalse(
            "the superseded shared costly-ability threshold must not remain as usable stat API");

        var root = FindRepositoryRoot();
        var combat = Read(root, "SWLOR.Game.Server", "Service", "Combat.cs");
        var guardedBonuses = ExtractMethod(combat, "private static void ApplyGuardedHitNextSkillAbilityEffects(");
        guardedBonuses.Should().Contain("primary.DamageBonus + secondary.DamageBonus");
        guardedBonuses.Should().Contain("Math.Max(primary.Window, secondary.Window)");

        var crossSkillCounter = ExtractMethod(combat, "private static void ApplyGuardedHitNextAttackEffects(");
        crossSkillCounter.Should().Contain("StatType.GuardedHitNextAttackDMGBonus");
        crossSkillCounter.Should().Contain("StatType.GuardedHitSecondaryNextAttackDMGBonus");
        crossSkillCounter.Should().Contain("StatType.GuardedHitSecondaryNextAttackEnmityBonus");
        crossSkillCounter.Should().Contain("primaryDMGBonus + secondaryDMGBonus");
        crossSkillCounter.Should().Contain("Math.Max(primaryWindow, secondaryWindow)",
            "independent 30-second providers must not add into a 60-second window");
        crossSkillCounter.Should().Contain("StatType.NextAttackGuardedHitDMGBonus");
        crossSkillCounter.Should().Contain("StatType.NextAttackGuardedHitEnmityBonus");

        var abilitySource = Read(root, "SWLOR.Game.Server", "Service", "Ability.cs");
        var beginAbilityImpact = ExtractMethod(abilitySource, "public static void BeginAbilityImpact(");
        beginAbilityImpact.Should().Contain("ability.IsHostileAbility");
        beginAbilityImpact.Should().Contain("ConsumeNextAttackGuardedHitBonuses");
        beginAbilityImpact.Should().Contain("guardedHitBonuses.DMGBonus");
        beginAbilityImpact.Should().Contain("guardedHitBonuses.CriticalRatePercentAdjustment");
        beginAbilityImpact.Should().Contain("guardedHitBonuses.EnmityBonus");
        abilitySource.Should().Contain("trackedImpact?.NextAttackEnmityBonus");
        var telegraphedImpact = ExtractMethod(abilitySource, "public static int ApplyTelegraphedCombatImpact(");
        telegraphedImpact.Should().Contain("trackedImpact?.NextAttackEnmityBonus ?? 0",
            "a telegraphed hostile ability must carry Retaliatory Flow's consumed Enmity into its delayed impact");
        var delayedImpact = ExtractMethod(
            abilitySource,
            "private static ApplyTelegraphEffect BuildTelegraphedCombatImpactAction(");
        delayedImpact.Should().Contain("int nextAttackEnmityBonus");
        delayedImpact.Should().Contain("nextAttackEnmityBonus,",
            "the reconstructed tracked impact must retain the guarded-hit Enmity bonus");
        delayedImpact.Should().Contain("Combat.GetStatusAppliedNextAttackDamageBonus(creator)",
            "a delayed impact must resolve the live Charged Blows proc when it lands");
        telegraphedImpact.Should().Contain("deferredNextAbilityDamageBonus");
        telegraphedImpact.Should().Contain(
            "(trackedImpact?.StatusAppliedNextAttackDamageBonus ?? 0)",
            "the cast-time Charged Blows snapshot must not be captured by a delayed impact");
        delayedImpact.Should().NotContain("int statusAppliedNextAttackDamageBonus",
            "the delayed callback must not carry a stale Charged Blows reservation");

        var nativeAttackSource = Read(root, "SWLOR.Game.Server", "Native", "ResolveAttackRoll.cs");
        nativeAttackSource.Should().Contain("ConsumeNextAttackGuardedHitCriticalRateBonus(attacker.m_idSelf)",
            "the next landed non-queued auto attack must receive Redirecting Counter's critical chance");
        var autoAttackDamage = ExtractMethod(combat, "public static int ApplyAutoAttackDamageModifiers(");
        autoAttackDamage.Should().NotContain("NextAttackGuardedHitDMGBonus",
            "DMG must enter the attack-versus-defense formula rather than being added to resolved damage");
        var nativeDamageRollSource = Read(root, "SWLOR.Game.Server", "Native", "GetDamageRoll.cs");
        var guardedDMGIndex = nativeDamageRollSource.IndexOf("guardedHitBonuses.DMGBonus", StringComparison.Ordinal);
        var calculateDamageIndex = nativeDamageRollSource.IndexOf("CalculateDamageWithCriticalMitigation", StringComparison.Ordinal);
        guardedDMGIndex.Should().BeGreaterThanOrEqualTo(0);
        guardedDMGIndex.Should().BeLessThan(calculateDamageIndex,
            "guarded-hit DMG must be part of the combat-formula input");
        nativeDamageRollSource.Should().Contain("ApplyNextAttackGuardedHitEnmityBonus(");
        var guardedHitEnmity = ExtractMethod(combat, "public static void ApplyNextAttackGuardedHitEnmityBonus(");
        guardedHitEnmity.Should().NotContain("appliedDamage",
            "a landed auto attack must grant Retaliatory Flow's Enmity even when mitigation reduces final damage to zero");

        var retaliationPulse = ExtractMethod(combat, "private static void ApplyGuardedHitRetaliationPulse(");
        retaliationPulse.Should().Contain("ApplyTriggeredDamage(");
        retaliationPulse.Should().NotContain("ApplyDamageDealtEffects(",
            "Iron Elbows pulse damage must not recursively trigger direct-damage perks");
        retaliationPulse.Should().Contain("ResolveGuardRetaliationDamage(defender, originalAttacker",
            "the triggering attacker must be affected even outside the nearby-enemy radius");
        retaliationPulse.Should().Contain("target != originalAttacker",
            "the triggering attacker must not be struck twice when inside the pulse radius");

        var guardedHitRetaliation = ExtractMethod(combat, "private static void ApplyGuardedHitRetaliation(");
        guardedHitRetaliation.Should().Contain("StatType.GuardedHitPulseDMG");
        guardedHitRetaliation.Should().NotContain("SkillTypeMatches(",
            "Iron Elbows is cross-skill and must work with any equipped weapon or unarmed");
        guardedHitRetaliation.Should().NotContain("PerkType.",
            "Iron Elbows behavior must be stat-driven rather than tied to a sibling perk");

        var evasion = ExtractMethod(combat, "private static void ApplyAbilityUsedEvasion(");
        evasion.Should().Contain("evasionStatType);",
            "each trigger family needs an independent replacement group so valid cross-skill Evasion perks can stack");

        var skillEvasion = ExtractMethod(combat, "private static void ApplyAbilityUsedSkillEvasion(");
        skillEvasion.Should().Contain("StatType.AbilityUsedEvasionPercentAdjustmentSkillType");
        skillEvasion.Should().Contain("StatType.SecondaryAbilityUsedEvasionPercentAdjustmentSkillType");
        var skillEvasionChannel = ExtractMethod(combat, "private static void ApplyAbilityUsedSkillEvasionChannel(");
        skillEvasionChannel.Should().Contain("new EvasiveFootworkStatusEffect(evasionPercent)",
            "Lateral and Mobile Footwork need a visible timed status while retaining their shared stat-driven trigger");
        skillEvasionChannel.Should().Contain("StatusEffect.ApplyStatusEffect(");

        var damageModifiers = ExtractMethod(combat, "public static int ApplyDamageDealtModifiers(");
        var outgoingDamageIndex = damageModifiers.IndexOf("ApplyOutgoingDamageModifier", StringComparison.Ordinal);
        var aggregateCapIndex = damageModifiers.IndexOf("MaximumDamageBonusPercent", StringComparison.Ordinal);
        outgoingDamageIndex.Should().BeGreaterThanOrEqualTo(0);
        aggregateCapIndex.Should().BeGreaterThan(outgoingDamageIndex,
            "Vigor Stance's global damage must participate in the shared outgoing-damage cap");

        var staminaCost = ExtractMethod(combat, "public static int GetAbilityStaminaCostFlatAdjustment(uint creature, AbilityDetail ability)");
        staminaCost.Should().Contain("if (ability.IsHostileAbility)");
        staminaCost.Should().Contain("StatType.HostileAbilityStaminaCostFlatAdjustment");

        var hostileEvasion = ExtractMethod(combat, "private static void ApplyHostileAbilityUsedEvasion(");
        hostileEvasion.Should().Contain("SkillTypeMatchesOrGlobal(skillType, requiredSkillType)",
            "an omitted selector makes Vigor Stance trigger from every hostile combat skill");
        var costlyEvasion = ExtractMethod(combat, "private static void ApplyCostlyAbilityUsedEvasion(");
        costlyEvasion.Should().Contain("SkillTypeMatchesOrGlobal(skillType, requiredSkillType)");
        var costlyDamage = ExtractMethod(combat, "public static int GetCostlyAbilityDamageBonus(");
        costlyDamage.Should().Contain("SkillTypeMatchesOrGlobal(skillType, requiredSkillType)");
        var globalSkillMatch = ExtractMethod(combat, "private static bool SkillTypeMatchesOrGlobal(");
        globalSkillMatch.Should().Contain(
            "requiredSkillType == SkillType.Invalid || SkillTypeMatches(actualSkillType, requiredSkillType)");

        combat.Should().Contain("Dictionary<(uint Creature, AbilityDetail Ability), AbilityStaminaCostState>",
            "cost-gated riders must bind the paid STM to the exact ability instead of a later ability");
        var costlyHitEffects = ExtractMethod(combat, "private static void ApplyCostlyAbilityHitEffects(");
        costlyHitEffects.Should().Contain("StatType.CostlyAbilityHitStaminaRestoreMinimumStaminaCost");
        costlyHitEffects.Should().Contain("StatType.CostlyAbilityStatusMinimumStaminaCost");
        costlyHitEffects.Should().Contain("!costState.StaminaRestoreApplied",
            "Restoration Strike may restore STM only once without consuming High Guard's Evasion context");
        costlyHitEffects.Should().Contain("SkillTypeMatchesOrGlobal(skillType, staminaRestoreSkillType)");
        costlyHitEffects.Should().Contain("SkillTypeMatchesOrGlobal(skillType, statusSkillType)");
        costlyHitEffects.Should().NotContain("_abilityStaminaCosts.Remove",
            "hit riders must not erase the context before post-use High Guard Evasion runs");
        var deferCostContext = ExtractMethod(combat, "public static void DeferAbilityStaminaCostContext(");
        deferCostContext.Should().Contain("state.DeferredImpactCount++");
        var completeCostContext = ExtractMethod(combat, "public static void CompleteAbilityStaminaCostContext(");
        completeCostContext.Should().Contain("state.DeferredImpactCount > 0");
        completeCostContext.Should().Contain("_abilityStaminaCosts.Remove((creature, ability))");
        var completeDeferredCostContext = ExtractMethod(
            combat,
            "public static void CompleteDeferredAbilityStaminaCostContext(");
        completeDeferredCostContext.Should().Contain("state.DeferredImpactCount - 1");
        completeDeferredCostContext.Should().Contain("_abilityStaminaCosts.Remove((creature, ability))");

        var usePerkFeat = Read(root, "SWLOR.Game.Server", "Feature", "UsePerkFeat.cs");
        usePerkFeat.Should().Contain("Combat.ApplyAbilityActivatedEffects(activator, target, feat, ability, summary);");
        usePerkFeat.Should().Contain("Combat.CompleteAbilityStaminaCostContext(activator, ability);");
        usePerkFeat.IndexOf("ApplyAbilityActivatedEffects(activator, target, feat, ability, summary)", StringComparison.Ordinal)
            .Should().BeLessThan(
                usePerkFeat.IndexOf("CompleteAbilityStaminaCostContext(activator, ability)", StringComparison.Ordinal),
                "High Guard must read the paid cost before the ability context is cleared");
    }

    private static IReadOnlyCollection<PerkDetail> BuildPerksWithout2daLookup()
    {
        var result = new List<PerkDetail>();
        var definitionTypes = typeof(IPerkListDefinition).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(IPerkListDefinition).IsAssignableFrom(type))
            .OrderBy(type => type.FullName)
            .ToArray();

        foreach (var definitionType in definitionTypes)
        {
            var definition = Activator.CreateInstance(definitionType)!;
            foreach (var method in definitionType
                         .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                         .Where(method => method.ReturnType == typeof(void) &&
                                          method.GetParameters().Length == 0 &&
                                          !method.Name.Contains('<'))
                         .OrderBy(method => method.MetadataToken))
            {
                method.Invoke(definition, null);
            }

            var builder = definitionType
                .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(definition)!;
            var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
                .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(builder)!;
            result.AddRange(perks.Values);
        }

        return result;
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"method signature '{signature}' must exist");
        var openBrace = source.IndexOf('{', start);
        openBrace.Should().BeGreaterThan(start);
        var depth = 0;
        for (var index = openBrace; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source[start..(index + 1)];
        }

        Assert.Fail($"Could not find the end of method '{signature}'.");
        return string.Empty;
    }

    private static string Read(DirectoryInfo root, params string[] pathParts)
    {
        return File.ReadAllText(Path.Combine(new[] { root.FullName }.Concat(pathParts).ToArray()));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory ?? throw new DirectoryNotFoundException("Could not locate SWLOR.Game.Server.sln from the test directory.");
    }

    private sealed class MaximumThresholdStatusEffect : StatusEffectBase
    {
        private readonly int _threshold;

        public override string Name => "Maximum Threshold Test";
        public override EffectIconType Icon => EffectIconType.Invalid;

        public MaximumThresholdStatusEffect()
            : this(0)
        {
        }

        public MaximumThresholdStatusEffect(int threshold)
        {
            _threshold = threshold;
            StatGroup.Stats[StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent] = threshold;
        }

        public override IStatusEffect Clone()
        {
            return new MaximumThresholdStatusEffect(_threshold);
        }
    }
}
