using SWLOR.Game.Server.Data.Contracts;

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
        public int Primary { get; set; }
        public int Secondary { get; set; }
        public int Tertiary { get; set; }
        public bool ContributesToSkillCap { get; set; }
    }
}
