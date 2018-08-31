using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    [Table("SkillXPRequirement")]
    public partial class SkillXPRequirement
    {
        public int SkillXPRequirementID { get; set; }

        public int SkillID { get; set; }

        public int Rank { get; set; }

        public int XP { get; set; }

        public virtual Skill Skill { get; set; }
    }
}
