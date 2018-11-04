
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCSkills]")]
    public class PCSkill: IEntity
    {
        [Key]
        public int PCSkillID { get; set; }
        public string PlayerID { get; set; }
        public int SkillID { get; set; }
        public int XP { get; set; }
        public int Rank { get; set; }
        public bool IsLocked { get; set; }
    }
}
