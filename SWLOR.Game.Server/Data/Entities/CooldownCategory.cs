using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class CooldownCategory
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public CooldownCategory()
        {
            PCCooldowns = new HashSet<PCCooldown>();
            Perks = new HashSet<Perk>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CooldownCategoryID { get; set; }

        [Required]
        [StringLength(64)]
        public string Name { get; set; }

        public double BaseCooldownTime { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCCooldown> PCCooldowns { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Perk> Perks { get; set; }
    }
}
