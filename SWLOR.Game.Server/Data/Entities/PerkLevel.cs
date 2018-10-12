using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class PerkLevel
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PerkLevel()
        {
            PerkLevelQuestRequirements = new HashSet<PerkLevelQuestRequirement>();
            PerkLevelSkillRequirements = new HashSet<PerkLevelSkillRequirement>();
        }

        public int PerkLevelID { get; set; }

        public int PerkID { get; set; }

        public int Level { get; set; }

        public int Price { get; set; }

        [Required]
        [StringLength(512)]
        public string Description { get; set; }

        public virtual Perk Perk { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PerkLevelQuestRequirement> PerkLevelQuestRequirements { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PerkLevelSkillRequirement> PerkLevelSkillRequirements { get; set; }
    }
}
