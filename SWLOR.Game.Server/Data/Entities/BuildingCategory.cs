using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class BuildingCategory
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public BuildingCategory()
        {
            BuildingInteriors = new HashSet<BuildingInterior>();
            StructureBlueprints = new HashSet<StructureBlueprint>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int BuildingCategoryID { get; set; }

        [Required]
        [StringLength(32)]
        public string Name { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<BuildingInterior> BuildingInteriors { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<StructureBlueprint> StructureBlueprints { get; set; }
    }
}
