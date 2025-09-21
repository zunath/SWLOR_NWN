using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Contracts
{
    public interface ISkillService
    {
        int GetSkillLevel(uint creature, SkillType skill);
        void GiveSkillXP(uint creature, SkillType skill, int xp);
    }
}
