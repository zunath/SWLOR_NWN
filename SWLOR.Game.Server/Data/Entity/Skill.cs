using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Data.Entity
{
    public class Skill: IEntity
    {
        public Skill()
        {
            Name = "";
            Description = "";
        }

        [Key]
        public int ID { get; set; }
        public int SkillCategoryID { get; set; }
        public string Name { get; set; }
        public int MaxRank { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; }
        public Ability Primary { get; set; }
        public Ability Secondary { get; set; }
        public Ability Tertiary { get; set; }
        public bool ContributesToSkillCap { get; set; }
    }
}
