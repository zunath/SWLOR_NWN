using SWLOR.CombatSim.Data;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.CombatSim.Engine;

/// <summary>Damage-roll and status-rider stages of the production weapon formula that the Vibroblade and Staff lines reach.</summary>
public sealed partial class WeaponCombatState
{
    private static readonly HashSet<StatType> FormulaSupportedStats = new()
    {
        StatType.TargetLowHPDamageThresholdPercent, StatType.TargetLowHPDamagePercentAdjustment,
        StatType.MeleeRepeatedTargetDamageBonusPerHit, StatType.MeleeRepeatedTargetDamageBonusMax,
        StatType.MeleeRepeatedTargetDamageStatusEffectIcon, StatType.MeleeAutoAttackCycleRequiredCount,
        StatType.MeleeAutoAttackCycleDamage, StatType.AutoAttackDamageBonusChance, StatType.AutoAttackDamageBonus,
        StatType.CriticalDamageTargetStatusCategory, StatType.CriticalDamageTargetStatusPercentAdjustment,
        StatType.StatusAppliedNextAttackDamageBonus, StatType.StatusAppliedNextAttackWindowSeconds,
        StatType.DamageToControlTargetPercentAdjustment, StatType.AbilityTargetStatusRequiredCategory,
        StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment, StatType.AbilityTargetStatusDurationSeconds,
        StatType.StatusAppliedTargetAccuracyPercentAdjustment, StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment,
        StatType.StatusAppliedTargetDurationSeconds, StatType.AccuracyPercentAdjustment,
        StatType.AutoAttackHitStaminaRestore, StatType.DefeatedEnemyStaminaRestore
    };
    private int _attackCycle;
    private uint _rundownTarget;
    private int _rundownStacks;
    private (int Amount, double Until) _nextAttackBonus;

    /// <summary>StatusEffect.HasStatusEffectCategory for the categories the weapon simulation tracks (Control only).</summary>
    public static bool HasCategory(Combatant target, StatusEffectCategory category, double now)
    {
        if (category == StatusEffectCategory.None) return false;
        if ((category & ~StatusEffectCategory.Control) != 0)
            throw new InvalidDataException($"Weapon simulation does not track status category {category}.");
        return target.Weapon.Control.IsControlled(now);
    }

    /// <summary>Charged Blows' unspent DMG bonus (NextAttackStatusAppliedDMGBonus), for the next landed attack or hostile ability hit.</summary>
    public int NextAttackBonus(double now) => now < _nextAttackBonus.Until ? _nextAttackBonus.Amount : 0;

    public int TakeNextAttackBonus(double now)
    {
        var bonus = NextAttackBonus(now);
        _nextAttackBonus = default;
        if (bonus != 0) Count("NextAttackBonusSpent");
        return bonus;
    }

    /// <summary>
    /// GetDamageRoll.CalculateCreatureDamage: a landed ordinary attack consumes Charged Blows' bonus and advances
    /// Follow-Through's cycle (ConsumeMeleeAutoAttackCycleDamageBonus); both add DMG before the damage formula.
    /// </summary>
    public int TakeLandedOrdinaryPower(double now)
    {
        var power = TakeNextAttackBonus(now);
        var required = Stat(StatType.MeleeAutoAttackCycleRequiredCount);
        var cycleDamage = Stat(StatType.MeleeAutoAttackCycleDamage);
        if (required <= 0 || cycleDamage <= 0) return power;
        if (++_attackCycle < required) return power;
        _attackCycle = 0;
        Count("AttackCycleProcs");
        return power + cycleDamage;
    }

    /// <summary>Combat.ApplyCriticalDamageModifier with the target-status critical bonus (Crushing Mastery).</summary>
    public int ApplyCriticalDamage(int damage, int critical, Combatant target, double now)
    {
        if (critical <= 0 || damage <= 0) return damage;
        var adjustment = Stat(StatType.CriticalDamageTargetStatusPercentAdjustment);
        if (adjustment == 0 || !HasCategory(target, (StatusEffectCategory)Stat(StatType.CriticalDamageTargetStatusCategory), now)) return damage;
        adjustment = Combat.ClampCriticalDamagePercentAdjustment(adjustment);
        Count("TargetStatusCriticalBonuses");
        return damage + damage * adjustment / 100;
    }

    /// <summary>
    /// Combat.ApplyAutoAttackDamageModifiers for a landed ordinary attack: each source's chance-based flat damage
    /// (Savage Reflexes, CalculateAutoAttackProcDamage), then the guaranteed STM restore (Blood Frenzy).
    /// </summary>
    public int ApplyAutoAttackDamage(int damage, Func<int> d100)
    {
        if (damage <= 0) return damage;
        foreach (var source in AllSources)
        {
            var chance = source.GetValueOrDefault(StatType.AutoAttackDamageBonusChance);
            var bonus = source.GetValueOrDefault(StatType.AutoAttackDamageBonus);
            if (chance > 0 && bonus != 0 && d100() <= chance)
            {
                damage += bonus;
                Count("AutoAttackDamageProcs");
            }
        }
        var restore = Stat(StatType.AutoAttackHitStaminaRestore);
        if (restore > 0) RestoreStamina(restore, "AutoAttackStaminaRestored");
        return damage;
    }

    /// <summary>
    /// Combat.ApplyDamageDealtModifiers stages these builds reach: the low-HP target bonus (Executioner), the
    /// target-status percent bonus (Heavy Hands, Crusher Stance), Rundown's flat bonus on ordinary attacks, then
    /// CapOutgoingDamageBonus.
    /// </summary>
    public int ApplyDamageDealt(int damage, Combatant target, bool ordinary, double now)
    {
        if (damage <= 0) return damage;
        var before = damage;
        var threshold = Stat(StatType.TargetLowHPDamageThresholdPercent);
        var lowHp = Stat(StatType.TargetLowHPDamagePercentAdjustment);
        if (threshold > 0 && lowHp != 0)
        {
            var adjusted = Combat.ApplyTargetHPDamageAdjustment(damage, (int)(target.Hp + target.TemporaryHpTotal), (int)target.Spec.MaxHp, threshold, lowHp);
            if (adjusted != damage) Count("LowHpTargetBonuses");
            damage = adjusted;
        }
        var control = Stat(StatType.DamageToControlTargetPercentAdjustment);
        if (control != 0 && HasCategory(target, StatusEffectCategory.Control, now))
        {
            damage = Combat.ApplyPercentDamageAdjustment(damage, control);
            Count("ControlledTargetBonuses");
        }
        var flat = ordinary ? RepeatedTargetBonus(target) : 0;
        return Combat.CapOutgoingDamageBonus(before, damage, flat);
    }

    // Combat.ApplyMeleeRepeatedTargetDamageModifier: landed ordinary attacks on one target build stacks; a new target restarts them.
    private int RepeatedTargetBonus(Combatant target)
    {
        var perHit = Stat(StatType.MeleeRepeatedTargetDamageBonusPerHit);
        var maximum = Stat(StatType.MeleeRepeatedTargetDamageBonusMax);
        if (perHit <= 0 || maximum <= 0)
        {
            _rundownTarget = 0;
            _rundownStacks = 0;
            return 0;
        }
        if (_rundownTarget != target.SimId)
        {
            _rundownTarget = target.SimId;
            _rundownStacks = 0;
        }
        _rundownStacks = Math.Min(_rundownStacks + 1, Math.Max(1, (int)Math.Ceiling(maximum / (float)perHit)));
        var bonus = Math.Min(maximum, _rundownStacks * perHit);
        Count("RepeatedTargetDamage", bonus);
        return bonus;
    }

    /// <summary>Combat.ApplyAbilityTargetStatusEffects (Break Posture): a successful hostile ability hit on a target with the required category.</summary>
    public void ApplyAbilityTargetStatus(AbilityInfo ability, Combatant target, double now)
    {
        if (!ability.IsHostile) return;
        var required = (StatusEffectCategory)Stat(StatType.AbilityTargetStatusRequiredCategory);
        if (required == StatusEffectCategory.None || !HasCategory(target, required, now)) return;
        if (ReplaceTargetStat(target, StatType.PhysicalDefensePercentAdjustment, Stat(StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment),
                Stat(StatType.AbilityTargetStatusDurationSeconds), nameof(StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment), now))
            Count("AbilityTargetDebuffs");
    }

    // Combat.ApplyStatusAppliedEffects after an ability applies a status of the required category.
    private void ApplyStatusAppliedBonuses(Combatant target, double now)
    {
        var bonus = Stat(StatType.StatusAppliedNextAttackDamageBonus);
        var window = Stat(StatType.StatusAppliedNextAttackWindowSeconds);
        if (bonus != 0 && window > 0)
        {
            _nextAttackBonus = (bonus, now + window);
            Count("NextAttackBonusGranted");
        }
        if (target == null) return;
        var duration = Stat(StatType.StatusAppliedTargetDurationSeconds);
        ReplaceTargetStat(target, StatType.PhysicalDefensePercentAdjustment, Stat(StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment),
            duration, nameof(StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment), now);
        if (ReplaceTargetStat(target, StatType.AccuracyPercentAdjustment, Stat(StatType.StatusAppliedTargetAccuracyPercentAdjustment),
                duration, nameof(StatType.StatusAppliedTargetAccuracyPercentAdjustment), now))
            Count("StatusAppliedTargetDebuffs");
    }

    // TemporaryStatModifier.Replace: one modifier per target and group, restarted by each application.
    private static bool ReplaceTargetStat(Combatant target, StatType stat, int amount, int duration, string group, double now)
    {
        if (amount == 0 || duration <= 0) return false;
        target.Statuses[group] = new ActiveStatus { Stats = new Dictionary<StatType, int> { [stat] = amount }, ExpiresAt = now + duration };
        return true;
    }

    /// <summary>Combat.ApplyDefeatedEnemyEffects' STM restore (Blood Frenzy).</summary>
    public void DefeatedEnemy()
    {
        var restore = Stat(StatType.DefeatedEnemyStaminaRestore);
        if (restore > 0) RestoreStamina(restore, "DefeatedEnemyStaminaRestored");
    }

    private void RestoreStamina(int amount, string diagnostic)
    {
        var restored = Math.Min(_owner.Spec.MaxStm - _owner.Stm, amount);
        if (restored <= 0) return;
        _owner.Stm += restored;
        Count(diagnostic, restored);
    }

    private void ClearFormulaState()
    {
        _attackCycle = 0;
        _rundownTarget = 0;
        _rundownStacks = 0;
        _nextAttackBonus = default;
    }
}
