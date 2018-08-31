using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class ConstructionSite
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ConstructionSite()
        {
            ConstructionSiteComponents = new HashSet<ConstructionSiteComponent>();
        }

        public int ConstructionSiteID { get; set; }

        public int? PCTerritoryFlagID { get; set; }

        [Required]
        [StringLength(60)]
        public string PlayerID { get; set; }

        public int StructureBlueprintID { get; set; }

        [Required]
        [StringLength(64)]
        public string LocationAreaTag { get; set; }

        public double LocationX { get; set; }

        public double LocationY { get; set; }

        public double LocationZ { get; set; }

        public double LocationOrientation { get; set; }

        public int? BuildingInteriorID { get; set; }

        public bool IsActive { get; set; }

        public virtual BuildingInterior BuildingInterior { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ConstructionSiteComponent> ConstructionSiteComponents { get; set; }

        public virtual PCTerritoryFlag PCTerritoryFlag { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }

        public virtual StructureBlueprint StructureBlueprint { get; set; }
    }
}
