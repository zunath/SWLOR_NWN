using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class SkillCategory
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public SkillCategory()
        {
            Skills = new HashSet<Skill>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SkillCategoryID { get; set; }

        [Required]
        [StringLength(32)]
        public string Name { get; set; }

        public bool IsActive { get; set; }

        public int Sequence { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Skill> Skills { get; set; }
    }
}
