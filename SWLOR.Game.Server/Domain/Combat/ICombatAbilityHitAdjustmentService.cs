using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Ability critical rate and hit adjustments for combat: the critical rate and roll, the skill and target adjustments, the sundered target reads and the evaded Force effects.
    /// </summary>
    public interface ICombatAbilityHitAdjustmentService
    {
        int CalculateAbilityCriticalRating(
            uint attacker,
            SkillType skillType,
            bool isAreaAbility,
            int criticalRateAdjustment = 0,
            uint defender = OBJECT_INVALID);

        int GetAbilityCriticalRate(
            uint attacker,
            SkillType skillType,
            bool isAreaAbility,
            int criticalRateAdjustment = 0,
            uint defender = OBJECT_INVALID);

        int GetAbilityHitOrCriticalAdjustment(
            uint creature,
            SkillType skillType,
            PerkType perkType,
            StatType skillTypeStat,
            StatType primaryPerkStat,
            StatType secondaryPerkStat,
            StatType adjustmentStat,
            bool includePerkTargeting,
            uint defender = OBJECT_INVALID);

        void ApplyRangedAbilityTargetDefenseReduction(
            uint attacker,
            uint defender,
            SkillType skillType);

        int GetHitChanceAgainstSunderedTargetAdjustment(uint attacker, uint defender);

        int GetCriticalRateAgainstSunderedTargetAdjustment(uint attacker, uint defender);

        void ApplyForceAbilityEvadedEffects(uint defender);
    }
}
