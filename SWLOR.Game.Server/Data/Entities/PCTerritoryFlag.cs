using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class PCTerritoryFlag
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PCTerritoryFlag()
        {
            ConstructionSites = new HashSet<ConstructionSite>();
            PCTerritoryFlagsPermissions = new HashSet<PCTerritoryFlagsPermission>();
            PCTerritoryFlagsStructures = new HashSet<PCTerritoryFlagsStructure>();
            StructureQuickBuildAudits = new HashSet<StructureQuickBuildAudit>();
        }

        public int PCTerritoryFlagID { get; set; }

        [Required]
        [StringLength(60)]
        public string PlayerID { get; set; }

        public int StructureBlueprintID { get; set; }
        
        [StringLength(64)]
        public string LocationAreaTag { get; set; }

        public double LocationX { get; set; }

        public double LocationY { get; set; }

        public double LocationZ { get; set; }

        public double LocationOrientation { get; set; }

        public int BuildPrivacySettingID { get; set; }

        public bool ShowOwnerName { get; set; }

        public long? BuildingPCStructureID { get; set; }

        public bool IsActive { get; set; }

        public virtual BuildPrivacyDomain BuildPrivacyDomain { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ConstructionSite> ConstructionSites { get; set; }

        public virtual PCTerritoryFlagsStructure PCTerritoryFlagsStructure { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }

        public virtual StructureBlueprint StructureBlueprint { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCTerritoryFlagsPermission> PCTerritoryFlagsPermissions { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCTerritoryFlagsStructure> PCTerritoryFlagsStructures { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<StructureQuickBuildAudit> StructureQuickBuildAudits { get; set; }
    }
}
