
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PerkLevelSkillRequirements")]
    public partial class PerkLevelSkillRequirement: IEntity
    {
        [Key]
        public int PerkLevelSkillRequirementID { get; set; }
        public int PerkLevelID { get; set; }
        public int SkillID { get; set; }
        public int RequiredRank { get; set; }
    
        public virtual PerkLevel PerkLevel { get; set; }
        public virtual Skill Skill { get; set; }
    }
}
