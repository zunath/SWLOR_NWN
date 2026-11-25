using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The damage bonuses an ability hit reads before its damage: the base bonus (the skill traits, the first hit bonus, the status
    /// category and stealth bonuses, the guarded hit bonus), the high resource bonus and the costly ability bonus.
    /// </summary>
    public class CombatAbilityDamageBonusService : ICombatAbilityDamageBonusService
    {
        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatActivityService _combatActivity;
        private readonly ICombatGuardedHitEffectsService _guardedHitEffects;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;

        public CombatAbilityDamageBonusService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatActivityService combatActivity,
            ICombatGuardedHitEffectsService guardedHitEffects,
            IStatService stat,
            IStatusEffectService statusEffect,
            ITemporaryStatModifierService temporaryStatModifier)
        {
            _queries = queries;
            _resources = resources;
            _combatActivity = combatActivity;
            _guardedHitEffects = guardedHitEffects;
            _stat = stat;
            _statusEffect = statusEffect;
            _temporaryStatModifier = temporaryStatModifier;
        }

        public int GetAbilityImpactBaseDamageBonus(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null || !GetIsObjectValid(activator))
                return 0;

            var bonus = 0;

            switch (skillType)
            {
                case SkillType.Lightsaber:
                    if (ability.IsAreaAbility)
                    {
                        bonus += _stat.GetStatAdjustment(activator, StatType.LightsaberOffenseAreaDamageBonus);
                    }

                    if (ability.IsSingleTargetAbility &&
                        GetIsObjectValid(target) &&
                        _statusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Debuff))
                    {
                        bonus += _stat.GetStatAdjustment(activator, StatType.LightsaberOffenseDebuffedTargetDamageBonus);
                    }
                    break;
                case SkillType.Vibroknife when ability.IsHostileAbility:
                    var toxicCoatingRank = _stat.GetStatAdjustment(activator, StatType.VibroknifeSaboteurToxicCoatingRank);
                    if (toxicCoatingRank > 0)
                    {
                        bonus += toxicCoatingRank >= 2 ? 22 : 10;
                    }
                    break;
                case SkillType.Staff when ability.IsHostileAbility:
                    bonus += _stat.GetStatAdjustment(activator, StatType.StaffCrusherFinisherDamageBonus);
                    break;
                case SkillType.Saberstaff when ability.IsHostileAbility:
                    bonus += _stat.GetStatAdjustment(activator, StatType.SaberstaffConduitFlareDamageBonus);
                    break;
                case SkillType.TwinBlade when ability.IsHostileAbility &&
                    _queries.AbilityMatchesReversalCutTrigger(activator, ability):
                    bonus += _temporaryStatModifier.Consume(
                        activator,
                        StatType.TwinBladeDuelistReversalCutDamageBonus,
                        StatType.TwinBladeDuelistReversalCut);
                    break;
            }

            if (ability.IsHostileAbility)
            {
                bonus += _combatActivity.GetFirstHostileAbilityHitDamageBonus(activator, ability);
                bonus += _combatActivity.GetDirectDamageToStatusCategoryOrStealthBonus(activator, target);
            }

            bonus += _guardedHitEffects.ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(activator, skillType);

            if (ability.IsHostileAbility)
                bonus += GetHighResourceAbilityDamageBonus(activator, ability);

            return bonus;
        }

        public int GetHighResourceAbilityDamageBonus(uint activator, AbilityDetail ability = null)
        {
            return _resources.GetHighResourceStatAdjustment(activator, StatType.HighFPAndStaminaAbilityDamageBonus,
                StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent, ability);
        }

        public int GetCostlyAbilityDamageBonus(
            uint activator,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (!_resources.TryGetAbilityStaminaCostState(activator, ability, out var costState))
                return 0;

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityDamageBonusSkillType));
            var minimumCost = _stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityDamageMinimumStaminaCost);
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType) ||
                minimumCost <= 0 ||
                costState.Cost < minimumCost)
            {
                return 0;
            }

            return _stat.GetStatAdjustment(activator, StatType.CostlyAbilityDamageBonus);
        }
    }
}
