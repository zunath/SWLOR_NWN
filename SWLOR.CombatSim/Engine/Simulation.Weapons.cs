using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Effects;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.CombatSim.Engine;

public sealed partial class Simulation
{
    // Logical impact times: animation batching, line of sight, inventory and projectile flight
    // require native engine coverage. This simulator never claims to execute those mechanisms.
    private DateTime WeaponNow => DateTime.UnixEpoch.AddSeconds(_now);
    private uint TargetId(Combatant target) => (uint)(_combatants.IndexOf(target) + 1);

    /// <summary>Grants Cadence through the server's landed-hit path, as one weapon ability activation would.</summary>
    internal void GrantCadence(Combatant actor, int charges) =>
        actor.WeaponTraits.Paired.RecordAbilityHit(WeaponNow, WeaponTraitState.Configuration(actor), Guid.NewGuid(), charges);

    private static double WeaponInterval(WeaponLoadoutSpec weapon, int haste) =>
        Combat.CalculateEffectiveAttackDelay(Combat.CalculateAttackDelayMilliseconds(
            weapon.MainDelay, weapon.OffhandDelay, haste, weapon.OffhandDelayReductionPercent)) / 1000.0;

    private bool CanUseTwin(Combatant actor, AbilityUseSpec use)
    {
        actor.WeaponTraits.Paired.Reconcile(WeaponNow, WeaponTraitState.Configuration(actor));
        return actor.WeaponTraits.Paired.Cadence >= use.MinimumCadence;
    }

    // PairedRangedTraits.CaptureWeaponAbility: the ability's per-charge override or the trait's, plus Steady Cadence.
    private static int CadenceDamagePerCharge(Combatant actor, Data.AbilityInfo info)
    {
        var configuration = WeaponTraitState.Configuration(actor);
        return (info.WeaponAbilityCadenceDamagePerCharge ?? configuration.CadenceDamagePerCharge) +
               configuration.CadenceBonusDamagePerCharge;
    }

    private static int[] CaptureTwinWeaponDamage(WeaponLoadoutSpec weapon) =>
        weapon.DistinctAttackItemCount == 2
            ? new[] { EffectiveMainWeaponDamage(weapon), weapon.OffhandDamage }
            : new[] { EffectiveMainWeaponDamage(weapon) };

    private static int EffectiveMainWeaponDamage(WeaponLoadoutSpec weapon) =>
        WeaponDamage.CalculateEffectiveDMG(weapon.MainDamage, weapon.SingleWeaponDamagePercent);

    private void ResolveTwin(Combatant actor, PendingCast cast)
    {
        var spec = cast.Ability.TwinPistol;
        if (cast.Ability.IsChanneled && cast.VolleyIndex >= spec.VolleyCount) return;
        if (cast.Ability.IsChanneled && cast.Target.IsDead)
        {
            CompleteTwin(actor, cast);
            actor.NextActionAt = _now + actor.Spec.ReactionSeconds;
            actor.NextAttackAt = Math.Max(cast.AttackReadyBeforeChannel, _now);
            return;
        }
        var landed = false;
        // Every Twin Pistol attack is a single-target shot at its selected enemy.
        var target = cast.Target;
        if (IsEngaged(target) && target.TeamIndex != actor.TeamIndex)
        {
            for (var hand = 0; hand < cast.WeaponDamages.Count; hand++)
            {
                if (target.IsDead) break;
                var weaponDamage = cast.WeaponDamages[hand];
                var cadenceBonus = cast.WeaponActivation?.TryTakeDamageBonus((uint)(hand + 1), cast.VolleyIndex, spec.VolleyCount) ?? 0;
                actor.Stats.CadenceDamagePowerApplied += WeaponImpactDamagePartition.Partition(cadenceBonus, cast.VolleyIndex, spec.VolleyCount);
                var power = weaponDamage * spec.WeaponPowerPercent / 100 + spec.FlatPower + cadenceBonus;
                var accuracy = cast.WeaponTraits.AccuracyAdjustment;
                actor.Stats.AbilityImpacts++;
                var (hit, damage) = RollWeapon(actor, target, power, accuracy, ordinary: false,
                    volleyIndex: cast.VolleyIndex, volleyCount: spec.VolleyCount);
                landed |= hit;
                if (hit)
                {
                    actor.Stats.AbilityHits++;
                    RecordSuccessfulWeaponAbilityHit(actor, cast);
                }
                actor.Stats.AbilityDamage += damage;
            }
        }
        cast.AnyWeaponHit |= landed;
        cast.VolleyIndex++;
        if (cast.VolleyIndex >= spec.VolleyCount) CompleteTwin(actor, cast);
        if (cast.Ability.IsChanneled && _now + 1e-9 < cast.ChannelEndsAt)
        {
            cast.ImpactAt = cast.VolleyIndex < spec.VolleyCount
                ? cast.ChannelStartedAt + cast.VolleyIndex * spec.VolleyIntervalSeconds
                : cast.ChannelEndsAt;
            actor.Casting = cast;
        }
    }

    private void CompleteTwin(Combatant actor, PendingCast cast)
        => CompleteWeaponTraits(actor, cast);

    private void CompleteWeaponTraits(Combatant actor, PendingCast cast)
    {
        if (cast.WeaponTraitsCompleted) return;
        cast.WeaponTraitsCompleted = true;
        var spec = cast.Ability.TwinPistol;
        if (spec?.OrdinaryBonusAttempts > 0 && spec.OrdinaryBonusSeconds > 0)
            actor.WeaponTraits.Paired.GrantOrdinaryBonus(WeaponNow, spec.OrdinaryBonusAttempts,
                spec.OrdinaryBonusDamagePercent, spec.OrdinaryBonusSeconds);
        if (cast.AnyWeaponHit) RestoreWeaponHitStamina(actor, cast, spec?.RestoreStaminaOnHit ?? 0);
    }

    private void RecordSuccessfulWeaponAbilityHit(Combatant actor, PendingCast cast)
    {
        var activation = cast.WeaponActivation;
        if (activation?.IsCommitted != true) return;
        if (!activation.RecoveryCompleted)
        {
            activation.RecoveryCompleted = true;
            var requested = actor.WeaponTraits.Paired.CompleteAbility(WeaponNow, WeaponTraitState.Configuration(actor),
                cast.WeaponTraits, true, cast.StmCost, 0);
            RestoreWeaponHitStamina(actor, cast, requested);
        }
        // A multi-hand, multi-target or multi-wave activation earns its Cadence once, on its first landed hit.
        if (activation.CadenceHitObserved) return;
        activation.CadenceHitObserved = true;
        var earned = activation.CadenceEarnedOnHitAt(WeaponNow);
        if (earned > 0)
            actor.WeaponTraits.Paired.RecordAbilityHit(WeaponNow, WeaponTraitState.Configuration(actor),
                activation.Snapshot.ActivationId, earned);
    }

    private void RestoreWeaponHitStamina(Combatant actor, PendingCast cast, int requested)
    {
        var amount = Combat.CalculateAbilityHitStaminaRestore(cast.StmCost, cast.HitStaminaRefunded, requested);
        cast.HitStaminaRefunded += amount;
        RestoreStamina(actor, amount, true);
    }

    private void ProcessWeaponAutoAttack(Combatant actor, double step)
    {
        if (actor.Casting != null || _now < actor.NextAttackAt) return;
        var weapon = actor.Spec.WeaponLoadout;
        var target = EnsureAttackTarget(actor);
        if (target == null)
        {
            actor.NextAttackAt = _now + step;
            return;
        }
        actor.NextAttackAt = Math.Max(actor.NextAttackAt, _now - step);
        while (_now + 1e-9 >= actor.NextAttackAt)
        {
            target = EnsureAttackTarget(actor);
            if (target == null) return;
            var config = WeaponTraitState.Configuration(actor);
            var hasteConfig = WeaponTraitState.HasteConfiguration(actor);
            actor.WeaponTraits.Haste.RecordTargetChange(TargetId(target));
            actor.WeaponTraits.Haste.Reconcile(WeaponNow, hasteConfig);
            var haste = weapon.HastePercent + actor.GetStat(StatType.AttackDelayReductionPercent) +
                        WeaponTraitState.DualWieldStat(actor, StatType.DualWieldAttackDelayReductionPercent) +
                        actor.WeaponTraits.Haste.Stacks * hasteConfig.HastePerStack;
            actor.NextAttackAt += WeaponInterval(weapon, haste);
            for (var hand = 0; hand < (weapon.HasOffhand ? 2 : 1); hand++)
            {
                if (target.IsDead) break;
                if (actor.QueuedWeaponAbility is { } queued)
                {
                    // A queued weapon ability replaces its carrier; it is not an ordinary hit
                    // and cannot trigger ordinary-only damage, recovery or ramping procs.
                    var (carrierHit, _) = RollWeapon(actor, target, 0,
                        queued.Cast.WeaponTraits?.AccuracyAdjustment ?? 0, ordinary: false, resolveDamage: false);
                    if (!carrierHit) continue;
                    actor.QueuedWeaponAbility = null;
                    DamageHealingCatalog.TryGet(queued.Ability.Feat, out var damageHealing);
                    var beforeDamage = actor.Stats.DamageDealt;
                    queued.Cast.AnyWeaponHit = ApplyDamageProfile(actor, target, queued.Use.Damage, 0f,
                        damageHealing, alwaysHits: true, cast: queued.Cast);
                    actor.Stats.AbilityDamage += actor.Stats.DamageDealt - beforeDamage;
                    if (queued.Cast.WeaponActivation != null) CompleteWeaponTraits(actor, queued.Cast);
                    if (HitPointCostCatalog.TryGet(queued.Ability.Feat, out var hitPointCost)) PayHitPoints(actor, hitPointCost);
                    continue;
                }
                var roll = ++actor.WeaponTraits.RollId;
                var bonuses = actor.WeaponTraits.Paired.RecordOrdinaryAttempt(WeaponNow, config, TargetId(target), roll);
                actor.Stats.OrdinaryAttempts++;
                var (hit, damage) = RollWeapon(actor, target, hand == 0 ? EffectiveMainWeaponDamage(weapon) : weapon.OffhandDamage,
                    bonuses.AccuracyAdjustment, true, bonuses.DamagePercentAdjustment);
                actor.Stats.OrdinaryDamage += damage;
                if (!hit) continue;
                actor.Stats.OrdinaryHits++;
                RestoreStamina(actor, actor.WeaponTraits.Paired.RecordOrdinaryHit(WeaponNow, config, TargetId(target), roll), true);
                actor.WeaponTraits.Haste.RecordHit(WeaponNow, hasteConfig, TargetId(target), roll);
            }
        }
    }

    private (bool Hit, double Damage) RollWeapon(Combatant actor, Combatant target, int power, int accuracyBonus,
        bool ordinary, int damageBonus = 0, int volleyIndex = 0, int volleyCount = 1,
        bool resolveDamage = true)
    {
        var weapon = actor.Spec.WeaponLoadout;
        var hitChance = Combat.CalculateHitRate(weapon.Accuracy + actor.GetStat(StatType.Accuracy) + accuracyBonus, target.Spec.Evasion, 0);
        if (_random.Next(1, 101) > hitChance)
        {
            target.WeaponTraits.Paired.RecordAccuracyEvasionMiss(WeaponNow, WeaponTraitState.Configuration(target));
            return (false, 0);
        }
        if (ordinary && _random.Next(1, 101) <= target.Spec.WeaponAvoidancePercent) return (false, 0);
        if (!resolveDamage) return (true, 0);
        var criticalChance = ordinary
            ? Combat.CalculateCriticalRate(weapon.Perception, target.Spec.Vitality, weapon.SkillRank, weapon.OrdinaryCriticalRateAdjustment)
            : Combat.CalculateAbilityCriticalChance(weapon.AbilityCriticalRateAdjustment);
        var critical = _random.Next(1, 101) <= criticalChance ? Combat.StandardCriticalRating : 0;
        // GetAbilityDamageBonus is engine-bound, but its only player calculation is floor(0.15 * rank).
        if (!ordinary) power += (int)(0.15f * weapon.SkillRank);
        var (minimum, maximum) = Combat.CalculateDamageRange(weapon.Attack, power, weapon.DamageAbility,
            target.Spec.Defense, target.Spec.Vitality, critical, ordinary ? weapon.WeaponRank : 0);
        // Both native and activated damage truncate a uniform float in [minimum, maximum),
        // rather than drawing an inclusive integer (which biases low-power weapons upward).
        var amount = SampleWeaponDamage(_random, minimum, maximum);
        if (ordinary)
        {
            var additive = damageBonus;
            amount += (int)Math.Ceiling(amount * Math.Clamp(additive, -Combat.MaximumNormalDamageReductionPercent, Combat.MaximumDamageBonusPercent) / 100f);
            amount = Combat.ApplyOrdinaryAttackDamageMultiplier(amount, actor.GetStat(StatType.OrdinaryAttackDamagePercentAdjustment));
        }
        amount = WeaponImpactDamagePartition.Partition(amount, volleyIndex, volleyCount);
        var dealt = amount > 0 ? ApplyDamage(actor, target, amount, directHostile: true) : 0;
        return (true, dealt);
    }

    internal static int SampleWeaponDamage(System.Random random, int minimum, int maximum) =>
        (int)(minimum + random.NextDouble() * (maximum - minimum));

    private (bool Hit, double Dealt) RollNativeWeaponProfile(Combatant actor, Combatant target,
        WeaponPowerSpec profile, uint item, Func<int> takeCadenceBonus, int accuracyBonus,
        double multiplier, bool alwaysHits)
    {
        if (target.IsDead) return (false, 0);
        var weapon = actor.Spec.WeaponLoadout;
        if (!alwaysHits && _random.Next(1, 101) > Combat.CalculateHitRate(
                weapon.Accuracy + actor.GetStat(StatType.Accuracy) + accuracyBonus, target.Spec.Evasion, 0))
        {
            if (actor.TeamIndex != target.TeamIndex)
                target.WeaponTraits.Paired.RecordAccuracyEvasionMiss(WeaponNow, WeaponTraitState.Configuration(target));
            return (false, 0);
        }

        var rating = item == 1 ? EffectiveMainWeaponDamage(weapon) : weapon.OffhandDamage;
        var power = rating * profile.WeaponDamagePercent / 100 + profile.FlatDamage +
                    (takeCadenceBonus?.Invoke() ?? 0) + (int)(0.15f * weapon.SkillRank);
        var critical = _random.Next(1, 101) <= Combat.CalculateAbilityCriticalChance(weapon.AbilityCriticalRateAdjustment)
            ? Combat.StandardCriticalRating : 0;
        var (minimum, maximum) = Combat.CalculateDamageRange(weapon.Attack, power, weapon.DamageAbility,
            target.Spec.Defense, target.Spec.Vitality, critical, 0);
        return (true, ApplyDamage(actor, target, SampleWeaponDamage(_random, minimum, maximum) * multiplier, directHostile: true));
    }
}
