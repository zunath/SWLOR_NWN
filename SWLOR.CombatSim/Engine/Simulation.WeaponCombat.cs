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
    private bool ApplyWeaponAbility(Combatant caster, Combatant target, PendingCast cast)
    {
        if (target.IsDead && !cast.Ability.IsAreaAbility) return false;
        var technique = cast.Ability.Technique;
        var count = technique?.ImpactCount ?? cast.Use.WeaponHits;
        var weaponPercent = technique?.WeaponDamagePercent ?? cast.Use.WeaponDamagePercent;
        var landed = false;
        var secondary = cast.Technique.SweepSecondary;
        BeginWeaponAreaImpact(caster, cast);
        List<Combatant> areaTargets = null;
        if (cast.Ability.IsAreaAbility)
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
        foreach (var (victim, index) in impacts)
        {
            if (victim.IsDead) continue;
            var power = cast.Use.WeaponPower.Value + (int)(0.15f * caster.Spec.WeaponCombat.WeaponSkillRank) + TechniqueSequence.ScaleWeaponDamage(caster.Spec.WeaponCombat.WeaponDamage, weaponPercent) +
                TechniqueSequence.SplitPower(cast.Technique.ConditionalPower, count, index) + cast.Technique.FlatPower;
            var accuracy = technique == null ? caster.Weapon.SetupAccuracy(victim, _now) : victim == target ? cast.Technique.Accuracy : 0;
            var hit = RollWeapon(caster, victim, power, accuracy, cast.Technique.HitPercent, false,
                () =>
                {
                    if (!hitTargets.Contains(victim.SimId)) ApplyWeaponStatusRiders(caster, victim, cast.Ability);
                    caster.Weapon.CostlyAbilityHit(cast.Technique);
                });
            landed |= hit;
            if (hit) hitTargets.Add(victim.SimId);
            if (hit && cast.Technique.PaidStamina >= caster.Weapon.Stat(StatType.CostlyAbilityDamageMinimumStaminaCost) && caster.Weapon.Stat(StatType.CostlyAbilityDamageBonus) > 0)
                caster.Weapon.Count("HighGuardImpacts");
        }
        cast.Technique.ImpactedTargets = hitTargets.Count;
        return landed;
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
        cast.Technique.IsArea = cast.Technique.IsSweep || cast.Ability.IsAreaAbility;
        if (cast.Technique.IsArea) caster.Weapon.AreaUsed(_now, cast.Technique.IsSweep);
    }

    private static double WeaponDistanceSquared(Combatant caster, Combatant target) =>
        Math.Pow(target.Spec.WeaponCombat.PositionX - caster.Spec.WeaponCombat.PositionX, 2) +
        Math.Pow(target.Spec.WeaponCombat.PositionY - caster.Spec.WeaponCombat.PositionY, 2);

    private void ApplyWeaponStatusRiders(Combatant caster, Combatant target, AbilityInfo ability)
    {
        // Tracked native impacts resolve statuses and their self riders before flushing lethal damage.
        caster.Weapon.ApplyStatusRiders(ApplyWeaponStatuses(caster, target, ability), _now);
    }

    private StatusEffectCategory ApplyWeaponStatuses(Combatant caster, Combatant target, AbilityInfo ability)
    {
        var appliedCategories = StatusEffectCategory.None;
        foreach (var type in ability.ImpactStatusTypes)
        {
            var status = (IStatusEffect)Activator.CreateInstance(type);
            var outgoing = StatusEffect.GetOutgoingDurationPercentAdjustment(status.Categories, caster.Weapon.Stat);
            if (status is HamstringStatusEffect)
            {
                // Its successful application can grant riders even though movement is not simulated.
                var mobility = target.Spec.WeaponCombat.MobilityResistance + target.Weapon.Stat(StatType.MobilityResistance);
                if (WeaponControlState.Duration(status, ability.ImpactStatusDuration, outgoing, mobility, IsPlayerVersusPlayer(caster, target)) > 0)
                    appliedCategories |= status.Categories;
                continue;
            }
            if (status is not DazedStatusEffect)
                throw new InvalidDataException($"Weapon simulation does not implement {type.Name}.");
            var resistance = target.Spec.WeaponCombat.MindResistance + target.Weapon.Stat(StatType.MindResistance);
            var duration = target.Weapon.Stat(StatType.DazeImmunity) > 0 ? 0 :
                WeaponControlState.Duration(status, ability.ImpactStatusDuration, outgoing, resistance, IsPlayerVersusPlayer(caster, target));
            if (target.Weapon.Control.TryDaze(_now, duration))
            {
                appliedCategories |= status.Categories;
                caster.Weapon.Count("DazesApplied");
                target.NextAttackAt = Math.Max(target.NextAttackAt, target.Weapon.Control.DazedUntil);
            }
            else caster.Weapon.Count("DazesResisted");
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
    private bool RollWeapon(Combatant attacker, Combatant target, int power, int accuracyBonus, int hitPercent, bool ordinary,
        Action beforeDamage = null)
    {
        var a = attacker.Spec.WeaponCombat; var d = target.Spec.WeaponCombat;
        attacker.Weapon.Refresh(_now); target.Weapon.Refresh(_now);
        var penalty = ordinary ? attacker.Weapon.Rattled.Values.Select(x => x.Penalty).DefaultIfEmpty().Max() : 0;
        var evasion = WeaponCombatState.Adjust(d.Evasion, target.Weapon.EvasionPercent(_now));
        var accuracy = a.Accuracy + (int)Math.Ceiling(a.Accuracy * attacker.Weapon.AccuracyPercent(_now) / 100f);
        var chance = Combat.CalculateHitRate(accuracy + accuracyBonus - penalty, evasion, hitPercent);
        attacker.Weapon.Count(ordinary ? "OrdinaryAttempts" : "AbilityImpactsAttempted");
        if (_random.Next(1, 101) > chance)
        {
            // Only native ordinary attack misses/deflections arm these production perks.
            if (ordinary) target.Weapon.AvoidedAttack(_now);
            return false;
        }
        var criticalChance = ordinary ? Combat.CalculateCriticalRate(a.Perception, d.Vitality, a.WeaponSkillRank, 0) : Combat.CalculateAbilityCriticalChance(0);
        var critical = a.EnableCriticals && _random.Next(1, 101) <= criticalChance ? Combat.StandardCriticalRating : 0;
        var attack = WeaponCombatState.Adjust(a.Attack, attacker.Weapon.Stat(StatType.AttackPercentAdjustment));
        var defense = WeaponCombatState.Adjust(d.Defense, target.Weapon.DefensePercent(_now));
        var (min, max) = Combat.CalculateDamageRange(attack, power, a.Might, defense, d.Vitality, critical, ordinary ? Math.Clamp(a.WeaponRequiredSkillRank/10+1,1,6) : 0);
        var amount = (int)(float)(_random.NextDouble() * (max - min) + min);
        if (ordinary) amount = Combat.ApplyPercentDamageAdjustment(amount, -attacker.Weapon.LowKick.Values.Select(x => x.Penalty).DefaultIfEmpty().Max());
        if (!ordinary && attacker.Spec.CombatReadinessPercent > 0)
            amount += (int)Math.Ceiling(amount * (attacker.Spec.CombatReadinessPercent / 100f));
        beforeDamage?.Invoke();
        ApplyDamage(attacker, target, amount, directHostile: true);
        attacker.Weapon.Count(ordinary ? "OrdinaryLanded" : "AbilityImpactsLanded");
        return true;
    }
}
