using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Domain.Combat;

public static class WeaponAbilityTraitPolicy
{
    public static bool IsEligibleSpender(bool hostile, SkillType skill, int paidStamina) =>
        hostile && paidStamina > 0 && CombatRules.IsWeaponSkillType(skill);

    public static bool IsHostileWeaponAbility(AbilityDetail ability) => ability?.IsHostileAbility == true &&
        CombatRules.IsWeaponSkillType(ability.SkillType);

    public static bool IsEligibleCostReduction(SkillType skill, int stamina) => stamina > 0 && IsCombatStaminaAbility(skill);

    public static bool IsCombatStaminaAbility(SkillType skill)
    {
        if (skill == SkillType.Invalid) return false;
        return skill.GetAttribute<SkillType, SkillAttribute>().Category is SkillCategoryType.Combat or SkillCategoryType.Utility;
    }
}
