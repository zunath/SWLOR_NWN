using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A status effect a test sets up field by field. It records what the service runs on it in <see cref="Log"/>; a test
/// shares the bench's log so the order against everything else the service does is visible.
/// </summary>
public class ProbeEffect : StatusEffectBase
{
    public string NameValue { get; set; } = "Probe";
    public EffectIconType IconValue { get; set; } = EffectIconType.Invalid;
    public StatusEffectCategory CategoriesValue { get; set; } = StatusEffectCategory.None;
    public StatusEffectStackType StackValue { get; set; } = StatusEffectStackType.Disabled;
    public StatusEffectActivationType ActivationValue { get; set; } = StatusEffectActivationType.Tick;
    public StatusEffectSourceType SourceValue { get; set; } = StatusEffectSourceType.Normal;
    public StatusEffectCleanseType CleanseValue { get; set; } = StatusEffectCleanseType.None;
    public ResistanceType ResistanceValue { get; set; } = ResistanceType.Invalid;
    public float FrequencyValue { get; set; } = 1f;
    public bool SendsApplication { get; set; } = true;
    public bool SendsWornOff { get; set; } = true;
    public bool Persists { get; set; } = true;
    public bool PreservesTicks { get; set; }
    public string CanApplyAnswer { get; set; } = string.Empty;
    public Func<IStatusEffect, bool>? RefreshAnswer { get; set; }
    public bool FlagOnApply { get; set; }
    public bool FlagOnTick { get; set; }
    public List<string> Log { get; set; } = new();

    public override string Name => NameValue;
    public override EffectIconType Icon => IconValue;
    public override StatusEffectCategory Categories => CategoriesValue;
    public override StatusEffectStackType StackingType => StackValue;
    public override StatusEffectActivationType ActivationType => ActivationValue;
    public override StatusEffectSourceType SourceType => SourceValue;
    public override StatusEffectCleanseType CleanseTypes => CleanseValue;
    public override ResistanceType ResistanceType => ResistanceValue;
    public override float Frequency => FrequencyValue;
    public override bool SendsApplicationMessage => SendsApplication;
    public override bool SendsWornOffMessage => SendsWornOff;
    public override bool PersistsOnLogout => Persists;
    public override bool PreservesTickScheduleOnRefresh => PreservesTicks;

    public void Flag() => IsFlaggedForRemoval = true;

    public override string CanApply(uint creature)
    {
        Log.Add($"CanApply:{Name}");
        return CanApplyAnswer;
    }

    public override bool CanRefreshInPlace(IStatusEffect active) => RefreshAnswer?.Invoke(active) ?? false;

    protected override void Apply(uint creature, int durationTicks)
    {
        Log.Add($"Apply:{Name}:{creature}:{durationTicks}");
        if (FlagOnApply)
            IsFlaggedForRemoval = true;
    }

    protected override void Tick(uint creature)
    {
        Log.Add($"Tick:{Name}:{creature}");
        if (FlagOnTick)
            IsFlaggedForRemoval = true;
    }

    protected override void Reapply(uint creature) => Log.Add($"Reapply:{Name}:{creature}");

    protected override void Remove(uint creature) => Log.Add($"Remove:{Name}:{creature}:{IsBeingReplaced}");

    protected override void OnHit(uint creature, uint target, int damage) => Log.Add($"OnHit:{Name}:{creature}:{target}:{damage}");

    protected override void OnDamageDealt(uint attacker, uint defender, int damage, CombatDamageType damageType) =>
        Log.Add($"OnDamageDealt:{Name}:{attacker}:{defender}:{damage}:{damageType}");

    protected override void OnDamageTaken(uint defender, uint attacker, int damage, CombatDamageType damageType) =>
        Log.Add($"OnDamageTaken:{Name}:{defender}:{attacker}:{damage}:{damageType}");
}

/// <summary>A second status type, so a test can tell two types apart.</summary>
public class OtherProbeEffect : ProbeEffect
{
}

/// <summary>A third status type.</summary>
public class ThirdProbeEffect : ProbeEffect
{
}

/// <summary>Removed from every target when its source logs out.</summary>
public class RemoveOnExitProbeEffect : ProbeEffect, IRemoveWhenSourceExits
{
}

/// <summary>Notified after it is removed and after it is restored on login.</summary>
public class HandlerProbeEffect : ProbeEffect, IStatusEffectRemovedHandler, IStatusEffectRestoredHandler
{
    public void AfterRemoved(uint creature) => Log.Add($"AfterRemoved:{Name}:{creature}");

    public void AfterRestored(uint creature) => Log.Add($"AfterRestored:{Name}:{creature}");
}

/// <summary>A member of the Leadership damage reduction family.</summary>
public class LeadershipProbeEffect : ProbeEffect, ILeadershipDamageReductionStatusEffect
{
    public Dictionary<StatType, int> Nominal { get; } = new();

    public IReadOnlyDictionary<StatType, int> LeadershipDamageReductionStats => Nominal;
}

/// <summary>A second member of the Leadership damage reduction family.</summary>
public class OtherLeadershipProbeEffect : LeadershipProbeEffect
{
}

/// <summary>Told about every attack attempt; flags itself once its charges run out.</summary>
public class AttackAttemptProbeEffect : ProbeEffect, IAttackAttemptStatusEffect
{
    public int Charges { get; set; } = 1;

    public void OnAttackAttemptedEffect(uint attacker, SkillType skillType, AbilityImpactSummary abilityImpact)
    {
        Log.Add($"OnAttackAttempted:{Name}:{attacker}:{skillType}:{abilityImpact?.CriticalHitCount}");
        if (--Charges <= 0)
            Flag();
    }
}

/// <summary>Validates its state before an originating hit lands.</summary>
public class PreDamageProbeEffect : ProbeEffect, IPreDamageStatusEffect
{
    public void OnBeforeDamageTaken(uint defender, uint attacker, int damage, CombatDamageType damageType, CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct) =>
        Log.Add($"OnBeforeDamageTaken:{Name}:{defender}:{attacker}:{damage}:{damageType}:{deliveryType}");
}

/// <summary>Told when its bearer's guard prevented damage.</summary>
public class GuardedHitProbeEffect : ProbeEffect, IGuardedHitStatusEffect
{
    public void OnGuardedHitEffect(uint defender, uint attacker, int preventedDamage) =>
        Log.Add($"OnGuardedHit:{Name}:{defender}:{attacker}:{preventedDamage}");
}

/// <summary>Limited attack delay reduction, deferred while a native attack cycle resolves.</summary>
public class LimitedDelayProbeEffect : ProbeEffect, ILimitedAttackDelayReductionStatusEffect
{
    public int AttackDelayReductionPercent { get; set; } = 25;
    public int RemainingAttacks { get; set; } = 3;
    public SkillType Skill { get; set; } = SkillType.Rifle;

    public bool AppliesToSkill(SkillType skillType) => skillType == Skill;
}

/// <summary>A second limited delay reduction with its own numbers.</summary>
public class OtherLimitedDelayProbeEffect : LimitedDelayProbeEffect
{
}

/// <summary>Limited no-delay attacks, deferred while a native attack cycle resolves.</summary>
public class LimitedNoDelayProbeEffect : ProbeEffect, ILimitedAttackNoDelayStatusEffect
{
    public int RemainingAttacks { get; set; } = 2;
    public SkillType Skill { get; set; } = SkillType.Rifle;

    public bool AppliesToSkill(SkillType skillType) => skillType == Skill;
}
