using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class CraftBlueprintCategory
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public CraftBlueprintCategory()
        {
            CraftBlueprints = new HashSet<CraftBlueprint>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long CraftBlueprintCategoryID { get; set; }

        [Required]
        [StringLength(32)]
        public string Name { get; set; }

        public bool IsActive { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CraftBlueprint> CraftBlueprints { get; set; }
    }
}
