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
    /// The guarded hit part of <c>Combat</c>: a Guard roll that lands removes its share of the physical damage, then restores
    /// stamina, draws the guard's retaliation and gives the guard its enmity. The effects the hit grants the guard and the
    /// retaliation are parts of their own.
    /// </summary>
    public class CombatGuardedHitService : ICombatGuardedHitService
    {
        private readonly ICombatFeedbackService _feedback;
        private readonly ICombatRatingService _combatRating;
        private readonly IStatusEffectCombatEventService _statusEffectCombatEvents;
        private readonly ICombatDamageTakenService _damageTaken;
        private readonly ICombatGuardedHitEffectsService _guardedHitEffects;
        private readonly ICombatGuardRetaliationService _retaliation;
        private readonly IStatService _stat;
        private readonly ICombatResourcesService _resources;
        private readonly IEnmityService _enmity;

        public CombatGuardedHitService(
            ICombatFeedbackService feedback,
            ICombatRatingService combatRating,
            IStatusEffectCombatEventService statusEffectCombatEvents,
            ICombatDamageTakenService damageTaken,
            ICombatGuardedHitEffectsService guardedHitEffects,
            ICombatGuardRetaliationService retaliation,
            IStatService stat,
            ICombatResourcesService resources,
            IEnmityService enmity)
        {
            _feedback = feedback;
            _combatRating = combatRating;
            _statusEffectCombatEvents = statusEffectCombatEvents;
            _damageTaken = damageTaken;
            _guardedHitEffects = guardedHitEffects;
            _retaliation = retaliation;
            _stat = stat;
            _resources = resources;
            _enmity = enmity;
        }

        public int ApplyGuardedHitModifiers(
            uint defender,
            uint attacker,
            int damage,
            CombatDamageType damageType,
            bool isLandedAttack)
        {
            if (!isLandedAttack ||
                !GetIsObjectValid(defender) ||
                !GetIsObjectValid(attacker) ||
                defender == attacker ||
                damage <= 0 ||
                !damageType.IsPhysicalDamageType() ||
                !_damageTaken.IsHostileAttackSource(defender, attacker))
                return damage;

            var guardChance = _combatRating.GetGuardChance(defender);
            if (guardChance <= 0 || Random.D100(1) > guardChance)
                return damage;

            var reductionPercent = _damageTaken.GetGuardDamageReductionPercent(defender);
            var preventedDamage = Math.Min(damage, GameMath.PercentOf(damage, reductionPercent));
            var adjustedDamage = Math.Max(0, damage - preventedDamage);

            _guardedHitEffects.TrackGuardedHit(defender);
            _statusEffectCombatEvents.OnGuardedHit(defender, attacker, preventedDamage);
            ApplyGuardedHitRecovery(defender);
            _retaliation.ApplyGuardedHitRetaliation(attacker, defender, damage);
            ApplyGuardedHitEnmity(attacker, defender, damage);
            _feedback.SendGuardedHitFeedback(defender, attacker, preventedDamage);

            return adjustedDamage;
        }

        private void ApplyGuardedHitRecovery(uint defender)
        {
            var staminaRestore = _stat.GetStatAdjustment(defender, StatType.GuardStaminaRestore);
            var cooldown = _stat.GetStatAdjustment(defender, StatType.GuardStaminaRestoreCooldownSeconds);
            if (staminaRestore > 0 &&
                _resources.TryUseStatTrigger(defender, StatType.GuardStaminaRestore, cooldown))
            {
                _resources.RestoreStaminaFromStat(defender, StatType.GuardStaminaRestore, staminaRestore);
            }
        }

        private void ApplyGuardedHitEnmity(uint attacker, uint defender, int damage)
        {
            var enmity = Math.Max(1, damage);
            var percentAdjustment = _stat.GetStatAdjustment(defender, StatType.GuardEnmityPercentAdjustment);
            if (percentAdjustment != 0)
            {
                enmity = Math.Max(1, (int)Math.Ceiling(enmity * ((100 + percentAdjustment) / 100f)));
            }

            _enmity.ModifyEnmity(defender, attacker, enmity);
        }
    }
}
