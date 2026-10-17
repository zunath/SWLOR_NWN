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
    private int DiscountedStamina(Combatant caster, int cost, bool consume)
    {
        if (caster.Weapon == null || cost <= 0) return cost;
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
    private (bool Landed, StatusEffectCategory AppliedCategories) ApplyWeaponAbility(Combatant caster, Combatant target, PendingCast cast)
    {
        if (target.IsDead) return (false, StatusEffectCategory.None);
        var technique = cast.Ability.Technique;
        var count = technique?.ImpactCount ?? cast.Use.WeaponHits;
        var weaponPercent = technique?.WeaponDamagePercent ?? cast.Use.WeaponDamagePercent;
        var landed = false;
        for (var index = 0; index < count; index++)
        {
            if (target.IsDead) break;
            var power = cast.Use.WeaponPower.Value + (int)(0.15f * caster.Spec.WeaponCombat.WeaponSkillRank) + TechniqueSequence.ScaleWeaponDamage(caster.Spec.WeaponCombat.WeaponDamage, weaponPercent) +
                TechniqueSequence.SplitPower(cast.Technique.ConditionalPower, count, index) + cast.Technique.FlatPower;
            var hit = RollWeapon(caster, target, power, cast.Technique.Accuracy, cast.Technique.HitPercent, false);
            landed |= hit;
            if (hit && cast.Technique.PaidStamina >= caster.Weapon.Stat(StatType.CostlyAbilityDamageMinimumStaminaCost) && caster.Weapon.Stat(StatType.CostlyAbilityDamageBonus) > 0)
                caster.Weapon.Count("HighGuardImpacts");
        }
        var categories = landed && !target.IsDead ? ApplyWeaponStatuses(caster, target, cast.Ability) : StatusEffectCategory.None;
        return (landed, categories);
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
    private bool RollWeapon(Combatant attacker, Combatant target, int power, int accuracyBonus, int hitPercent, bool ordinary)
    {
        var a = attacker.Spec.WeaponCombat; var d = target.Spec.WeaponCombat;
        attacker.Weapon.Refresh(_now); target.Weapon.Refresh(_now);
        var penalty = ordinary ? attacker.Weapon.Rattled.Values.Select(x => x.Penalty).DefaultIfEmpty().Max() : 0;
        var evasion = WeaponCombatState.Adjust(d.Evasion, target.Weapon.EvasionPercent(_now));
        var chance = Combat.CalculateHitRate(a.Accuracy + accuracyBonus - penalty, evasion, hitPercent);
        attacker.Weapon.Count(ordinary ? "OrdinaryAttempts" : "AbilityImpactsAttempted");
        if (_random.Next(1, 101) > chance) return false;
        var criticalChance = ordinary ? Combat.CalculateCriticalRate(a.Perception, d.Vitality, a.WeaponSkillRank, 0) : Combat.CalculateAbilityCriticalChance(0);
        var critical = a.EnableCriticals && _random.Next(1, 101) <= criticalChance ? Combat.StandardCriticalRating : 0;
        var attack = WeaponCombatState.Adjust(a.Attack, attacker.Weapon.Stat(StatType.AttackPercentAdjustment));
        var defense = WeaponCombatState.Adjust(d.Defense, target.Weapon.DefensePercent(_now));
        var (min, max) = Combat.CalculateDamageRange(attack, power, a.Might, defense, d.Vitality, critical, ordinary ? Math.Clamp(a.WeaponRequiredSkillRank/10+1,1,6) : 0);
        var amount = (int)(float)(_random.NextDouble() * (max - min) + min);
        if (ordinary) amount = Combat.ApplyPercentDamageAdjustment(amount, -attacker.Weapon.LowKick.Values.Select(x => x.Penalty).DefaultIfEmpty().Max());
        if (!ordinary && attacker.Spec.CombatReadinessPercent > 0)
            amount += (int)Math.Ceiling(amount * (attacker.Spec.CombatReadinessPercent / 100f));
        ApplyDamage(attacker, target, amount, directHostile: true);
        attacker.Weapon.Count(ordinary ? "OrdinaryLanded" : "AbilityImpactsLanded");
        return true;
    }
}
