using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CombatService
{
    internal static class CombatSkillType
    {
        public static bool IsRangedDamageSkill(SkillType skillType)
        {
            return skillType == SkillType.Pistol ||
                   skillType == SkillType.Rifle ||
                   skillType == SkillType.Throwing ||
                   skillType == SkillType.Devices;
        }

        public static bool IsRangedWeaponSkill(SkillType skillType)
        {
            return skillType == SkillType.Pistol ||
                   skillType == SkillType.Rifle ||
                   skillType == SkillType.Throwing;
        }
    }
}
