using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class BuildingInterior
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public BuildingInterior()
        {
            ConstructionSites = new HashSet<ConstructionSite>();
            PCTerritoryFlagsStructures = new HashSet<PCTerritoryFlagsStructure>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int BuildingInteriorID { get; set; }

        public int BuildingCategoryID { get; set; }

        [Required]
        [StringLength(16)]
        public string AreaResref { get; set; }

        [Required]
        [StringLength(64)]
        public string Name { get; set; }

        public bool IsDefaultForCategory { get; set; }

        public virtual BuildingCategory BuildingCategory { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ConstructionSite> ConstructionSites { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCTerritoryFlagsStructure> PCTerritoryFlagsStructures { get; set; }
    }
}
