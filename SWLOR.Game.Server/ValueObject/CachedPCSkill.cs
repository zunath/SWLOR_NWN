using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.ValueObject
{
    public class CachedPCSkill
    {
        public int PCSkillID { get; set; }
        public string PlayerID { get; set; }
        public int SkillID { get; set; }
        public int XP { get; set; }
        public int Rank { get; set; }
        public bool IsLocked { get; set; }
        public bool ContributesToSkillCap { get; set; }
        public SkillType SkillType => (SkillType) SkillID;

        public CachedPCSkill(PCSkill dbPCSkill)
        {
            PCSkillID = dbPCSkill.PCSkillID;
            PlayerID = dbPCSkill.PlayerID;
            SkillID = dbPCSkill.SkillID;
            XP = dbPCSkill.XP;
            Rank = dbPCSkill.Rank;
            IsLocked = dbPCSkill.IsLocked;
            ContributesToSkillCap = dbPCSkill.Skill.ContributesToSkillCap;
        }
    }
}
