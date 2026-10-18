using SWLOR.CombatSim.Scenario;
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

    private static double WeaponInterval(WeaponLoadoutSpec weapon, int haste) =>
        Combat.CalculateEffectiveAttackDelay(Combat.CalculateAttackDelayMilliseconds(
            weapon.MainDelay, weapon.OffhandDelay, haste, weapon.OffhandDelayReductionPercent)) / 1000.0;

    private bool CanUseTwin(Combatant actor, AbilityUseSpec use, TwinPistolAbilitySpec spec)
    {
        actor.WeaponTraits.Paired.Reconcile(WeaponNow, WeaponTraitState.Configuration(actor));
        if (actor.WeaponTraits.Paired.Momentum < use.MinimumFlow) return false;
        if (spec.Movement == TwinPistolMovementType.None) return true;
        if (spec.Movement == TwinPistolMovementType.Approach && use.MovementMeters is >= 0 and < .01f) return true;
        if (!use.MovementMeters.HasValue || use.MovementMeters < spec.MinimumMovementMeters ||
            _now < actor.WeaponTraits.MovementReadyAt) return false;
        return true;
    }

    private Combatant[] CaptureTwinTargets(Combatant actor, AbilityUseSpec use, TwinPistolAbilitySpec spec, Combatant target) =>
        spec.Movement == TwinPistolMovementType.FreeDestination
            ? Enemies(actor).OrderBy(enemy => ReferenceEquals(enemy, target) ? 0 : 1)
                .Take(Math.Min(spec.MaximumTargets, use.LineTargets)).ToArray()
            : new[] { target };

    private static int[] CaptureTwinWeaponDamage(WeaponLoadoutSpec weapon) =>
        weapon.DistinctAttackItemCount == 2
            ? new[] { weapon.MainDamage, weapon.OffhandDamage }
            : new[] { weapon.MainDamage };

    private void ResolveTwin(Combatant actor, PendingCast cast)
    {
        var spec = cast.Ability.TwinPistol;
        var config = WeaponTraitState.Configuration(actor);
        var distance = cast.Use.MovementMeters ?? 0;
        if (!cast.MovementStarted && spec.Movement != TwinPistolMovementType.None && distance >= .01f)
        {
            cast.MovementStarted = true;
            actor.WeaponTraits.MovementReadyAt = _now + 2;
            if (cast.Use.MovementSeconds > 0)
            {
                cast.Activated = true;
                cast.ImpactAt = _now + cast.Use.MovementSeconds / (1 + actor.GetStat(StatType.MovementSpeedPercentAdjustment) / 100f);
                actor.Casting = cast;
                actor.NextAttackAt = Math.Max(actor.NextAttackAt, cast.ImpactAt + ResumeAttackDelaySeconds);
                actor.NextActionAt = Math.Max(actor.NextActionAt, cast.ImpactAt + actor.Spec.ReactionSeconds);
                return;
            }
        }

        var landed = false;
        foreach (var target in cast.WeaponTargets.Where(target => IsEngaged(target) && target.TeamIndex != actor.TeamIndex))
        {
            for (var hand = 0; hand < cast.WeaponDamages.Count; hand++)
            {
                if (target.IsDead) break;
                var weaponDamage = cast.WeaponDamages[hand];
                var power = weaponDamage * spec.WeaponPowerPercent / 100 + spec.FlatPower + cast.WeaponTraits.MomentumSpent * spec.MomentumPowerPerCharge;
                var accuracy = cast.WeaponTraits.AccuracyAdjustment + (hand > 0 ? config.CompanionAccuracy : 0);
                actor.Stats.AbilityImpacts++;
                var (hit, damage) = RollWeapon(actor, target, power, accuracy, ordinary: false);
                landed |= hit;
                if (hit) actor.Stats.AbilityHits++;
                actor.Stats.AbilityDamage += damage;
            }
        }
        RestoreStamina(actor, actor.WeaponTraits.Paired.CompleteAbility(WeaponNow, config, cast.WeaponTraits,
            landed, distance, spec.GeneratesMomentum, cast.StmCost, spec.RestoreStaminaOnHit,
            spec.OrdinaryBonusAttempts, spec.OrdinaryBonusDamagePercent, spec.OrdinaryBonusSeconds), true);
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
                        actor.WeaponTraits.Haste.Stacks * hasteConfig.HastePerStack;
            actor.NextAttackAt += WeaponInterval(weapon, haste);
            for (var hand = 0; hand < (weapon.HasOffhand ? 2 : 1); hand++)
            {
                if (target.IsDead) break;
                var roll = ++actor.WeaponTraits.RollId;
                var bonuses = actor.WeaponTraits.Paired.RecordOrdinaryAttempt(WeaponNow, config, TargetId(target), roll, hand > 0);
                actor.Stats.OrdinaryAttempts++;
                var (hit, damage) = RollWeapon(actor, target, hand == 0 ? weapon.MainDamage : weapon.OffhandDamage,
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
        bool ordinary, int damageBonus = 0)
    {
        var weapon = actor.Spec.WeaponLoadout;
        var hitChance = Combat.CalculateHitRate(weapon.Accuracy + accuracyBonus, target.Spec.Evasion, 0);
        if (_random.Next(1, 101) > hitChance)
        {
            target.WeaponTraits.Paired.RecordAccuracyEvasionMiss(WeaponNow, WeaponTraitState.Configuration(target));
            return (false, 0);
        }
        if (ordinary && _random.Next(1, 101) <= target.Spec.WeaponAvoidancePercent) return (false, 0);
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
            var additive = damageBonus + (!weapon.HasOffhand ? weapon.SingleWeaponDamagePercent : 0);
            amount += (int)Math.Ceiling(amount * Math.Clamp(additive, -Combat.MaximumNormalDamageReductionPercent, Combat.MaximumDamageBonusPercent) / 100f);
            amount = Combat.ApplyOrdinaryAttackDamageMultiplier(amount, actor.GetStat(StatType.OrdinaryAttackDamagePercentAdjustment));
        }
        return (true, ApplyDamage(actor, target, amount));
    }

    internal static int SampleWeaponDamage(System.Random random, int minimum, int maximum) =>
        (int)(minimum + random.NextDouble() * (maximum - minimum));
}
