using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.ValueObject
{
    public class CachedSkillXPRequirement
    {
        public int SkillXPRequirementID { get; set; }
        public int SkillID { get; set; }
        public int Rank { get; set; }
        public int XP { get; set; }

        public CachedSkillXPRequirement(SkillXPRequirement xpReq)
        {
            SkillXPRequirementID = xpReq.SkillXPRequirementID;
            SkillID = xpReq.SkillID;
            Rank = xpReq.Rank;
            XP = xpReq.XP;
        }
    }
}
