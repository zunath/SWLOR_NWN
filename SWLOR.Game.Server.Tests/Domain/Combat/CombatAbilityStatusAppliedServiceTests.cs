using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The status-applied part of <c>Combat</c>: the party buff, the status-applied effects and their stages, the target status effects, the stamina
/// drain, the listener notices and the boot handler that registers the part.
/// </summary>
[TestFixture]
public class CombatAbilityStatusAppliedServiceTests : CombatAbilityImpactTestBase
{
    protected override object PrivateTarget => _statusApplied;

    [Test]
    public void ThePartyBuffNeedsADamagingOrStatusHitAndAFullConfiguration()
    {
        var ability = Hostile();
        _statusApplied.ApplyHostileAbilityPartyBuff(Attacker, ability, 10, false);
        Stat(StatType.HostileAbilityPartyBuffAccuracyPercent, 5);
        Stat(StatType.HostileAbilityPartyBuffDurationSeconds, 30);
        Stat(StatType.HostileAbilityPartyBuffCooldownSeconds, 12);
        Stat(StatType.HostileAbilityPartyBuffNameStrRef, 99);
        _statusApplied.ApplyHostileAbilityPartyBuff(Attacker, ability, 10, false);
        _log.Should().BeEmpty("the icon is missing");

        Stat(StatType.HostileAbilityPartyBuffIcon, (int)EffectIconType.AttackIncrease);
        _statusApplied.ApplyHostileAbilityPartyBuff(Attacker, ability, 0, false);
        _statusApplied.ApplyHostileAbilityPartyBuff(Attacker, new AbilityDetail(), 10, true);
        _statusApplied.ApplyHostileAbilityPartyBuff(Attacker, null!, 10, true);
        _log.Should().BeEmpty("a miss or a friendly ability buffs nobody");

        _statusApplied.ApplyHostileAbilityPartyBuff(Attacker, ability, 0, true);
        Lines("Apply").Should().ContainSingle().Which.Should().StartWith($"Apply {Attacker}>{Attacker} HostileAbilityPartyBuffStatusEffect");
    }

    [Test]
    public void ThePartyBuffReachesTheFriendlyTargetsOnceASequenceAndOnlyOffCooldown()
    {
        var ability = Hostile();
        Stat(StatType.HostileAbilityPartyBuffDamagePercent, 5);
        Stat(StatType.HostileAbilityPartyBuffDurationSeconds, 30);
        Stat(StatType.HostileAbilityPartyBuffCooldownSeconds, 12);
        Stat(StatType.HostileAbilityPartyBuffNameStrRef, 99);
        Stat(StatType.HostileAbilityPartyBuffIcon, (int)EffectIconType.DamageIncrease);
        _shape.AddRange(new[] { Attacker, Ally, Other });
        _hitPoints[Ally] = 20;
        _hitPoints[Other] = 20;
        _partyMembers.Add(Ally);
        _sequence = new AbilityImpactSequence();

        _statusApplied.ApplyHostileAbilityPartyBuff(Attacker, ability, 10, false);
        Lines("Apply").Select(line => line.Split(' ')[1]).Should().Equal($"{Attacker}>{Attacker}", $"{Attacker}>{Ally}");

        _log.Clear();
        _statusApplied.ApplyHostileAbilityPartyBuff(Attacker, ability, 10, false);
        _log.Should().BeEmpty("the sequence already buffed the party");

        _sequence = null;
        _blockedTriggers.Add(StatType.HostileAbilityPartyBuffDamagePercent);
        _statusApplied.ApplyHostileAbilityPartyBuff(Attacker, ability, 10, false);
        Lines("Apply").Should().BeEmpty("the buff is on cooldown");
        Lines("Trigger").Should().ContainSingle();
    }

    [Test]
    public void TheStatusAppliedEffectsNeedTheRequiredCategory()
    {
        Stat(StatType.StatusAppliedNextSkillAbilityDamageBonus, 3);
        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0, applied: StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedRequiredCategory, (int)StatusEffectCategory.Control);
        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0, applied: StatusEffectCategory.Debuff);
        Lines("NextSkill").Should().BeEmpty();

        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0, applied: StatusEffectCategory.Debuff | StatusEffectCategory.Control);
        Lines("NextSkill").Should().ContainSingle();
    }

    [Test]
    public void TheStatusAppliedEffectsGrantTheNextSkillAndAttackBonusesShowTheCueAndRunTheSelfTargetAndAllyStagesInOrder()
    {
        Stat(StatType.StatusAppliedRequiredCategory, (int)StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedNextSkillAbilitySkillType, (int)SkillType.Katar);
        Stat(StatType.StatusAppliedNextSkillAbilityDamageBonus, 3);
        Stat(StatType.StatusAppliedNextSkillAbilityCriticalRatePercentAdjustment, 5);
        Stat(StatType.StatusAppliedNextSkillAbilityWindowSeconds, 8);
        Stat(StatType.StatusAppliedNextAttackDamageBonus, 6);
        Stat(StatType.StatusAppliedNextAttackWindowSeconds, 10);
        _temporary[StatType.NextAttackStatusAppliedDMGBonus] = 2;
        Stat(StatType.StatusAppliedSelfDurationSeconds, 12);
        Stat(StatType.StatusAppliedSelfAttackDeflection, 1);
        Stat(StatType.StatusAppliedSelfDefensePercentAdjustment, 5);
        Stat(StatType.StatusAppliedSelfEvasionPercentAdjustment, 6);
        Stat(StatType.StatusAppliedSelfForceAttackPercentAdjustment, 7);
        Stat(StatType.StatusAppliedSelfHastePercentAdjustment, 8);
        Stat(StatType.StatusAppliedSelfEnmityPercentAdjustment, 9);
        Stat(StatType.StatusAppliedSelfStaminaRestore, 4);
        Stat(StatType.StatusAppliedTargetDurationSeconds, 5);
        Stat(StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment, -10);
        Stat(StatType.StatusAppliedTargetAccuracyPercentAdjustment, -15);
        Stat(StatType.StatusAppliedTargetAttackingAllyEnmityToSourcePercentAdjustment, 40);
        Stat(StatType.StatusAppliedTargetAttackingAllyEnmityDurationSeconds, 20);

        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0, applied: StatusEffectCategory.Debuff, attackingAlly: true);

        var deflection = StatRules.GetGrantedDeflectionStatType(StatType.StatusAppliedSelfAttackDeflection);
        _log.Where(line => !line.StartsWith("Record", StringComparison.Ordinal)).Should().Equal(
            $"NextSkill {Attacker} {SkillType.Katar} 3 5 8 0 {StatusEffectCategory.None}",
            $"Cue {Attacker} True 6 {ReadyCue.Damage("Charged Blows", 6)}",
            $"NextAttack {Attacker} 6 10",
            $"Replace {Attacker} {deflection} 1 12 {StatType.StatusAppliedSelfAttackDeflection}",
            $"Replace {Attacker} {StatType.PhysicalDefensePercentAdjustment} 5 12 {StatType.StatusAppliedSelfDefensePercentAdjustment}",
            $"Replace {Attacker} {StatType.EvasionPercentAdjustment} 6 12 {StatType.StatusAppliedSelfEvasionPercentAdjustment}",
            $"Replace {Attacker} {StatType.ForceAttackPercentAdjustment} 7 12 {StatType.StatusAppliedSelfForceAttackPercentAdjustment}",
            $"Replace {Attacker} {StatType.AttackDelayReductionPercent} 8 12 {StatType.StatusAppliedSelfHastePercentAdjustment}",
            $"Replace {Attacker} {StatType.EnmityPercentAdjustment} 9 12 {StatType.StatusAppliedSelfEnmityPercentAdjustment}",
            $"HitStamina {Attacker} {StatType.StatusAppliedSelfStaminaRestore} 4",
            $"Replace {Defender} {StatType.PhysicalDefensePercentAdjustment} -10 5 {StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment}",
            $"Replace {Defender} {StatType.AccuracyPercentAdjustment} -15 5 {StatType.StatusAppliedTargetAccuracyPercentAdjustment}",
            $"Apply {Attacker}>{Defender} ChallengeEnmityStatusEffect 40 20");
    }

    [Test]
    public void TheReadyCueShowsOnlyWithANextAttackWindowAndTheSelfAndTargetStagesNeedTheirDurations()
    {
        Stat(StatType.StatusAppliedRequiredCategory, (int)StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedNextAttackDamageBonus, 6);
        Stat(StatType.StatusAppliedSelfDefensePercentAdjustment, 5);
        Stat(StatType.StatusAppliedSelfStaminaRestore, 4);
        Stat(StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment, -10);
        Stat(StatType.StatusAppliedSelfDurationSeconds, 12);
        Stat(StatType.StatusAppliedNextAttackWindowSeconds, 10);

        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0, applied: StatusEffectCategory.Debuff);
        Lines("Cue").Should().Equal(new[] { $"Cue {Attacker} False 6 {ReadyCue.Damage("Charged Blows", 6)}" }, "nothing was armed before");
        Lines("Replace").Should().Equal(
            new[] { $"Replace {Attacker} {StatType.PhysicalDefensePercentAdjustment} 5 12 {StatType.StatusAppliedSelfDefensePercentAdjustment}" },
            "the target stage has no duration and the zero self stats are skipped");

        _log.Clear();
        Stat(StatType.StatusAppliedNextAttackWindowSeconds, 0);
        Stat(StatType.StatusAppliedSelfDurationSeconds, 0);
        Stat(StatType.StatusAppliedSelfStaminaRestore, 0);
        Stat(StatType.StatusAppliedTargetDurationSeconds, 5);
        _invalid.Add(Gone);
        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0, applied: StatusEffectCategory.Debuff, target: Defender);
        Lines("Cue").Should().BeEmpty();
        Lines("Replace").Should().Equal(
            $"Replace {Defender} {StatType.PhysicalDefensePercentAdjustment} -10 5 {StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment}");
        Lines("NextAttack").Should().Equal(new[] { $"NextAttack {Attacker} 6 0" }, "the grant still runs and keeps its own window check");

        _log.Clear();
        Private("ApplyStatusAppliedTargetEffects", Attacker, Gone);
        Private("ReplaceTemporaryStat", Attacker, StatType.Invalid, 5, 5, StatType.Invalid);
        Private("ReplaceTemporaryStat", Attacker, StatType.EvasionPercentAdjustment, 5, 0, StatType.Invalid);
        _log.Should().BeEmpty();
    }

    [Test]
    public void TheAttackingAllyEnmityDoesNotReplaceAStrongerChallengeFromTheSameSource()
    {
        Stat(StatType.StatusAppliedTargetAttackingAllyEnmityToSourcePercentAdjustment, 40);
        Private("ApplyStatusAppliedTargetAttackingAllyEnmity", Attacker, Defender);
        Lines("Apply").Should().BeEmpty("no duration");

        Stat(StatType.StatusAppliedTargetAttackingAllyEnmityDurationSeconds, 20);
        _invalid.Add(Gone);
        Private("ApplyStatusAppliedTargetAttackingAllyEnmity", Attacker, Gone);
        Effects(Defender,
            WithSource(new ChallengeEnmityStatusEffect(60), Attacker),
            WithSource(new SunderStatusEffect(5), Attacker));
        Private("ApplyStatusAppliedTargetAttackingAllyEnmity", Attacker, Defender);
        Lines("Apply").Should().BeEmpty("the activator's own Challenge is stronger");

        Effects(Defender, WithSource(new ChallengeEnmityStatusEffect(60), Other), WithSource(new ChallengeEnmityStatusEffect(30), Attacker));
        Private("ApplyStatusAppliedTargetAttackingAllyEnmity", Attacker, Defender);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} ChallengeEnmityStatusEffect 40 20");
    }

    [Test]
    public void TheTargetStatusEffectNeedsAHostileAbilityAValidTargetAndTheRequiredCategory()
    {
        Stat(StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment, -10);
        Stat(StatType.AbilityTargetStatusDurationSeconds, 5);
        Private("ApplyAbilityTargetStatusEffects", Attacker, Defender, Hostile());
        Stat(StatType.AbilityTargetStatusRequiredCategory, (int)StatusEffectCategory.Bleeding);
        Private("ApplyAbilityTargetStatusEffects", Attacker, Defender, Hostile());
        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        Private("ApplyAbilityTargetStatusEffects", Attacker, Defender, new AbilityDetail());
        Private("ApplyAbilityTargetStatusEffects", Attacker, Defender, null!);
        _invalid.Add(Defender);
        Private("ApplyAbilityTargetStatusEffects", Attacker, Defender, Hostile());
        _log.Should().BeEmpty();

        _invalid.Clear();
        Private("ApplyAbilityTargetStatusEffects", Attacker, Defender, Hostile());
        _log.Should().Equal($"Replace {Defender} {StatType.PhysicalDefensePercentAdjustment} -10 5 {StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment}");
    }

    [Test]
    public void TheStatusAppliedStaminaDrainShowsWhatTheTargetActuallyLost()
    {
        Stat(StatType.StatusAppliedTargetStaminaDrainRequiredCategory, (int)StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedTargetStaminaDrain, 6);
        Stat(StatType.StatusAppliedTargetStaminaDrainCooldownSeconds, 12);
        _stamina[Defender] = 4;
        _staminaDrained = 4;

        _statusApplied.ApplyStatusAppliedTargetStaminaDrain(Attacker, Defender, StatusEffectCategory.Debuff | StatusEffectCategory.Control);

        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.StatusAppliedTargetStaminaDrain} 12",
            $"Stamina {Defender}",
            $"Attribute {Defender} {StatType.StatusAppliedTargetStaminaDrain}",
            $"ReduceStamina {Defender} 6",
            $"Stamina {Defender}");
        FloatingText().Should().Equal($"{Defender} {SWLOR.Game.Server.Domain.Foundation.ColorToken.Combat("-4 STM")}");
    }

    [Test]
    public void TheStatusAppliedStaminaDrainNeedsAnotherValidCreatureTheCategoryAndItsTrigger()
    {
        _invalid.Add(Gone);
        _statusApplied.ApplyStatusAppliedTargetStaminaDrain(Gone, Defender, StatusEffectCategory.Debuff);
        _statusApplied.ApplyStatusAppliedTargetStaminaDrain(Attacker, Gone, StatusEffectCategory.Debuff);
        _statusApplied.ApplyStatusAppliedTargetStaminaDrain(Attacker, Attacker, StatusEffectCategory.Debuff);
        _statusApplied.ApplyStatusAppliedTargetStaminaDrain(Attacker, Defender, StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedTargetStaminaDrainRequiredCategory, (int)StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedTargetStaminaDrain, 6);
        _statusApplied.ApplyStatusAppliedTargetStaminaDrain(Attacker, Defender, StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedTargetStaminaDrainCooldownSeconds, 12);
        _statusApplied.ApplyStatusAppliedTargetStaminaDrain(Attacker, Defender, StatusEffectCategory.Control);
        _log.Should().BeEmpty();

        _blockedTriggers.Add(StatType.StatusAppliedTargetStaminaDrain);
        _statusApplied.ApplyStatusAppliedTargetStaminaDrain(Attacker, Defender, StatusEffectCategory.Debuff);
        _log.Should().Equal($"Trigger {Attacker} {StatType.StatusAppliedTargetStaminaDrain} 12");
        FloatingText().Should().BeEmpty();
    }

    [Test]
    public void ItHoldsTheEightServicesItReadsAndNoLazy()
    {
        var constructor = typeof(CombatAbilityStatusAppliedService).GetConstructors().Should().ContainSingle().Subject;

        constructor.GetParameters().Select(parameter => parameter.ParameterType).Should().Equal(
            typeof(ICombatResourcesService), typeof(ICombatAttackTimingService), typeof(IStatService), typeof(IStatusEffectService),
            typeof(IStatusEffectApplicationService), typeof(ITemporaryStatModifierService), typeof(IAbilityImpactState), typeof(ICombatRecorderService));
        constructor.GetParameters().Should().NotContain(parameter => parameter.ParameterType.IsGenericType, "no Lazy<> is added");
    }

    [Test]
    public void ItHoldsNoStateBeyondItsServicesSoItsBootHandlerOwnsNothingToMark()
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        typeof(CombatAbilityStatusAppliedService).GetFields(All).Where(field => !field.FieldType.IsInterface).Should().BeEmpty("it holds services only");
    }

    [Test]
    public void ItRegistersFromARerunHandlerOnModuleCacheBeforeWhereTheStayPartDidAndTheStayPartHasNone()
    {
        var handler = typeof(CombatAbilityStatusAppliedService).GetMethod(nameof(CombatAbilityStatusAppliedService.RegisterStatusEffectListener), Type.EmptyTypes)!;

        handler.GetCustomAttribute<BootPhaseAttribute>()!.Reload.Should().Be(BootReload.Rerun);
        handler.GetCustomAttributes<NWNEventHandler>().Select(attribute => attribute.Script).Should().Equal(ScriptName.OnModuleCacheBefore);
        typeof(CombatAbilityImpactService).GetMethods().SelectMany(method => method.GetCustomAttributes<NWNEventHandler>()).Should().BeEmpty();
    }

    [Test]
    public void TheBootHandlerGivesTheStatusEffectsThePartItselfAsTheListenerOncePerRun()
    {
        _statusApplied.RegisterStatusEffectListener();

        _log.Should().Equal("Listener CombatAbilityStatusAppliedService");
    }

    [Test]
    public void TheNoticesRunTheStaminaDrainAndThePartyBuffAndTheCompletedNoticeDoesNothing()
    {
        Stat(StatType.StatusAppliedTargetStaminaDrainRequiredCategory, (int)StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedTargetStaminaDrain, 6);
        Stat(StatType.StatusAppliedTargetStaminaDrainCooldownSeconds, 12);
        _stamina[Defender] = 20;
        IStatusEffectAppliedListener listener = _statusApplied;

        listener.OnStatusApplied(Attacker, Defender, StatusEffectCategory.Debuff);
        Lines("ReduceStamina").Should().Equal($"ReduceStamina {Defender} 6");

        _log.Clear();
        Stat(StatType.HostileAbilityPartyBuffDamagePercent, 10);
        Stat(StatType.HostileAbilityPartyBuffDurationSeconds, 30);
        Stat(StatType.HostileAbilityPartyBuffCooldownSeconds, 12);
        Stat(StatType.HostileAbilityPartyBuffNameStrRef, 99);
        Stat(StatType.HostileAbilityPartyBuffIcon, (int)EffectIconType.AttackIncrease);
        listener.OnHostileDebuffApplied(Attacker, Hostile());
        Lines("Apply").Should().ContainSingle().Which.Should().StartWith($"Apply {Attacker}>{Attacker} HostileAbilityPartyBuffStatusEffect");

        _log.Clear();
        listener.OnApplicationCompleted(Attacker, Defender, typeof(DazedStatusEffect), Hostile());
        _log.Should().BeEmpty();
    }

    [Test]
    public void TheStayPartNoLongerDeclaresTheStatusAppliedMembersOrTheListener()
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        var moved = new[]
        {
            "RegisterStatusEffectListener", "OnStatusApplied", "OnHostileDebuffApplied", "OnApplicationCompleted", "ApplyHostileAbilityPartyBuff",
            "ApplyStatusAppliedEffects", "ApplyStatusAppliedTargetAttackingAllyEnmity", "ApplyStatusAppliedSelfEffects", "ApplyStatusAppliedTargetEffects",
            "ReplaceTemporaryStat", "ApplyAbilityTargetStatusEffects", "ApplyStatusAppliedTargetStaminaDrain"
        };

        typeof(ICombatAbilityImpactService).GetMethods().Select(method => method.Name).Should().NotIntersectWith(moved);
        typeof(CombatAbilityImpactService).GetMethods(All).Select(method => method.Name).Should().NotIntersectWith(moved);
        typeof(CombatAbilityStatusAppliedService).GetMethods(All).Select(method => method.Name).Should().Contain(moved);
        typeof(CombatAbilityImpactService).GetInterfaces().Should().NotContain(typeof(IStatusEffectAppliedListener));
    }

    [Test]
    public void TheContainerHoldsOnePartThatIsTheListenerAndIsNotTheStayPart()
    {
        using var facades = new FacadeContainer();
        using var installed = facades.Install();

        var part = facades.Get<ICombatAbilityStatusAppliedService>();

        part.Should().BeOfType<CombatAbilityStatusAppliedService>().And.BeAssignableTo<IStatusEffectAppliedListener>();
        part.Should().NotBeSameAs(facades.Get<ICombatAbilityImpactService>());
        GameServices.CombatAbilityStatusApplied.Should().BeSameAs(part);
        facades.Get<ICombatAbilityStatusAppliedService>().Should().BeSameAs(part, "it is one singleton");
    }
}
