
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PerkLevels")]
    public class PerkLevel: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PerkLevel()
        {
            Description = "";
            PerkLevelQuestRequirements = new HashSet<PerkLevelQuestRequirement>();
            PerkLevelSkillRequirements = new HashSet<PerkLevelSkillRequirement>();
        }

        [Key]
        public int PerkLevelID { get; set; }
        public int PerkID { get; set; }
        public int Level { get; set; }
        public int Price { get; set; }
        public string Description { get; set; }
    
        public virtual ICollection<PerkLevelQuestRequirement> PerkLevelQuestRequirements { get; set; }
        public virtual ICollection<PerkLevelSkillRequirement> PerkLevelSkillRequirements { get; set; }
    }
}
