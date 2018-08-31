using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class StructureBlueprint
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public StructureBlueprint()
        {
            ConstructionSites = new HashSet<ConstructionSite>();
            PCTerritoryFlags = new HashSet<PCTerritoryFlag>();
            PCTerritoryFlagsStructures = new HashSet<PCTerritoryFlagsStructure>();
            StructureComponents = new HashSet<StructureComponent>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int StructureBlueprintID { get; set; }

        public int StructureCategoryID { get; set; }

        [Required]
        [StringLength(64)]
        public string Name { get; set; }

        [Required]
        [StringLength(255)]
        public string Description { get; set; }

        [Required]
        [StringLength(16)]
        public string Resref { get; set; }

        public bool IsActive { get; set; }

        public bool IsTerritoryFlag { get; set; }

        public bool IsUseable { get; set; }

        public int ItemStorageCount { get; set; }

        public int VanityCount { get; set; }

        public double MaxBuildDistance { get; set; }

        public int Level { get; set; }

        public int? PerkID { get; set; }

        public int RequiredPerkLevel { get; set; }

        public bool GivesSkillXP { get; set; }

        public int SpecialCount { get; set; }

        public bool IsVanity { get; set; }

        public bool IsSpecial { get; set; }

        public int CraftTierLevel { get; set; }

        public int ResourceCount { get; set; }

        public int BuildingCount { get; set; }

        public bool IsResource { get; set; }

        public bool IsBuilding { get; set; }

        [Required]
        [StringLength(16)]
        public string ResourceResref { get; set; }

        public int? BuildingCategoryID { get; set; }

        public virtual BuildingCategory BuildingCategory { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ConstructionSite> ConstructionSites { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCTerritoryFlag> PCTerritoryFlags { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCTerritoryFlagsStructure> PCTerritoryFlagsStructures { get; set; }

        public virtual Perk Perk { get; set; }

        public virtual StructureCategory StructureCategory { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<StructureComponent> StructureComponents { get; set; }
    }
}
