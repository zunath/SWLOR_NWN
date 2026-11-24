using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The defense riders of an ability activation: the evasion, the attack and ranged deflection, the nearby ally defense and
    /// the perk category guard that an activation grants.
    /// </summary>
    public interface ICombatAbilityDefenseEffectsService
    {
        void ApplyAbilityUsedSkillEvasion(uint activator, AbilityDetail ability);

        void ApplyHostileAbilityUsedEvasion(uint activator, AbilityDetail ability, SkillType skillType);

        void ApplyCostlyAbilityUsedEvasion(uint activator, AbilityDetail ability, SkillType skillType);

        void ApplyAbilityUsedSkillRangedDeflection(uint activator, AbilityDetail ability);

        void ApplyAbilityUsedSkillAttackDeflection(uint activator, AbilityDetail ability);

        void ApplyAbilityUsedPerkCategoryAttackDeflection(uint activator, AbilityDetail ability);

        void ApplySingleTargetAbilityUsedAttackDeflection(uint activator, AbilityDetail ability, bool isSingleTargetAbility);

        void ApplyAreaAbilityUsedEvasion(uint activator, AbilityDetail ability, SkillType skillType, bool isAreaAbility);

        void ApplyAbilityUsedNearbyAllyDefense(uint activator);

        void ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection(uint activator, AbilityDetail ability);

        void ApplyAbilityUsedPerkCategorySelfDefense(uint activator, AbilityDetail ability);
    }
}
