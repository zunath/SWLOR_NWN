using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The ability hit roll of <c>Combat</c>: an ability's accuracy and hit chance with the adjustments the user, the
    /// target and the skill bring, and the roll itself. The critical rate and the hit adjustments are
    /// <see cref="CombatAbilityHitAdjustmentService"/>. The in-engine test overrides of the ability and auto-attack hit
    /// rolls are this instance's own fields.
    /// </summary>
    public class CombatAbilityHitService : ICombatAbilityHitService
    {
        private readonly ICombatQueriesService _queries;
        private readonly ICombatSuppressionService _suppression;
        private readonly ICombatActivityService _combatActivity;
        private readonly ICombatDamageModifiersService _damageModifiers;
        private readonly ICombatRateModifiersService _rateModifiers;
        private readonly ICombatRatingService _combatRating;
        private readonly IAbilityImpactState _impactState;
        private readonly IPerkForceAffinityService _perk;
        private readonly IPairedRangedTraitsService _pairedRangedTraits;
        private readonly ICombatAbilityHitAdjustmentService _hitAdjustment;

        public CombatAbilityHitService(
            ICombatQueriesService queries,
            ICombatSuppressionService suppression,
            ICombatActivityService combatActivity,
            ICombatDamageModifiersService damageModifiers,
            ICombatRateModifiersService rateModifiers,
            ICombatRatingService combatRating,
            IAbilityImpactState impactState,
            IPerkForceAffinityService perk,
            IPairedRangedTraitsService pairedRangedTraits,
            ICombatAbilityHitAdjustmentService hitAdjustment)
        {
            _queries = queries;
            _suppression = suppression;
            _combatActivity = combatActivity;
            _damageModifiers = damageModifiers;
            _rateModifiers = rateModifiers;
            _combatRating = combatRating;
            _impactState = impactState;
            _perk = perk;
            _pairedRangedTraits = pairedRangedTraits;
            _hitAdjustment = hitAdjustment;
        }

        private bool? _abilityHitResolutionOverride;

        /// <summary>
        /// Forces every TryResolveAbilityHit call to the given outcome instead of rolling.
        /// Intended solely for the in-engine test harness: ability behavior assertions cannot
        /// be made against a hit roll that legitimately misses up to 5% of the time even at
        /// capped hit rates. Pass null to restore normal resolution. Always restore in a
        /// finally block.
        /// </summary>
        public void SetAbilityHitResolutionOverride(bool? forcedOutcome)
        {
            _abilityHitResolutionOverride = forcedOutcome;
        }

        private bool? _autoAttackHitResolutionOverride;

        /// <summary>
        /// Forces every native auto-attack roll (ResolveAttackRoll hook) to the given outcome
        /// instead of rolling. Intended solely for the in-engine test harness: ability damage
        /// assertions cannot distinguish ability damage from the activator's resumed
        /// auto-attacks, so behavior sweeps force auto-attack misses. Pass null to restore
        /// normal resolution. Always restore in a finally block.
        /// </summary>
        public void SetAutoAttackHitResolutionOverride(bool? forcedOutcome)
        {
            _autoAttackHitResolutionOverride = forcedOutcome;
        }

        /// <summary>
        /// The current auto-attack resolution override, if any. Read by the native
        /// ResolveAttackRoll hook.
        /// </summary>
        public bool? GetAutoAttackHitResolutionOverride()
        {
            return _autoAttackHitResolutionOverride;
        }

        public bool TryResolveAbilityHit(
            uint attacker,
            uint defender,
            SkillType skillType,
            PerkType perkType,
            out int hitRate,
            int hitChancePercentAdjustment = 0,
            int skillLevelOverride = -1,
            AbilityType statOverride = AbilityType.Invalid,
            int accuracyFlatAdjustment = 0)
        {
            hitRate = 100;
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                skillType == SkillType.Invalid)
                return true;

            var accuracy = GetAbilityAccuracy(attacker, defender, skillType, skillLevelOverride, statOverride) + accuracyFlatAdjustment;
            var evasion = _combatRating.GetEvasion(defender, SkillType.Invalid, skillType);
            evasion = _rateModifiers.ApplySideAttackEvasionIgnore(attacker, defender, skillType, evasion);

            var modifier = hitChancePercentAdjustment + _hitAdjustment.GetAbilityHitOrCriticalAdjustment(
                attacker,
                skillType,
                perkType,
                StatType.AbilityHitChancePercentAdjustmentSkillType,
                StatType.AbilityHitChancePercentAdjustmentPerkType,
                StatType.AbilityHitChancePercentAdjustmentSecondaryPerkType,
                StatType.AbilityHitChancePercentAdjustment,
                false,
                defender);
            modifier += _queries.GetTargetedAbilityAdjustment(
                attacker,
                perkType,
                StatType.AbilityHitChancePercentAdjustmentPerkType,
                StatType.AbilityHitChancePercentAdjustmentSecondaryPerkType,
                StatType.TargetedAbilityHitChancePercentAdjustment);
            modifier += _queries.GetPhysicalAndForceAbilityHitChanceAdjustment(attacker, skillType);
            modifier += GetIncomingAbilityHitChanceAdjustment(defender, skillType);
            modifier += _rateModifiers.GetSideAttackHitChanceAdjustment(attacker, defender, skillType);
            modifier += _combatActivity.GetIdleAbilityHitChanceAdjustment(attacker, skillType);
            modifier += _suppression.ConsumeSuppressionRangedAttackAccuracyAdjustment(attacker, defender, skillType);
            modifier += _hitAdjustment.GetHitChanceAgainstSunderedTargetAdjustment(attacker, defender);
            if (skillType == SkillType.Force)
            {
                modifier += _perk.GetForceAffinityHitChanceAdjustment(attacker, perkType);
            }

            hitRate = CalculateHitRate(accuracy, evasion, modifier, _queries.GetMinimumHitRate(attacker));
            var isHit = _abilityHitResolutionOverride ?? Random.D100(1) <= hitRate;
            if (!isHit && GetIsReactionTypeHostile(attacker, defender))
                _pairedRangedTraits.RecordAccuracyEvasionMiss(defender);
            if (!isHit && skillType == SkillType.Force)
            {
                _hitAdjustment.ApplyForceAbilityEvadedEffects(defender);
            }

            return isHit;
        }

        private int GetAbilityAccuracy(
            uint attacker,
            uint defender,
            SkillType skillType,
            int skillLevelOverride = -1,
            AbilityType statOverride = AbilityType.Invalid)
        {
            if (WeaponImpactProfile.Get(attacker) is { } explicitProfile)
                return _damageModifiers.ApplyStatusSourceAccuracyModifiers(attacker, defender,
                    explicitProfile.Weapon.Accuracy + explicitProfile.AccuracyAdjustment + _impactState.GetWeaponTraitAccuracy(attacker));
            var weapon = _queries.GetRelevantSkillWeapon(attacker, skillType);
            var usesForceAccuracy = skillType == SkillType.Force;
            var accuracyStatOverride = usesForceAccuracy ? AbilityType.Willpower : statOverride;
            var accuracy = _combatRating.GetAccuracy(
                attacker,
                weapon,
                accuracyStatOverride,
                skillType,
                skillLevelOverride,
                ignoreWeaponAccuracyStatOverride: usesForceAccuracy);
            return _damageModifiers.ApplyStatusSourceAccuracyModifiers(attacker, defender, accuracy + _impactState.GetWeaponTraitAccuracy(attacker));
        }

        private int GetIncomingAbilityHitChanceAdjustment(uint defender, SkillType skillType)
        {
            return _queries.GetSkillSelectedStatAdjustment(
                defender,
                skillType,
                StatType.IncomingAbilityHitChancePercentAdjustmentSkillType,
                StatType.IncomingAbilityHitChancePercentAdjustment,
                unselectedSourcesApplyToEverySkill: true);
        }
    }
}
