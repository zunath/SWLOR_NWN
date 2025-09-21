using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Shared.Core.Contracts
{
    public interface ISkillService
    {
        int APCap { get; }
        void GiveSkillXP(uint player, SkillType skill, int xp, bool ignoreBonuses = false, bool applyHenchmanPenalty = true);
        void AddMissingSkills();
        int GetMaxDistributableXP(uint player, SkillType skillType);
    }
}
