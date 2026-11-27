using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The ability activation part of <c>Combat</c>: the order of the riders that run when a creature activates an ability
    /// (the steps are the public members of the cost, defense, offense and weapon parts), the queued weapon ability use and
    /// the skill an activation is attributed to.
    /// </summary>
    public class CombatAbilityActivationService : ICombatAbilityActivationService
    {
        private readonly ICombatQueriesService _queries;
        private readonly ICombatActivityService _combatActivity;
        private readonly IAbilityImpactState _impactState;
        private readonly ICombatAbilityCostEffectsService _costEffects;
        private readonly ICombatAbilityDefenseEffectsService _defenseEffects;
        private readonly ICombatAbilityOffenseEffectsService _offenseEffects;
        private readonly ICombatAbilityWeaponActivationService _weaponActivation;

        public CombatAbilityActivationService(
            ICombatQueriesService queries,
            ICombatActivityService combatActivity,
            IAbilityImpactState impactState,
            ICombatAbilityCostEffectsService costEffects,
            ICombatAbilityDefenseEffectsService defenseEffects,
            ICombatAbilityOffenseEffectsService offenseEffects,
            ICombatAbilityWeaponActivationService weaponActivation)
        {
            _queries = queries;
            _combatActivity = combatActivity;
            _impactState = impactState;
            _costEffects = costEffects;
            _defenseEffects = defenseEffects;
            _offenseEffects = offenseEffects;
            _weaponActivation = weaponActivation;
        }

        public void ApplyAbilityActivatedEffects(
            uint activator,
            uint target,
            FeatType feat,
            AbilityDetail ability,
            AbilityImpactSummary summary)
        {
            if (!GetIsObjectValid(activator) || ability == null || summary == null)
                return;

            _costEffects.ApplyAbilityUsedRecastReduction(activator, ability);
            _costEffects.ApplyAbilityUsedNextSkillAutoAttackDamageBonus(activator, ability);
            _costEffects.ApplyAbilityUsedNextSkillFPCostAdjustment(activator, ability);
            _costEffects.ApplyAbilityUsedMasterAbilityHitChance(activator);
            _costEffects.ApplyForceFPCostActivatedEffects(activator, ability);

            var skillType = ResolveActivatedAbilitySkillType(activator, ability, summary);
            var isSingleTargetAbility = summary.IsSingleTargetAbility ||
                summary.SkillType == SkillType.Invalid &&
                ability.IsHostileAbility &&
                ability.IsSingleTargetAbility;

            _defenseEffects.ApplyAbilityUsedSkillEvasion(activator, ability);
            _defenseEffects.ApplyHostileAbilityUsedEvasion(activator, ability, skillType);
            _defenseEffects.ApplyCostlyAbilityUsedEvasion(activator, ability, skillType);
            _defenseEffects.ApplyAbilityUsedSkillRangedDeflection(activator, ability);
            _offenseEffects.ApplyAbilityUsedMovementSpeed(activator, ability, skillType);
            _defenseEffects.ApplyAbilityUsedSkillAttackDeflection(activator, ability);
            _defenseEffects.ApplyAbilityUsedPerkCategoryAttackDeflection(activator, ability);
            _defenseEffects.ApplySingleTargetAbilityUsedAttackDeflection(activator, ability, isSingleTargetAbility);
            var isAreaAbility = _impactState.IsAreaImpact(summary, ability);
            _defenseEffects.ApplyAreaAbilityUsedEvasion(activator, ability, skillType, isAreaAbility);
            _offenseEffects.ApplyAreaAbilityUsedRewards(activator, ability, isAreaAbility);
            _offenseEffects.ApplyHostileAbilityForceAttack(activator, ability);
            _offenseEffects.ApplyHostileAbilityFPSpendForceAttack(activator, ability);
            _defenseEffects.ApplyAbilityUsedNearbyAllyDefense(activator);
            _defenseEffects.ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection(activator, ability);
            _defenseEffects.ApplyAbilityUsedPerkCategorySelfDefense(activator, ability);
            _offenseEffects.ApplyHostileAbilityUsedAttackAdjustment(activator, ability);
            _weaponActivation.ApplyAbilityActivatedRiders(activator, target, ability, skillType);
            _offenseEffects.ApplyHostileAbilitySequenceEffects(activator, feat, ability);
            _offenseEffects.ApplyHostileAbilityResourceRestoreEffects(activator, ability);

            if (ability.ActivationType != AbilityActivationType.Weapon)
                _combatActivity.TrackCombatAbilityUse(activator, ability);
        }

        public void TrackQueuedWeaponAbilityUse(uint activator, AbilityDetail ability)
        {
            _combatActivity.TrackCombatAbilityUse(activator, ability);
        }

        private SkillType ResolveActivatedAbilitySkillType(
            uint activator,
            AbilityDetail ability,
            AbilityImpactSummary summary)
        {
            return summary.SkillType != SkillType.Invalid
                ? summary.SkillType
                : _queries.GetAbilitySkillType(activator, ability);
        }
    }
}
