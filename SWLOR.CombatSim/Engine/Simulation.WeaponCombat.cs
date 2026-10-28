using SWLOR.CombatSim.Data;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;

namespace SWLOR.CombatSim.Engine;
public sealed partial class Simulation
{
    private int DiscountedStamina(Combatant caster, int cost, bool consume, AbilityInfo ability)
    {
        if (caster.Weapon == null || cost <= 0) return cost;
        if (ability.IsHostile) cost -= caster.Weapon.AvoidedAttackDiscount(cost, consume, _now);
        var discount = consume ? caster.Weapon.Resources.TakeDiscount(cost, WeaponCombatState.At(_now)) : Math.Min(cost, caster.Weapon.Resources.GetDiscount(WeaponCombatState.At(_now)));
        if (consume && discount > 0) caster.Weapon.Count("PaybackStaminaSaved", discount);
        return cost - discount;
    }
    private float FlatProfileSetupBonus(Combatant caster, Combatant target)
    {
        if (caster.Weapon == null || target?.Weapon == null) return 0;
        var a = caster.Spec.WeaponCombat; var d = target.Spec.WeaponCombat;
        var accuracy = caster.Weapon.SetupAccuracy(target, _now);
        return (Combat.CalculateHitRate(a.Accuracy + accuracy, WeaponCombatState.Adjust(d.Evasion, target.Weapon.EvasionPercent(_now)), 0) - Combat.CalculateHitRate(a.Accuracy, d.Evasion, 0)) / 100f;
    }
    private bool ApplyWeaponAbility(Combatant caster, Combatant target, PendingCast cast, bool alwaysHits = false)
    {
        var isArea = IsWeaponArea(cast.Ability);
        if (target.IsDead && !isArea) return false;
        var technique = cast.Ability.Technique;
        var count = technique?.ImpactCount ?? cast.Use.WeaponHits;
        var weaponPercent = technique?.WeaponDamagePercent ?? cast.Use.WeaponDamagePercent;
        var landed = false;
        var secondary = cast.Technique.SweepSecondary;
        BeginWeaponAreaImpact(caster, cast);
        List<Combatant> areaTargets = null;
        if (isArea)
        {
            if (cast.Ability.WeaponAreaRadius <= 0)
                throw new InvalidDataException($"Weapon area simulation requires a source-defined self-centered sphere: {cast.Ability.Name}.");
            areaTargets = EnemiesAt(caster, _now).Where(enemy => WeaponDistanceSquared(caster, enemy) <= Math.Pow(cast.Ability.WeaponAreaRadius, 2))
                .OrderBy(enemy => WeaponDistanceSquared(caster, enemy)).ThenBy(enemy => enemy.SimId)
                .Take(cast.Ability.MaximumAreaTargets > 0 ? cast.Ability.MaximumAreaTargets : int.MaxValue).ToList();
        }
        var impacts = areaTargets == null
            ? Enumerable.Range(0, count).Select(index => (Victim: index == 1 && secondary != null ? secondary : target, Index: index))
            : areaTargets.SelectMany(victim => Enumerable.Range(0, count).Select(index => (Victim: victim, Index: index)));
        var hitTargets = new HashSet<uint>();
        // Charged Blows' bonus joins every impact's DMG until the first damaging hit consumes it (TrackedAbilityImpact).
        var nextAttackBonus = caster.Weapon.NextAttackBonus(_now);
        foreach (var (victim, index) in impacts)
        {
            if (victim.IsDead) continue;
            // The profile's baseDamageAdjustment reads the target before this impact's own statuses.
            var controlled = cast.Ability.ExtraDamageIfTargetControlled != 0 && WeaponCombatState.HasCategory(victim, StatusEffectCategory.Control, _now);
            var power = cast.Use.WeaponPower.Value + (int)(0.15f * caster.Spec.WeaponCombat.WeaponSkillRank) + TechniqueSequence.ScaleWeaponDamage(caster.Spec.WeaponCombat.AbilityWeaponDamage, weaponPercent) +
                TechniqueSequence.SplitPower(cast.Technique.ConditionalPower, count, index) + cast.Technique.FlatPower +
                (controlled ? cast.Ability.ExtraDamageIfTargetControlled : 0) + nextAttackBonus;
            var accuracy = technique == null ? caster.Weapon.SetupAccuracy(victim, _now) : victim == target ? cast.Technique.Accuracy : 0;
            var hit = RollWeapon(caster, victim, power, accuracy, cast.Technique.HitPercent, false,
                () =>
                {
                    if (controlled) caster.Weapon.Count("ControlledTargetPower");
                    if (nextAttackBonus != 0)
                    {
                        caster.Weapon.TakeNextAttackBonus(_now);
                        nextAttackBonus = 0;
                    }
                    if (!hitTargets.Contains(victim.SimId)) ApplyWeaponStatusRiders(caster, victim, cast.Ability);
                    caster.Weapon.CostlyAbilityHit(cast.Technique);
                }, alwaysHits);
            landed |= hit;
            if (hit) hitTargets.Add(victim.SimId);
            if (hit && cast.Technique.PaidStamina >= caster.Weapon.Stat(StatType.CostlyAbilityDamageMinimumStaminaCost) && caster.Weapon.Stat(StatType.CostlyAbilityDamageBonus) > 0)
                caster.Weapon.Count("HighGuardImpacts");
        }
        cast.Technique.ImpactedTargets = hitTargets.Count;
        if (cast.Ability.RestoresSecondaryTargetStamina && hitTargets.Count > 1)
            throw new InvalidDataException($"{cast.Ability.Name}'s secondary-target stamina restore is not modeled; use one target.");
        return landed;
    }

    // Savage Cleave declares a self-centered targeting sphere and a telegraphed area impact without IsAreaAbility.
    private static bool IsWeaponArea(AbilityInfo ability) => ability.IsAreaAbility || ability.WeaponAreaRadius > 0;

    /// <summary>
    /// A paid formula weapon ability rides the next attack (ResolveAttackRoll): the native roll uses ability accuracy
    /// without ordinary penalties, a miss keeps it queued and counts as an avoided attack, and a hit lands the whole
    /// impact without a second roll while suppressing the carrier's own damage and auto-attack riders.
    /// </summary>
    private void ApplyQueuedWeaponAbility(Combatant attacker, Combatant target, QueuedWeaponAbility queued)
    {
        var state = attacker.Weapon;
        state.Target(target, _now);
        state.Refresh(_now); target.Weapon.Refresh(_now);
        state.Count("QueuedCarrierAttempts");
        if (_random.Next(1, 101) > WeaponHitChance(attacker, target, state.SetupAccuracy(target, _now), 0, false))
        {
            target.Weapon.AvoidedAttack(_now);
            return;
        }
        attacker.QueuedWeaponAbility = null;
        var cast = queued.Cast;
        var landed = ApplyWeaponAbility(attacker, target, cast, alwaysHits: true);
        state.Complete(cast.Ability, cast.Use, cast.Technique, target, landed, _now);
        state.CompleteSweep(cast.Technique, _now);
    }

    private void CaptureTechniqueSweep(Combatant caster, Combatant target, PendingCast cast)
    {
        var technique = cast.Ability.Technique;
        if (!_rules.EnableTechniqueSweeps || cast.Technique.Stage != MeleeTechniqueStage.Opening || technique?.OpeningSweepRadiusMeters is not > 0) return;
        cast.Technique.SweepSecondary = EnemiesAt(caster, _now)
            .Where(enemy => enemy != target && WeaponDistanceSquared(caster, enemy) <= Math.Pow(technique.OpeningSweepRadiusMeters, 2))
            .OrderBy(enemy => WeaponDistanceSquared(caster, enemy)).ThenBy(enemy => enemy.SimId).FirstOrDefault();
        cast.Technique.IsSweep = cast.Technique.SweepSecondary != null;
    }

    private void BeginWeaponAreaImpact(Combatant caster, PendingCast cast)
    {
        cast.Technique.IsArea = cast.Technique.IsSweep || IsWeaponArea(cast.Ability);
        if (cast.Technique.IsArea) caster.Weapon.AreaUsed(_now, cast.Technique.IsSweep);
    }

    private static double WeaponDistanceSquared(Combatant caster, Combatant target) =>
        Math.Pow(target.Spec.WeaponCombat.PositionX - caster.Spec.WeaponCombat.PositionX, 2) +
        Math.Pow(target.Spec.WeaponCombat.PositionY - caster.Spec.WeaponCombat.PositionY, 2);

    private void ApplyWeaponStatusRiders(Combatant caster, Combatant target, AbilityInfo ability)
    {
        // Tracked native impacts resolve statuses and their riders before flushing lethal damage, in
        // ApplyHostileCombatImpact's order: primary statuses, ApplySuccessfulAbilityImpactRiders, then afterSuccessfulHit.
        caster.Weapon.ApplyStatusRiders(ApplyWeaponStatuses(caster, target, ability), _now, target);
        caster.Weapon.ApplyAbilityTargetStatus(ability, target, _now);
        ApplyConditionalTargetStatus(caster, target, ability);
    }

    /// <summary>WeaponAbilityProfile.ApplyTargetConditionalStatus: Ground Quake's Daze-to-Knockdown conversion and Worldbreaker's Daze.</summary>
    private void ApplyConditionalTargetStatus(Combatant caster, Combatant target, AbilityInfo ability)
    {
        if (ability.ConditionalTargetStatus is not { } conditional) return;
        if (conditional.RequiredStatus != null && !HasWeaponStatus(target, conditional.RequiredStatus)) return;
        if (conditional.RequiredCategory != StatusEffectCategory.None && !WeaponCombatState.HasCategory(target, conditional.RequiredCategory, _now)) return;
        var status = (IStatusEffect)Activator.CreateInstance(conditional.Status);
        var outgoing = StatusEffect.GetOutgoingDurationPercentAdjustment(status.Categories, caster.Weapon.Stat);
        var pvp = IsPlayerVersusPlayer(caster, target);
        if (!conditional.ConvertsRequiredStatus)
        {
            if (TryApplyControl(caster, target, status, conditional.DurationSeconds, outgoing, pvp) == null)
                throw new InvalidDataException($"Weapon simulation does not implement conditional {conditional.Status.Name}.");
            return;
        }
        if (status is not KnockdownStatusEffect || conditional.RequiredStatus != typeof(DazedStatusEffect))
            throw new InvalidDataException($"Weapon simulation does not implement converting {conditional.RequiredStatus?.Name} into {conditional.Status.Name}.");
        var control = target.Weapon.Control;
        var duration = target.Weapon.Stat(StatType.KnockdownImmunity) > 0 ? 0 :
            WeaponControlState.Duration(status, conditional.DurationSeconds, outgoing, MobilityResistance(target), pvp);
        if (duration > 0 && control.TryConvertDazeToKnockdown(_now, duration, status.Frequency))
        {
            caster.Weapon.Count("KnockdownsApplied");
            caster.Weapon.Count("DazesConverted");
            // The daze held this swing until its own end; the shorter knockdown releases it sooner.
            target.NextAttackAt = Math.Min(target.NextAttackAt, control.KnockdownUntil);
        }
        else caster.Weapon.Count("KnockdownsResisted");
    }

    private bool HasWeaponStatus(Combatant target, Type status)
    {
        var control = target.Weapon.Control;
        if (status == typeof(DazedStatusEffect)) return control.IsDazed(_now);
        if (status == typeof(KnockdownStatusEffect)) return control.IsKnockedDown(_now);
        if (status == typeof(HamstringStatusEffect)) return _now < control.HamstrungUntil;
        throw new InvalidDataException($"Weapon simulation does not track {status.Name} as a requirement.");
    }

    private static int MobilityResistance(Combatant target) => target.Spec.WeaponCombat.MobilityResistance + target.Weapon.Stat(StatType.MobilityResistance);

    /// <summary>Applies a Dazed or Knockdown status (native EffectDazed or EffectKnockdown); null for any other status type.</summary>
    private bool? TryApplyControl(Combatant caster, Combatant target, IStatusEffect status, int seconds, int outgoing, bool pvp)
    {
        var control = target.Weapon.Control;
        if (status is DazedStatusEffect)
        {
            var resistance = target.Spec.WeaponCombat.MindResistance + target.Weapon.Stat(StatType.MindResistance);
            var duration = target.Weapon.Stat(StatType.DazeImmunity) > 0 ? 0 : WeaponControlState.Duration(status, seconds, outgoing, resistance, pvp);
            if (control.TryDaze(_now, duration))
            {
                caster.Weapon.Count("DazesApplied");
                target.NextAttackAt = Math.Max(target.NextAttackAt, control.DazedUntil);
                return true;
            }
            caster.Weapon.Count("DazesResisted");
            return false;
        }
        if (status is KnockdownStatusEffect)
        {
            var duration = target.Weapon.Stat(StatType.KnockdownImmunity) > 0 ? 0 : WeaponControlState.Duration(status, seconds, outgoing, MobilityResistance(target), pvp);
            if (control.TryKnockdown(_now, duration))
            {
                caster.Weapon.Count("KnockdownsApplied");
                target.NextAttackAt = Math.Max(target.NextAttackAt, control.KnockdownUntil);
                return true;
            }
            caster.Weapon.Count("KnockdownsResisted");
            return false;
        }
        return null;
    }

    private StatusEffectCategory ApplyWeaponStatuses(Combatant caster, Combatant target, AbilityInfo ability)
    {
        var appliedCategories = StatusEffectCategory.None;
        foreach (var type in ability.ImpactStatusTypes)
        {
            var status = ability.WeaponStatusFactory?.Invoke() is { } configured && configured.GetType() == type
                ? configured : (IStatusEffect)Activator.CreateInstance(type);
            var outgoing = StatusEffect.GetOutgoingDurationPercentAdjustment(status.Categories, caster.Weapon.Stat);
            var pvp = IsPlayerVersusPlayer(caster, target);
            if (status is HamstringStatusEffect)
            {
                // Movement is not simulated, but Hamstring is a control effect and its application grants riders.
                var hamstring = WeaponControlState.Duration(status, ability.ImpactStatusDuration, outgoing, MobilityResistance(target), pvp);
                if (hamstring > 0)
                {
                    target.Weapon.Control.Hamstring(_now, hamstring);
                    appliedCategories |= status.Categories;
                }
                continue;
            }
            if (status is ExposedStatusEffect)
            {
                // A stat-only debuff (Rending Strike): its Defense penalty lasts its resisted duration on the target.
                var resistance = target.Spec.WeaponCombat.TraumaResistance + target.Weapon.Stat(StatType.TraumaResistance);
                var exposed = WeaponControlState.Duration(status, ability.ImpactStatusDuration, outgoing, resistance, pvp);
                if (exposed > 0)
                {
                    target.Statuses[type.Name] = new ActiveStatus
                    {
                        Stats = ((StatusEffectBase)status).StatGroup.Stats.Where(x => x.Value != 0).ToDictionary(x => x.Key, x => x.Value),
                        ExpiresAt = _now + exposed
                    };
                    appliedCategories |= status.Categories;
                    caster.Weapon.Count("ExposedApplied");
                }
                continue;
            }
            var applied = TryApplyControl(caster, target, status, ability.ImpactStatusDuration, outgoing, pvp)
                ?? throw new InvalidDataException($"Weapon simulation does not implement {type.Name}.");
            if (applied) appliedCategories |= status.Categories;
        }
        if (ability.InterruptsTarget && target.Casting != null)
        {
            target.Casting = null;
            target.Stats.InterruptedCasts++;
            target.NextActionAt = _now + target.Spec.ReactionSeconds;
            target.NextAttackAt = Math.Min(target.NextAttackAt, _now);
            caster.Weapon.Count("CastsInterrupted");
        }
        return appliedCategories;
    }

    private void ApplyWeaponOrdinary(Combatant attacker, Combatant target)
    {
        var state = attacker.Weapon;
        state.Target(target, _now);
        var matched = state.Sequence.Matches(target.SimId, WeaponCombatState.At(_now));
        var accuracy = matched ? state.OrdinaryAccuracy : 0;
        var weave = state.Sequence.TakeWeave(target.SimId, WeaponCombatState.At(_now));
        if (weave > 0) state.Count("WeaveAttempts");
        var hit = RollWeapon(attacker, target, attacker.Spec.WeaponCombat.WeaponDamage + weave, accuracy, 0, true);
        if (weave > 0 && hit) state.Count("WeavesLanded");
    }
    /// <summary>Accuracy against evasion; Rattled's ordinary-weapon penalty applies only to ordinary attacks.</summary>
    private int WeaponHitChance(Combatant attacker, Combatant target, int accuracyBonus, int hitPercent, bool ordinary)
    {
        var a = attacker.Spec.WeaponCombat; var d = target.Spec.WeaponCombat;
        var penalty = ordinary ? attacker.Weapon.Rattled.Values.Select(x => x.Penalty).DefaultIfEmpty().Max() : 0;
        var evasion = WeaponCombatState.Adjust(d.Evasion, target.Weapon.EvasionPercent(_now));
        // Stat.ApplyPostAccuracyStatusModifiers: a percent of accuracy, rounded away from zero.
        var accuracyPercent = attacker.Weapon.AccuracyPercent(_now);
        var accuracy = accuracyPercent == 0 ? a.Accuracy : WeaponCombatState.Adjust(a.Accuracy, accuracyPercent);
        return Combat.CalculateHitRate(accuracy + accuracyBonus - penalty, evasion, hitPercent);
    }

    private bool RollWeapon(Combatant attacker, Combatant target, int power, int accuracyBonus, int hitPercent, bool ordinary,
        Action beforeDamage = null, bool alwaysHits = false)
    {
        var a = attacker.Spec.WeaponCombat; var d = target.Spec.WeaponCombat;
        attacker.Weapon.Refresh(_now); target.Weapon.Refresh(_now);
        var chance = alwaysHits ? 100 : WeaponHitChance(attacker, target, accuracyBonus, hitPercent, ordinary);
        attacker.Weapon.Count(ordinary ? "OrdinaryAttempts" : "AbilityImpactsAttempted");
        if (!alwaysHits && _random.Next(1, 101) > chance)
        {
            // Only native ordinary attack misses/deflections arm these production perks.
            if (ordinary) target.Weapon.AvoidedAttack(_now);
            return false;
        }
        if (ordinary) power += attacker.Weapon.TakeLandedOrdinaryPower(_now);
        var criticalChance = ordinary ? Combat.CalculateCriticalRate(a.Perception, d.Vitality, a.WeaponSkillRank, 0) : Combat.CalculateAbilityCriticalChance(0);
        var critical = a.EnableCriticals && _random.Next(1, 101) <= criticalChance ? Combat.StandardCriticalRating : 0;
        var attack = WeaponCombatState.Adjust(a.Attack, attacker.Weapon.Stat(StatType.AttackPercentAdjustment));
        var defense = WeaponCombatState.Adjust(d.Defense, target.Weapon.DefensePercent(_now));
        var (min, max) = Combat.CalculateDamageRange(attack, power, a.Might, defense, d.Vitality, critical, ordinary ? Math.Clamp(a.WeaponRequiredSkillRank/10+1,1,6) : 0);
        var amount = (int)(float)(_random.NextDouble() * (max - min) + min);
        // Production order after the roll: critical modifier, auto-attack riders, ApplyDamageDealtModifiers, then
        // MeleeTechnique.ApplyOrdinaryDamageModifiers (Low Kick) or Combat Readiness.
        amount = attacker.Weapon.ApplyCriticalDamage(amount, critical, target, _now);
        if (ordinary) amount = attacker.Weapon.ApplyAutoAttackDamage(amount, () => _random.Next(1, 101));
        amount = attacker.Weapon.ApplyDamageDealt(amount, target, ordinary, _now);
        if (ordinary) amount = Combat.ApplyPercentDamageAdjustment(amount, -attacker.Weapon.LowKick.Values.Select(x => x.Penalty).DefaultIfEmpty().Max());
        if (!ordinary && attacker.Spec.CombatReadinessPercent > 0)
            amount += (int)Math.Ceiling(amount * (attacker.Spec.CombatReadinessPercent / 100f));
        beforeDamage?.Invoke();
        ApplyDamage(attacker, target, amount, directHostile: true);
        attacker.Weapon.Count(ordinary ? "OrdinaryLanded" : "AbilityImpactsLanded");
        return true;
    }
}
