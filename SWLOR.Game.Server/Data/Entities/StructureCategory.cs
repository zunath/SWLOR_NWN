using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class StructureCategory
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public StructureCategory()
        {
            StructureBlueprints = new HashSet<StructureBlueprint>();
        }

        public int StructureCategoryID { get; set; }

        [Required]
        [StringLength(64)]
        public string Name { get; set; }

        [Required]
        [StringLength(64)]
        public string Description { get; set; }

        public bool IsActive { get; set; }

        public bool IsTerritoryFlagCategory { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<StructureBlueprint> StructureBlueprints { get; set; }
    }
}
