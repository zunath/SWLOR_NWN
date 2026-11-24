using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The damage a guard returns on a guarded hit: the retaliation, the retaliation pulse and the damage roll of both.
    /// </summary>
    public class CombatGuardRetaliationService : ICombatGuardRetaliationService
    {
        private readonly ICombatQueriesService _queries;
        private readonly ICombatDamageModifiersService _damageModifiers;
        private readonly ICombatHitEffectsService _hitEffects;
        private readonly IStatService _stat;
        private readonly ICombatRatingService _combatRating;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly IEnmityService _enmity;
        private readonly ICombatDamageRollService _damageRoll;
        private readonly ICombatDamageTakenService _damageTaken;

        public CombatGuardRetaliationService(
            ICombatQueriesService queries,
            ICombatDamageModifiersService damageModifiers,
            ICombatHitEffectsService hitEffects,
            IStatService stat,
            ICombatRatingService combatRating,
            ICombatRecorderService combatRecorder,
            IEnmityService enmity,
            ICombatDamageRollService damageRoll,
            ICombatDamageTakenService damageTaken)
        {
            _queries = queries;
            _damageModifiers = damageModifiers;
            _hitEffects = hitEffects;
            _stat = stat;
            _combatRating = combatRating;
            _combatRecorder = combatRecorder;
            _enmity = enmity;
            _damageRoll = damageRoll;
            _damageTaken = damageTaken;
        }

        // CalculateDamage lives on the damage roll part because it rolls the shared Random; guard retaliation damage reaches it there.
        protected virtual int CalculateDamage(
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical,
            int deltaCap = 0) =>
            _damageRoll.CalculateDamage(attackerAttack, attackerDMG, attackerStat, defenderDefense, defenderStat, critical, deltaCap);

        public void ApplyGuardedHitRetaliation(uint attacker, uint defender, int incomingDamage)
        {
            var skillType = _queries.GetEquippedWeaponSkillType(defender);
            var retaliationDMG = _stat.GetStatAdjustment(defender, StatType.GuardRetaliationDMG);
            var pulseDMG = _stat.GetStatAdjustment(defender, StatType.GuardedHitPulseDMG);

            // DMG is an input to the attack-versus-defense damage range, not already-resolved HP damage.
            // Resolve each retaliation before using the non-recursive triggered-damage delivery path.
            if (retaliationDMG > 0)
            {
                var retaliationDamage = ResolveGuardRetaliationDamage(
                    defender,
                    attacker,
                    retaliationDMG,
                    skillType);
                using var retaliationAttribution = _combatRecorder.BeginStatTriggerAttribution(defender, StatType.GuardRetaliationDMG);
                _hitEffects.ApplyTriggeredDamage(defender, attacker, retaliationDamage, CombatDamageType.Physical, skillType);
            }

            if (pulseDMG <= 0)
                return;

            using var pulseAttribution = _combatRecorder.BeginStatTriggerAttribution(defender, StatType.GuardedHitPulseDMG);

            var radius = _stat.GetStatAdjustment(defender, StatType.GuardedHitPulseRadiusMeters);
            if (radius <= 0)
            {
                var pulseDamage = ResolveGuardRetaliationDamage(defender, attacker, pulseDMG, skillType);
                _hitEffects.ApplyTriggeredDamage(defender, attacker, pulseDamage, CombatDamageType.Physical, skillType);
                return;
            }

            ApplyGuardedHitRetaliationPulse(defender, attacker, incomingDamage, pulseDMG, radius, skillType);
        }

        private void ApplyGuardedHitRetaliationPulse(
            uint defender,
            uint originalAttacker,
            int incomingDamage,
            int dmg,
            float radius,
            SkillType skillType)
        {
            var enmityPercent = _stat.GetStatAdjustment(
                defender,
                StatType.GuardedHitPulseEnmityPercentOfIncomingDamage);
            var additionalTargetEnmity = enmityPercent > 0
                ? Math.Max(1, GameMath.PercentOf(incomingDamage, enmityPercent))
                : 0;
            var location = GetLocation(defender);
            var applied = false;

            // The creature whose hit was guarded is always affected, even when attacking from
            // beyond the nearby-enemy radius. Normal Guard handling supplies this target's _enmity.
            if (GetIsObjectValid(originalAttacker) &&
                !GetIsDead(originalAttacker) &&
                GetCurrentHitPoints(originalAttacker) > 0 &&
                GetIsReactionTypeHostile(originalAttacker, defender))
            {
                var damage = ResolveGuardRetaliationDamage(defender, originalAttacker, dmg, skillType);
                _hitEffects.ApplyTriggeredDamage(defender, originalAttacker, damage, CombatDamageType.Physical, skillType);
                applied = true;
            }

            foreach (var target in ObjectSnapshot.InShape(Shape.Sphere, radius, location, true, SWLOR.NWN.API.NWScript.Enum.ObjectType.Creature))
            {
                if (!GetIsObjectValid(target))
                    continue;

                if (target != originalAttacker &&
                    !GetIsDead(target) &&
                    GetCurrentHitPoints(target) > 0 &&
                    GetIsReactionTypeHostile(target, defender))
                {
                    var damage = ResolveGuardRetaliationDamage(defender, target, dmg, skillType);
                    _hitEffects.ApplyTriggeredDamage(defender, target, damage, CombatDamageType.Physical, skillType);
                    if (additionalTargetEnmity > 0)
                        _enmity.ModifyEnmity(defender, target, additionalTargetEnmity);

                    applied = true;
                }

            }

            if (applied && GetIsPC(defender))
                FloatingTextStringOnCreature(ColorToken.Combat("Retaliation Pulse"), defender, false);
        }

        private int ResolveGuardRetaliationDamage(
            uint source,
            uint target,
            int dmg,
            SkillType skillType)
        {
            if (!GetIsObjectValid(source) || !GetIsObjectValid(target) || dmg <= 0)
                return 0;

            const CombatDamageType damageType = CombatDamageType.Physical;
            var damageAbility = GetGuardRetaliationDamageAbility(source, skillType);
            var attack = _combatRating.GetAttack(source, damageAbility, skillType);
            attack = _damageModifiers.ApplyTargetStatusAttackModifiers(source, target, attack, skillType);
            var attackStat = GetAbilityScore(source, damageAbility);
            var defenseAbility = damageType.GetDefenseAbilityType();
            var defense = _combatRating.GetDefense(target, damageType, defenseAbility);
            defense = _damageModifiers.ApplyStatusSourceDefenseModifiers(source, target, defense);
            defense = _damageTaken.ApplyIncomingPhysicalToForceDefenseConversion(
                target,
                damageType,
                defense,
                () => _damageModifiers.ApplyStatusSourceDefenseModifiers(
                    source,
                    target,
                    _combatRating.GetDefense(target, CombatDamageType.Force, CombatDamageType.Force.GetDefenseAbilityType())));
            var defenderStat = GetAbilityScore(target, defenseAbility);

            return CalculateDamage(
                attack,
                dmg,
                attackStat,
                defense,
                defenderStat,
                0);
        }

        private AbilityType GetGuardRetaliationDamageAbility(uint defender, SkillType skillType)
        {
            var weapon = _queries.GetRelevantSkillWeapon(defender, skillType);
            if (!GetIsObjectValid(weapon))
                return AbilityType.Might;

            var ability = _queries.GetWeaponDamageAbilityType(defender, GetBaseItemType(weapon));
            return ability == AbilityType.Invalid
                ? AbilityType.Might
                : ability;
        }
    }
}
