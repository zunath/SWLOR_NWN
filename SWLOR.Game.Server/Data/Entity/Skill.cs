
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("Skills")]
    public class Skill: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Skill()
        {
            Name = "";
            Description = "";
            SkillXPRequirements = new HashSet<SkillXPRequirement>();
        }

        [ExplicitKey]
        public int SkillID { get; set; }
        public int SkillCategoryID { get; set; }
        public string Name { get; set; }
        public int MaxRank { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; }
        public int Primary { get; set; }
        public int Secondary { get; set; }
        public int Tertiary { get; set; }
        public bool ContributesToSkillCap { get; set; }
    
        public virtual ICollection<SkillXPRequirement> SkillXPRequirements { get; set; }
    }
}
