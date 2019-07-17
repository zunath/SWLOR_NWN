using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[Skill]")]
    public class Skill: IEntity
    {
        public Skill()
        {
            Name = "";
            Description = "";
        }

        [ExplicitKey]
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

        public IEntity Clone()
        {
            return new Skill
            {
                ID = ID,
                SkillCategoryID = SkillCategoryID,
                Name = Name,
                MaxRank = MaxRank,
                IsActive = IsActive,
                Description = Description,
                Primary = Primary,
                Secondary = Secondary,
                Tertiary = Tertiary,
                ContributesToSkillCap = ContributesToSkillCap
            };
        }
    }
}
