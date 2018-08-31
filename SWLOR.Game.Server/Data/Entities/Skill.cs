using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class Skill
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Skill()
        {
            CraftBlueprints = new HashSet<CraftBlueprint>();
            PCSkills = new HashSet<PCSkill>();
            PerkLevelSkillRequirements = new HashSet<PerkLevelSkillRequirement>();
            SkillXPRequirements = new HashSet<SkillXPRequirement>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SkillID { get; set; }

        public int SkillCategoryID { get; set; }

        [Required]
        [StringLength(32)]
        public string Name { get; set; }

        public int MaxRank { get; set; }

        public bool IsActive { get; set; }

        [Required]
        [StringLength(1024)]
        public string Description { get; set; }

        public int Primary { get; set; }

        public int Secondary { get; set; }

        public int Tertiary { get; set; }

        public virtual Attribute Attribute { get; set; }

        public virtual Attribute Attribute1 { get; set; }

        public virtual Attribute Attribute2 { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CraftBlueprint> CraftBlueprints { get; set; }
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCSkill> PCSkills { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PerkLevelSkillRequirement> PerkLevelSkillRequirements { get; set; }

        public virtual SkillCategory SkillCategory { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<SkillXPRequirement> SkillXPRequirements { get; set; }
    }
}
