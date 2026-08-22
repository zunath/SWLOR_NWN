using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Leadership;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class LeadershipCombatUpgradeTests
{
    [Test]
    public void LeadershipBibleManifest_ContainsBatch()
    {
        var root = FindRepositoryRoot();
        var manifest = File.ReadAllText((root / "SWLOR.Game.Server" / "Readmes" / "CombatUpgradeBiblePerkManifest.csv").FullName);
        var perkNames = new[]
        {
            "Rallying Standard I", "Press the Attack I", "Coordinated Focus I", "Mark Target I",
            "Charge Order I", "Press the Attack II", "Rallying Standard II", "Break Morale I",
            "Coordinated Focus II", "Command Presence I", "Mark Target II", "Charge Order II",
            "Press the Attack III", "Break Morale II", "Coordinated Focus III", "Command Presence II",
            "Decisive Command", "Watchful Presence I", "Rousing Shout I", "Steady Formation I",
            "Bolster Resolve I", "Field Recovery I", "Rousing Shout II", "Watchful Presence II",
            "Cleanse Order I", "Steady Formation II", "Triage Protocol I", "Bolster Resolve II",
            "Field Recovery II", "Rousing Shout III", "Cleanse Order II", "Watchful Presence III",
            "Triage Protocol II", "Hold the Line"
        };

        foreach (var perkName in perkNames)
        {
            manifest.Should().Contain($"\"{perkName}\"");
        }
    }
    [Test]
    public void LeadershipStatuses_MatchCombatBibleStats()
    {
        AssertAppliedStat(new RallyingStandard1StatusEffect(), StatType.PhysicalAndForceAbilityHitChancePercentAdjustment, 3);
        AssertAppliedStat(new RallyingStandard1StatusEffect(), StatType.AbilityHitChancePercentAdjustment, 0);
        AssertAppliedStat(new RallyingStandard2StatusEffect(), StatType.PhysicalAndForceAbilityHitChancePercentAdjustment, 5);
        AssertAppliedStat(new RallyingStandard2StatusEffect(), StatType.AbilityHitChancePercentAdjustment, 0);
        AssertAppliedStat(new PressTheAttack1StatusEffect(), StatType.DamageDealtPercentAdjustment, 6);
        AssertAppliedStat(new PressTheAttack2StatusEffect(), StatType.DamageDealtPercentAdjustment, 8);
        AssertAppliedStat(new PressTheAttack3StatusEffect(), StatType.DamageDealtPercentAdjustment, 10);
        AssertAppliedStat(new PressTheAttack3StatusEffect(), StatType.PhysicalAndForceAbilityHitChancePercentAdjustment, 5);
        AssertAppliedStat(new PressTheAttack3StatusEffect(), StatType.AbilityHitChancePercentAdjustment, 0);
        AssertAppliedStat(new DecisiveCommand1StatusEffect(), StatType.DamageDealtPercentAdjustment, 12);
        AssertAppliedStat(new DecisiveCommand1StatusEffect(), StatType.PhysicalAndForceAbilityHitChancePercentAdjustment, 6);
        AssertAppliedStat(new DecisiveCommand1StatusEffect(), StatType.AbilityHitChancePercentAdjustment, 0);
        AssertAppliedStat(new DecisiveCommand1StatusEffect(), StatType.CriticalRatePercentAdjustment, 6);
        AssertStatusStat(new FlashStatusEffect(10), StatType.PhysicalAndForceAbilityHitChancePercentAdjustment, -10);
        AssertStatusStat(new FlashStatusEffect(10), StatType.AbilityHitChancePercentAdjustment, 0);

        AssertAppliedStat(new MarkTarget1StatusEffect(), StatType.DamageDealtPercentAdjustment, 8);
        AssertAppliedStat(new MarkTarget2StatusEffect(), StatType.DamageDealtPercentAdjustment, 12);
        AssertAppliedStat(new MarkTarget2StatusEffect(), StatType.AccuracyPercentAdjustment, 10);
        AssertAppliedStat(new MarkTarget2StatusEffect(), StatType.PhysicalAndForceAbilityHitChancePercentAdjustment, 0);

        AssertAppliedStat(new WatchfulPresence3StatusEffect(), StatType.LeadershipPhysicalDamageTakenPercentAdjustment, -8);
        AssertAppliedStat(new WatchfulPresence3StatusEffect(), StatType.LeadershipForceDamageTakenPercentAdjustment, -8);
        AssertAppliedStat(new SteadyFormation2StatusEffect(), StatType.EvasionPercentAdjustment, 5);
        AssertAppliedStat(new SteadyFormation2StatusEffect(), StatType.MindResistance, 50);
        AssertAppliedStat(new SteadyFormation2StatusEffect(), StatType.MobilityResistance, 50);
        AssertAppliedStat(new RousingShout1StatusEffect(), StatType.LeadershipPhysicalDamageTakenPercentAdjustment, -10);
        AssertAppliedStat(new RousingShout1StatusEffect(), StatType.LeadershipForceDamageTakenPercentAdjustment, -10);
        AssertAppliedStat(new RousingShout2StatusEffect(), StatType.LeadershipPhysicalDamageTakenPercentAdjustment, -15);
        AssertAppliedStat(new RousingShout2StatusEffect(), StatType.LeadershipForceDamageTakenPercentAdjustment, -15);
        AssertAppliedStat(new RousingShout3StatusEffect(), StatType.LeadershipPhysicalDamageTakenPercentAdjustment, -20);
        AssertAppliedStat(new RousingShout3StatusEffect(), StatType.LeadershipForceDamageTakenPercentAdjustment, -20);
        AssertAppliedStat(new RousingShout3StatusEffect(), StatType.DamageTakenPercentAdjustment, 0);
        AssertAppliedStat(new BolsterResolve2StatusEffect(), StatType.LeadershipPhysicalDamageTakenPercentAdjustment, -12);
        AssertAppliedStat(new BolsterResolve2StatusEffect(), StatType.LeadershipForceDamageTakenPercentAdjustment, -12);
        AssertAppliedStat(new BolsterResolve2StatusEffect(), StatType.DamageTakenPercentAdjustment, 0);
        AssertAppliedStat(new CleanseOrder2StatusEffect(), StatType.DamageTakenPercentAdjustment, 0);
        AssertAppliedStat(new CleanseOrder2StatusEffect(), StatType.PhysicalDamageTakenPercentAdjustment, 0);
        AssertAppliedStat(new CleanseOrder2StatusEffect(), StatType.ForceDamageTakenPercentAdjustment, 0);
        new CleanseOrder2StatusEffect().Categories.Should().Be(StatusEffectCategory.Buff);
        AssertAppliedStat(new TriageProtocol2StatusEffect(), StatType.HealingReceivedPercentAdjustment, 12);
        AssertAppliedStat(new HoldTheLine1StatusEffect(), StatType.DamageTakenPercentAdjustment, 0);
        AssertAppliedStat(new HoldTheLine1StatusEffect(), StatType.LeadershipPhysicalDamageTakenPercentAdjustment, -18);
        AssertAppliedStat(new HoldTheLine1StatusEffect(), StatType.LeadershipForceDamageTakenPercentAdjustment, -18);
        AssertAppliedStat(new HoldTheLine1StatusEffect(), StatType.LeadershipOtherDamageTakenPercentAdjustment, -18);
        AssertAppliedResistance(new HoldTheLine1StatusEffect(), ResistanceType.Mind, 100);
        AssertAppliedResistance(new HoldTheLine1StatusEffect(), ResistanceType.Mobility, 100);
    }

    [Test]
    public void ReportedLeadershipFailPerks_UseDynamicRadiusAndTemporaryHitPoints()
    {
        var root = FindRepositoryRoot();
        var breakMorale = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Leadership" / "BreakMoraleAbilityDefinition.cs").FullName);
        (breakMorale.Split("LeadershipAbilityEffects.ApplyLeadershipCommandRadiusBonus").Length - 1).Should().Be(2);
        var affectedTargetGuard = breakMorale.IndexOf("if (affectedCount <= 0)", StringComparison.Ordinal);
        var markTargetRider = breakMorale.IndexOf("Combat.ApplyLeadershipVanguardImpactRiders(activator);", StringComparison.Ordinal);
        affectedTargetGuard.Should().BeGreaterThanOrEqualTo(0);
        markTargetRider.Should().BeGreaterThan(affectedTargetGuard,
            "Break Morale must grant Mark Target once after at least one direct status application succeeds");

        var leadershipPerks = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "PerkDefinition" / "LeadershipVanguardCommandPerkDefinition.cs").FullName);
        var commandPresence = leadershipPerks[leadershipPerks.IndexOf("private void CommandPresence()", StringComparison.Ordinal)..];
        commandPresence = commandPresence[..commandPresence.IndexOf("private void ", 1, StringComparison.Ordinal)];
        (commandPresence.Split(".TriggerPurchase(AbilityTargeting.RefreshClientTargeting)").Length - 1).Should().Be(2);
        (commandPresence.Split(".TriggerRefund(AbilityTargeting.RefreshClientTargeting)").Length - 1).Should().Be(2);

        var cleanseOrder = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Leadership" / "CleanseOrderAbilityDefinition.cs").FullName);
        cleanseOrder.Should().Contain("AbilityEffectScaling.ScaleValueBySourceSocial(activator, 12, 15)");
        cleanseOrder.Should().Contain("GameMath.PercentOf(GetMaxHitPoints(target), percent)");
        cleanseOrder.Should().Contain("TemporaryHitPointEffects.ApplyFlatOwned(");
        var cleanseOrder1Impact = cleanseOrder[cleanseOrder.IndexOf(
            "private static void CleanseOrder1ImpactAction",
            StringComparison.Ordinal)..cleanseOrder.IndexOf(
            "private static void CleanseOrder2ImpactAction",
            StringComparison.Ordinal)];
        var cleanseOrder2Impact = cleanseOrder[cleanseOrder.IndexOf(
            "private static void CleanseOrder2ImpactAction",
            StringComparison.Ordinal)..];
        cleanseOrder1Impact.Should().Contain("new CleanseOrder1StatusEffect()");
        cleanseOrder2Impact.Should().Contain("new CleanseOrder2StatusEffect()");
        var helperStart = cleanseOrder.IndexOf("private static void ApplyCommandAndTemporaryHP", StringComparison.Ordinal);
        helperStart.Should().BeGreaterThanOrEqualTo(0);
        var helperBody = cleanseOrder[helperStart..];
        helperBody.IndexOf("StatusEffect.ApplyStatusEffect", StringComparison.Ordinal)
            .Should().BeLessThan(helperBody.IndexOf("TemporaryHitPointEffects.ApplyFlatOwned", StringComparison.Ordinal),
                "the marker must be accepted before its ID claims ownership of the temporary-HP pool");
        helperBody.Should().Contain("commandMarker.Id");

        var rank1Marker = new CleanseOrder1StatusEffect();
        rank1Marker.Icon.Should().Be(EffectIconType.CleanseOrder1StatusEffect);
        rank1Marker.Categories.Should().Be(StatusEffectCategory.Buff);
        rank1Marker.SourceType.Should().Be(StatusEffectSourceType.Command);

        var rank2Marker = new CleanseOrder2StatusEffect();
        rank2Marker.Icon.Should().Be(EffectIconType.CleanseOrder2StatusEffect);
        rank2Marker.Categories.Should().Be(StatusEffectCategory.Buff);
        rank2Marker.SourceType.Should().Be(StatusEffectSourceType.Command);

        CleanseOrder1StatusEffect.TemporaryHitPointEffectKey
            .Should().Be(CleanseOrder2StatusEffect.TemporaryHitPointEffectKey);

        foreach (var rank in new[] { "CleanseOrder1StatusEffect.cs", "CleanseOrder2StatusEffect.cs" })
        {
            var cleanseOrderStatus = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "StatusEffectDefinition" / rank).FullName);
            cleanseOrderStatus.Should().Contain("TemporaryHitPointEffects.RemoveIfCurrent(creature, TemporaryHitPointEffectKey, Id)");
        }

        var temporaryHitPoints = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "TemporaryHitPointEffects.cs").FullName);
        temporaryHitPoints.Should().Contain("SetLocalString(target, GetOwnerVariable(effectKey), ownerId)");
        temporaryHitPoints.Should().Contain("GetLocalString(target, GetOwnerVariable(effectKey)) != ownerId");
        temporaryHitPoints.Should().Contain("DeleteLocalString(target, GetOwnerVariable(effectKey))");
        temporaryHitPoints.Should().NotContain("Dictionary<(uint Target, string EffectKey)",
            "ownership belongs to the game object and must not leak through process-global tracking");

        var combat = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "Combat.cs").FullName);
        combat.Should().Contain("typeof(MarkTarget2StatusEffect)");
        combat.Should().Contain("typeof(MarkTarget1StatusEffect)");
        combat.Should().Contain("AbilityTargeting.GetFriendlyTargets(activator, activator, true, radius)");
        combat.Should().Contain("StatType.LeadershipPhysicalDamageTakenPercentAdjustment");
        combat.Should().Contain("StatType.LeadershipForceDamageTakenPercentAdjustment");
        combat.Should().Contain("StatType.LeadershipOtherDamageTakenPercentAdjustment");
    }

    [Test]
    public void LeadershipDamageReduction_ReconcilesEachChannelAndRestoresWeakerEffects()
    {
        var tracker = new CreatureStatusEffect();
        var watchful = ApplyLeadershipEffect(tracker, new WatchfulPresence3StatusEffect());
        tracker.StatGroup.Stats[StatType.LeadershipPhysicalDamageTakenPercentAdjustment].Should().Be(-8);

        var rousing1 = ApplyLeadershipEffect(tracker, new RousingShout1StatusEffect());
        tracker.StatGroup.Stats[StatType.LeadershipPhysicalDamageTakenPercentAdjustment].Should().Be(-10);
        watchful.StatGroup.Stats[StatType.LeadershipPhysicalDamageTakenPercentAdjustment].Should().Be(0,
            "the weaker Watchful Presence contribution must remain tracked without stacking");

        var holdTheLine = ApplyLeadershipEffect(tracker, new HoldTheLine1StatusEffect());
        tracker.StatGroup.Stats[StatType.LeadershipPhysicalDamageTakenPercentAdjustment].Should().Be(-18);
        tracker.StatGroup.Stats[StatType.LeadershipForceDamageTakenPercentAdjustment].Should().Be(-18);
        tracker.StatGroup.Stats[StatType.LeadershipOtherDamageTakenPercentAdjustment].Should().Be(-18);
        rousing1.StatGroup.Stats[StatType.LeadershipPhysicalDamageTakenPercentAdjustment].Should().Be(0);

        var rousing3 = ApplyLeadershipEffect(tracker, new RousingShout3StatusEffect());
        tracker.StatGroup.Stats[StatType.LeadershipPhysicalDamageTakenPercentAdjustment].Should().Be(-20);
        tracker.StatGroup.Stats[StatType.LeadershipForceDamageTakenPercentAdjustment].Should().Be(-20);
        tracker.StatGroup.Stats[StatType.LeadershipOtherDamageTakenPercentAdjustment].Should().Be(-18,
            "Rousing Shout does not cover Other damage, so Hold the Line must retain that channel");
        holdTheLine.StatGroup.Stats[StatType.LeadershipPhysicalDamageTakenPercentAdjustment].Should().Be(0);
        holdTheLine.StatGroup.Stats[StatType.LeadershipForceDamageTakenPercentAdjustment].Should().Be(0);
        holdTheLine.StatGroup.Stats[StatType.LeadershipOtherDamageTakenPercentAdjustment].Should().Be(-18);

        tracker.Remove(rousing3);
        ReconcileLeadershipDamageReduction(tracker);
        tracker.StatGroup.Stats[StatType.LeadershipPhysicalDamageTakenPercentAdjustment].Should().Be(-18,
            "removing the stronger effect must restore Hold the Line's physical contribution");
        tracker.StatGroup.Stats[StatType.LeadershipForceDamageTakenPercentAdjustment].Should().Be(-18);
        tracker.StatGroup.Stats[StatType.LeadershipOtherDamageTakenPercentAdjustment].Should().Be(-18);
        holdTheLine.StatGroup.Stats[StatType.LeadershipPhysicalDamageTakenPercentAdjustment].Should().Be(-18);

        var tiedTracker = new CreatureStatusEffect();
        var tiedA = ApplyLeadershipEffect(tiedTracker, new RousingShout1StatusEffect());
        var tiedB = ApplyLeadershipEffect(tiedTracker, new RousingShout1StatusEffect());
        tiedTracker.StatGroup.Stats[StatType.LeadershipPhysicalDamageTakenPercentAdjustment].Should().Be(-10);
        new[]
            {
                tiedA.StatGroup.Stats[StatType.LeadershipPhysicalDamageTakenPercentAdjustment],
                tiedB.StatGroup.Stats[StatType.LeadershipPhysicalDamageTakenPercentAdjustment],
            }
            .Should().BeEquivalentTo(new[] { -10, 0 },
                "equal reductions must contribute once regardless of which tied effect wins");
    }

    [Test]
    public void LeadershipDamageReduction_AppliesTypedChannelsExactlyOnceAcrossDeliveryPaths()
    {
        ApplyDamageTakenPercentageModifiers(
                100,
                CombatDamageType.Physical,
                typedLeadershipReductionAlreadyApplied: false)
            .Should().Be(72,
                "triggered and periodic physical damage apply the 20% typed reduction and then the separate 10% generic reduction");
        ApplyDamageTakenPercentageModifiers(
                100,
                CombatDamageType.Force,
                typedLeadershipReductionAlreadyApplied: false)
            .Should().Be(77,
                "triggered and periodic Force damage apply 15% typed reduction before the separate generic reduction");
        ApplyDamageTakenPercentageModifiers(
                100,
                CombatDamageType.Fire,
                typedLeadershipReductionAlreadyApplied: false)
            .Should().Be(74,
                "Other damage applies Hold-the-Line-style coverage before the separate generic reduction");

        ApplyDamageTakenPercentageModifiers(
                80,
                CombatDamageType.Physical,
                typedLeadershipReductionAlreadyApplied: true)
            .Should().Be(72,
                "direct damage that already reached 80 after the typed stage must receive only the generic stage");
        ApplyDamageTakenPercentageModifiers(
                100,
                CombatDamageType.Physical,
                typedLeadershipReductionAlreadyApplied: true)
            .Should().Be(90,
                "the guard must skip the typed stage rather than applying Leadership reduction twice");
        ApplyDamageTakenPercentageModifiers(
                82,
                CombatDamageType.Fire,
                typedLeadershipReductionAlreadyApplied: true)
            .Should().Be(74,
                "direct Other damage that already received Leadership must receive only the generic stage");

        var forcePortion = Combat.GetIncomingPhysicalToForceConversionPortion(100, 40);
        var physicalPortion = 100 - forcePortion;
        var adjustedPhysicalPortion = ApplyTypedLeadershipDamageTakenPercentageModifier(
                physicalPortion,
                CombatDamageType.Physical,
                physicalAdjustment: -20,
                forceAdjustment: -15,
                otherAdjustment: -18);
        var adjustedForcePortion = ApplyTypedLeadershipDamageTakenPercentageModifier(
                forcePortion,
                CombatDamageType.Force,
                physicalAdjustment: -20,
                forceAdjustment: -15,
                otherAdjustment: -18);
        var adjustedOtherDamage = ApplyTypedLeadershipDamageTakenPercentageModifier(
            100,
            CombatDamageType.Fire,
            physicalAdjustment: -20,
            forceAdjustment: -15,
            otherAdjustment: -18);
        adjustedPhysicalPortion.Should().Be(48);
        adjustedForcePortion.Should().Be(34,
                "the converted share must use Force Leadership rather than inheriting the physical reduction");
        adjustedOtherDamage.Should().Be(82,
            "the Other Leadership channel must use the same independent typed stage as Physical and Force");
        (adjustedPhysicalPortion + adjustedForcePortion).Should().Be(82,
            "different typed channels must reduce their own post-split portions exactly once");
    }

    [Test]
    public void LeadershipPhysicalAndForceReduction_CoversNonDirectDamageWithoutDoubleApplyingDirectDamage()
    {
        var root = FindRepositoryRoot();
        var combat = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "Combat.cs").FullName);
        var genericStage = combat[combat.IndexOf(
            "public static int ApplyDamageTakenModifiers(",
            StringComparison.Ordinal)..combat.IndexOf(
            "private static int ApplyDamageTakenPercentageModifiers(",
            StringComparison.Ordinal)];
        var percentageStage = combat[combat.IndexOf(
            "private static int ApplyDamageTakenPercentageModifiers(",
            StringComparison.Ordinal)..combat.IndexOf(
            "private static int ApplyDamageTakenRedirectToStatusSource(",
            StringComparison.Ordinal)];
        var typedStage = combat[combat.IndexOf(
            "private static int ApplyTargetStatusDamageModifiers(",
            StringComparison.Ordinal)..combat.IndexOf(
            "public static int ApplyTwinBladeAbilityShapeDamageModifier(",
            StringComparison.Ordinal)];

        genericStage.Should().Contain("bool typedLeadershipReductionAlreadyApplied = false");
        genericStage.Should().Contain("ApplyDamageTakenPercentageModifiers(");
        genericStage.Should().Contain("StatType.LeadershipPhysicalDamageTakenPercentAdjustment");
        genericStage.Should().Contain("StatType.LeadershipForceDamageTakenPercentAdjustment");
        genericStage.Should().Contain("StatType.LeadershipOtherDamageTakenPercentAdjustment");
        percentageStage.Should().Contain("if (!typedLeadershipReductionAlreadyApplied)");
        percentageStage.Should().Contain("damage = ApplyTypedLeadershipDamageTakenPercentageModifier(");
        percentageStage.IndexOf("damage = ApplyTypedLeadershipDamageTakenPercentageModifier(", StringComparison.Ordinal)
            .Should().BeLessThan(percentageStage.IndexOf(
                "return genericAdjustment == 0",
                StringComparison.Ordinal),
                "typed Leadership and generic damage reduction must remain separate multiplicative stages");
        typedStage.Should().NotContain("StatType.LeadershipPhysicalDamageTakenPercentAdjustment");
        typedStage.Should().NotContain("StatType.LeadershipForceDamageTakenPercentAdjustment");

        var directTypedStage = combat[combat.IndexOf(
            "public static int ApplyTypedLeadershipDamageTakenModifier(",
            StringComparison.Ordinal)..combat.IndexOf(
            "private static int ApplyTypedLeadershipDamageTakenPercentageModifier(",
            StringComparison.Ordinal)];
        directTypedStage.Should().Contain("StatType.LeadershipPhysicalDamageTakenPercentAdjustment");
        directTypedStage.Should().Contain("StatType.LeadershipForceDamageTakenPercentAdjustment");
        directTypedStage.Should().Contain("StatType.LeadershipOtherDamageTakenPercentAdjustment");

        var ability = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "Ability.cs").FullName);
        (ability.Split("typedLeadershipReductionAlreadyApplied: true").Length - 1).Should().Be(2,
            "both direct ability damage paths apply typed Leadership in the explicit post-conversion stage");
        (ability.Split("ApplyTypedLeadershipDamageTakenModifier(target, calculatedDamage, damageType)").Length - 1)
            .Should().Be(2);

        var nativeDamageRoll = File.ReadAllText((root / "SWLOR.Game.Server" / "Native" / "GetDamageRoll.cs").FullName);
        nativeDamageRoll.Should().Contain("typedLeadershipReductionAlreadyApplied: true",
            "the direct weapon pipeline applies typed Leadership in the explicit post-conversion stage");
        nativeDamageRoll.Should().Contain(
            "ApplyTypedLeadershipDamageTakenModifier(target.m_idSelf, damage, damageType)");

        var triggeredDamage = combat[combat.IndexOf(
            "public static int ApplyTriggeredDamage(",
            StringComparison.Ordinal)..];
        triggeredDamage = triggeredDamage[..triggeredDamage.IndexOf("public static ", 1, StringComparison.Ordinal)];
        triggeredDamage.Should().Contain("bool typedLeadershipReductionAlreadyApplied = false");
        triggeredDamage.Should().Contain(
            "typedLeadershipReductionAlreadyApplied: typedLeadershipReductionAlreadyApplied");

        var conversion = combat[combat.IndexOf(
            "public static int ApplyIncomingPhysicalToForceConversion(",
            StringComparison.Ordinal)..combat.IndexOf(
            "public static int ApplyStatusSourceAccuracyModifiers(",
            StringComparison.Ordinal)];
        conversion.Should().Contain("typedLeadershipReductionAlreadyApplied: false",
            "the converted Force portion must apply its own typed Leadership channel");
    }

    [Test]
    public void FieldRecoveryStatusEffects_UseCombatBibleRecoveryTick()
    {
        new FieldRecovery1StatusEffect().Frequency.Should().Be(4f);
        new FieldRecovery2StatusEffect().Frequency.Should().Be(4f);
    }

    [Test]
    public void LeadershipFeatAndAbilityIcons_AreAssignedAndUnique()
    {
        var labels = new HashSet<string>
        {
            "PressTheAttack1", "PressTheAttack2", "BreakMorale1",
            "PressTheAttack3", "BreakMorale2", "DecisiveCommand1",
            "RousingShout1", "RousingShout2", "CleanseOrder1",
            "RousingShout3", "CleanseOrder2", "HoldTheLine1",
            "RallyingStandard1", "CoordinatedFocus1", "ChargeOrder1", "RallyingStandard2",
            "CoordinatedFocus2", "ChargeOrder2", "CoordinatedFocus3", "WatchfulPresence1",
            "SteadyFormation1", "FieldRecovery1", "WatchfulPresence2", "SteadyFormation2",
            "FieldRecovery2", "WatchfulPresence3"
        };
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da", "ICON");
        var spellRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da", "IconResRef");
        var targetIcons = new List<string>();

        foreach (var label in labels)
        {
            var featIcon = featRows.Values.Should().ContainSingle(x => x.Label == label).Which.Icon;
            var spellIcon = spellRows.Values.Should().ContainSingle(x => x.Label == label).Which.Icon;
            targetIcons.Add(featIcon);

            featIcon.Should().NotBe("****");
            spellIcon.Should().NotBe("****");
            featIcon.Should().Be(spellIcon);

            featRows.Values.Where(x => x.Icon == featIcon && !labels.Contains(x.Label)).Should().BeEmpty();
            spellRows.Values.Where(x => x.Icon == spellIcon && !labels.Contains(x.Label)).Should().BeEmpty();
            File.Exists((root / "SWLOR_Haks" / "sw_ability" / $"{featIcon}.tga").FullName).Should().BeTrue();
        }

        targetIcons.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void LeadershipFeatAndAbilityDescriptions_MatchCombatBible()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2daRows(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var abilityRows = Read2daRows(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");
        var tlkEntries = ReadTlkEntries(root / "SWLOR_Haks" / "sw_tlk" / "sw_tlk.tlk.json");
        const int CustomTlkOffset = 16777216;
        var descriptions = new[]
        {
            (FeatType.RallyingStandard1, "Aura: Party members within Leadership range (5m base) gain +3% physical and Force ability hit chance. SOC scaling can raise this to +4%."),
            (FeatType.PressTheAttack1, "Party members within Leadership range (5m base) deal +6% damage for 30 seconds. SOC scaling can raise this to +8%."),
            (FeatType.CoordinatedFocus1, "Aura: Party members within Leadership range (5m base) gain +3% critical hit chance. SOC scaling can raise this to +4%."),
            (FeatType.ChargeOrder1, "Aura: Party members within Leadership range (5m base) gain +10% movement speed and +30 Mobility Resistance. SOC scaling can raise these to +12% movement speed and +40 Mobility Resistance."),
            (FeatType.PressTheAttack2, "Party members within Leadership range (5m base) deal +8% damage for 30 seconds. SOC scaling can raise this to +10%."),
            (FeatType.RallyingStandard2, "Aura: Party members within Leadership range (5m base) gain +5% physical and Force ability hit chance. SOC scaling can raise this to +6%."),
            (FeatType.BreakMorale1, "Enemies within Leadership range (5m base) suffer Flash for 30 seconds, reducing physical and Force ability hit chance by 10%. SOC scaling can raise the penalty to 12%. This command applies reliably to valid enemies within Leadership range (5m base)."),
            (FeatType.CoordinatedFocus2, "Aura: Party members within Leadership range (5m base) gain +4% critical hit chance and +5% critical damage. SOC scaling can raise these to +5% and +7%."),
            (FeatType.ChargeOrder2, "Aura: Party members within Leadership range (5m base) gain +15% movement speed and +50 Mobility Resistance. SOC scaling can raise these to +18% movement speed and +65 Mobility Resistance."),
            (FeatType.PressTheAttack3, "Party members within Leadership range (5m base) gain +10% damage and +5% physical and Force ability hit chance for 30 seconds. SOC scaling can raise these to +12% damage and +7% hit chance."),
            (FeatType.BreakMorale2, "Enemies within Leadership range (5m base) suffer Flash, reducing physical and Force ability hit chance by 15%, and Weakened, reducing Attack by 12%, for 30 seconds. SOC scaling can raise these penalties to 18% and 15%. This command applies reliably to valid enemies within Leadership range (5m base)."),
            (FeatType.CoordinatedFocus3, "Aura: Party members within Leadership range (5m base) gain +6% critical hit chance and +8% critical damage. SOC scaling can raise these to +7% and +10%."),
            (FeatType.DecisiveCommand1, "For 45 seconds, party members within Leadership range (5m base), including you, gain +12% damage, +6% physical and Force ability hit chance, +6% critical hit chance, and restore 1 STM every 3 seconds. SOC scaling can raise the bonuses to +15%, +8%, and +8%."),
            (FeatType.WatchfulPresence1, "Aura: Party members within Leadership range (5m base) take 4% less physical and Force damage. SOC scaling can raise this to 5%."),
            (FeatType.RousingShout1, "Bolsters one living ally, granting temporary HP equal to 10% of maximum HP for 30 seconds. SOC scaling can raise this to 13%. If the target is at or below 35% HP, they also take 10% less physical and Force damage, scaling up to 12%."),
            (FeatType.SteadyFormation1, "Aura: Party members within Leadership range (5m base) gain +3% evasion chance, +30 Mind Resistance, and +30 Mobility Resistance. SOC scaling can raise these to +4% evasion chance, +40 Mind Resistance, and +40 Mobility Resistance."),
            (FeatType.FieldRecovery1, "Aura: Party members within Leadership range (5m base) restore 1 STM every 4 seconds. SOC scaling can raise this to 2 STM per tick."),
            (FeatType.RousingShout2, "Bolsters one living ally, granting temporary HP equal to 15% of maximum HP for 30 seconds. SOC scaling can raise this to 19%. If the target is at or below 35% HP, they also take 15% less physical and Force damage, scaling up to 18%."),
            (FeatType.WatchfulPresence2, "Aura: Party members within Leadership range (5m base) take 6% less physical and Force damage. SOC scaling can raise this to 7%."),
            (FeatType.CleanseOrder1, "Removes one standard elemental or trauma ailment from party members within Leadership range (5m base) and grants temporary HP equal to 6% of maximum HP for 30 seconds. SOC scaling can raise temporary HP to 8%."),
            (FeatType.SteadyFormation2, "Aura: Party members within Leadership range (5m base) gain +5% evasion chance, +50 Mind Resistance, and +50 Mobility Resistance. SOC scaling can raise these to +6% evasion chance, +65 Mind Resistance, and +65 Mobility Resistance."),
            (FeatType.FieldRecovery2, "Aura: Party members within Leadership range (5m base) restore 2 STM every 4 seconds. SOC scaling can raise this to 3 STM per tick."),
            (FeatType.RousingShout3, "Bolsters one living ally, granting temporary HP equal to 20% of maximum HP for 30 seconds. SOC scaling can raise this to 25%. If the target is at or below 35% HP, they also take 20% less physical and Force damage, scaling up to 25%."),
            (FeatType.CleanseOrder2, "Removes one major negative status effect from party members within Leadership range (5m base) and grants temporary HP equal to 12% of maximum HP for 30 seconds. SOC scaling can raise temporary HP to 15%."),
            (FeatType.WatchfulPresence3, "Aura: Party members within Leadership range (5m base) take 8% less physical and Force damage. SOC scaling can raise this to 10%."),
            (FeatType.HoldTheLine1, "For 45 seconds, party members within Leadership range (5m base), including you, gain temporary HP equal to 18% of maximum HP, take 18% less damage, and become immune to Mind and Mobility effects. SOC scaling can raise temporary HP and damage reduction to 22%.")
        };

        foreach (var (featType, expectedDescription) in descriptions)
        {
            var featRow = featRows[(int)featType];
            var featDescriptionId = int.Parse(featRow["DESCRIPTION"]) - CustomTlkOffset;
            tlkEntries[featDescriptionId].Should().Be(expectedDescription);

            var abilityRow = abilityRows[int.Parse(featRow["SPELLID"])];
            var abilityDescriptionId = int.Parse(abilityRow["SpellDesc"]) - CustomTlkOffset;
            tlkEntries[abilityDescriptionId].Should().Be(expectedDescription);
        }
    }

    [Test]
    public void LeadershipAreaAbility2daRows_MatchTargeting()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2daRows(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var abilityRows = Read2daRows(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");
        var expectedAreaRows = new[]
        {
            FeatType.CleanseOrder2,
        };

        foreach (var featType in expectedAreaRows)
        {
            var featRow = featRows[(int)featType];
            var abilityRow = abilityRows[int.Parse(featRow["SPELLID"])];

            featRow["TARGETSELF"].Should().Be("1");
            abilityRow["Range"].Should().Be("P");
            abilityRow["TargetType"].Should().Be("0x01");
            abilityRow["TargetShape"].Should().Be("sphere");
            abilityRow["TargetSizeX"].Should().Be("5");
            abilityRow["TargetFlags"].Should().Be("17");
        }
    }

    private static void AssertAppliedStat(IStatusEffect statusEffect, StatType statType, int expected)
    {
        statusEffect.ApplyEffect(0, 0, -1);
        statusEffect.StatGroup.Stats[statType].Should().Be(expected);
    }

    private static T ApplyLeadershipEffect<T>(CreatureStatusEffect tracker, T effect)
        where T : IStatusEffect, ILeadershipDamageReductionStatusEffect
    {
        effect.ApplyEffect(0, 0, -1);
        tracker.Add(effect);
        ReconcileLeadershipDamageReduction(tracker);
        return effect;
    }

    private static void ReconcileLeadershipDamageReduction(CreatureStatusEffect tracker)
    {
        var method = typeof(StatusEffect).GetMethod(
            "ReconcileLeadershipDamageReduction",
            BindingFlags.Static | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        method!.Invoke(null, new object[] { tracker });
    }

    private static int ApplyDamageTakenPercentageModifiers(
        int damage,
        CombatDamageType damageType,
        bool typedLeadershipReductionAlreadyApplied)
    {
        var method = typeof(Combat).GetMethod(
            "ApplyDamageTakenPercentageModifiers",
            BindingFlags.Static | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        method!.GetParameters().Select(x => x.Name).Should().Equal(
            "damage",
            "damageType",
            "leadershipPhysicalAdjustment",
            "leadershipForceAdjustment",
            "leadershipOtherAdjustment",
            "genericAdjustment",
            "typedLeadershipReductionAlreadyApplied");
        return (int)method.Invoke(null, new object[]
        {
            damage,
            damageType,
            -20,
            -15,
            -18,
            -10,
            typedLeadershipReductionAlreadyApplied,
        })!;
    }

    private static int ApplyTypedLeadershipDamageTakenPercentageModifier(
        int damage,
        CombatDamageType damageType,
        int physicalAdjustment,
        int forceAdjustment,
        int otherAdjustment)
    {
        var method = typeof(Combat).GetMethod(
            "ApplyTypedLeadershipDamageTakenPercentageModifier",
            BindingFlags.Static | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        method!.GetParameters().Select(x => x.Name).Should().Equal(
            "damage",
            "damageType",
            "leadershipPhysicalAdjustment",
            "leadershipForceAdjustment",
            "leadershipOtherAdjustment");
        return (int)method.Invoke(null, new object[]
        {
            damage,
            damageType,
            physicalAdjustment,
            forceAdjustment,
            otherAdjustment,
        })!;
    }

    private static void AssertAppliedResistance(IStatusEffect statusEffect, ResistanceType resistanceType, int expected)
    {
        statusEffect.ApplyEffect(0, 0, -1);
        statusEffect.StatGroup.Resists[resistanceType].Should().Be(expected);
    }

    private static void AssertStatusStat(IStatusEffect statusEffect, StatType statType, int expected)
    {
        statusEffect.StatGroup.Stats[statType].Should().Be(expected);
    }

    private static void AssertSingleAura(
        Dictionary<FeatType, AbilityDetail> abilities,
        FeatType featType,
        string name,
        RecastGroup recastGroup)
    {
        abilities.Keys.Should().Equal(featType);
        AssertAura(abilities[featType], name, 1, recastGroup);
    }

    private static void AssertAura(
        AbilityDetail ability,
        string name,
        int level,
        RecastGroup recastGroup)
    {
        AssertAbility(ability, name, level, recastGroup, 60f, 2f, null, false, true, false, false);
    }

    private static void AssertAbility(
        AbilityDetail ability,
        string name,
        int level,
        RecastGroup recastGroup,
        float recastSeconds,
        float activationSeconds,
        int? staminaCost,
        bool isHostile,
        bool isArea,
        bool isSingleTarget,
        bool requiresTarget)
    {
        ability.Name.Should().Be(name);
        ability.AbilityLevel.Should().Be(level);
        ability.SkillType.Should().Be(SkillType.Leadership);
        ability.RecastGroup.Should().Be(recastGroup);
        ability.RecastDelay(0).Should().Be(recastSeconds);
        ability.ActivationDelay(0, 0, level).Should().Be(activationSeconds);
        ability.ActivationType.Should().Be(AbilityActivationType.Casted);
        ability.IsHostileAbility.Should().Be(isHostile);
        ability.RequiresTarget.Should().Be(requiresTarget);
        ability.IsAreaAbility.Should().Be(isArea);
        ability.IsSingleTargetAbility.Should().Be(isSingleTarget);
        ability.BreaksStealth.Should().BeTrue();
        ability.Requirements.OfType<AbilityRequirementFP>().Should().BeEmpty();

        if (staminaCost.HasValue)
        {
            ability.Requirements
                .OfType<AbilityRequirementStamina>()
                .Should()
                .ContainSingle()
                .Which
                .RequiredSTM
                .Should()
                .Be(staminaCost.Value);
        }
        else
        {
            ability.Requirements.OfType<AbilityRequirementStamina>().Should().BeEmpty();
        }
    }

    private static void AssertPerkLevel(
        PerkDetail perk,
        string name,
        int level,
        int price,
        int? leadershipRequirement,
        FeatType? grantedFeat,
        params (StatType Stat, int Value)[] statBonuses)
    {
        perk.Name.Should().Be(name);
        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);

        if (leadershipRequirement.HasValue)
        {
            var requirement = perkLevel.Requirements
                .OfType<PerkRequirementSkill>()
                .Should()
                .ContainSingle()
                .Which;

            requirement.Type.Should().Be(SkillType.Leadership);
            requirement.RequiredRank.Should().Be(leadershipRequirement.Value);
        }
        else
        {
            perkLevel.Requirements.OfType<PerkRequirementSkill>().Should().BeEmpty();
        }

        if (grantedFeat.HasValue)
            perkLevel.GrantedFeats.Should().ContainSingle().Which.Should().Be(grantedFeat.Value);
        else
            perkLevel.GrantedFeats.Should().BeEmpty();

        perkLevel.StatBonuses.Should().HaveCount(statBonuses.Length);
        foreach (var (stat, value) in statBonuses)
        {
            perkLevel.StatBonuses
                .Should()
                .ContainSingle(x => x.Stat == stat)
                .Which
                .Calculate(0)
                .Should()
                .Be(value);
        }
    }

    private static void AssertPerkCategory(
        Dictionary<PerkType, PerkDetail> perks,
        PerkCategoryType category)
    {
        foreach (var perk in perks.Values)
        {
            perk.Category.Should().Be(category);
        }
    }

    private static Dictionary<PerkType, PerkDetail> BuildPerksWithout2daLookup<T>(T definition, params string[] methodNames)
    {
        foreach (var methodName in methodNames)
        {
            typeof(T)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(T)
            .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition);

        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(builder)!;
    }

    private static Dictionary<int, TwoDaIconRow> Read2da(PathInfo path, string iconColumn)
    {
        var lines = File.ReadAllLines(path.FullName)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        var header = lines[1].Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        var labelIndex = Array.IndexOf(header, "LABEL");
        if (labelIndex < 0)
            labelIndex = Array.IndexOf(header, "Label");

        var iconIndex = Array.IndexOf(header, iconColumn);
        var result = new Dictionary<int, TwoDaIconRow>();

        foreach (var line in lines.Skip(2))
        {
            var cells = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(cells[0], out var row))
                continue;

            result[row] = new TwoDaIconRow(cells[labelIndex + 1], cells[iconIndex + 1]);
        }

        return result;
    }

    private static Dictionary<int, Dictionary<string, string>> Read2daRows(PathInfo path)
    {
        var lines = File.ReadAllLines(path.FullName)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        var header = lines[1].Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        var result = new Dictionary<int, Dictionary<string, string>>();

        foreach (var line in lines.Skip(2))
        {
            var cells = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(cells[0], out var row))
                continue;

            var values = new Dictionary<string, string>();
            for (var index = 0; index < header.Length && index + 1 < cells.Length; index++)
            {
                values[header[index]] = cells[index + 1];
            }

            result[row] = values;
        }

        return result;
    }

    private static Dictionary<int, string> ReadTlkEntries(PathInfo path)
    {
        var tlk = JsonSerializer.Deserialize<TlkFile>(File.ReadAllText(path.FullName))!;
        return tlk.Entries.ToDictionary(entry => entry.Id, entry => entry.Text);
    }

    private static PathInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = directory.FullName;
            if (File.Exists(Path.Combine(candidate, "SWLOR.Game.Server.sln")) &&
                File.Exists(Path.Combine(candidate, "SWLOR_Haks", "sw_2da", "feat.2da")))
            {
                return new PathInfo(candidate);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private sealed record TwoDaIconRow(string Label, string Icon);

    private sealed record TlkFile([property: JsonPropertyName("entries")] TlkEntry[] Entries);

    private sealed record TlkEntry(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("text")] string Text);

    private sealed record PathInfo(string FullName)
    {
        public static PathInfo operator /(PathInfo path, string child)
        {
            return new PathInfo(Path.Combine(path.FullName, child));
        }
    }
}
