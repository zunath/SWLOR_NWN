using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CombatService;

public static class WeaponAbilityTraitPolicy
{
    public static bool IsEligibleSpender(bool hostile, SkillType skill, int paidStamina) =>
        hostile && paidStamina > 0 && Combat.IsWeaponSkillType(skill);

    public static bool IsHostileWeaponAbility(AbilityDetail ability) => ability?.IsHostileAbility == true &&
        Combat.IsWeaponSkillType(ability.SkillType);

    public static bool IsEligibleCostReduction(SkillType skill, int stamina) => stamina > 0 && IsCombatStaminaAbility(skill);

    public static bool IsCombatStaminaAbility(SkillType skill)
    {
        if (skill == SkillType.Invalid) return false;
        return skill.GetAttribute<SkillType, SkillAttribute>().Category is SkillCategoryType.Combat or SkillCategoryType.Utility;
    }
}
