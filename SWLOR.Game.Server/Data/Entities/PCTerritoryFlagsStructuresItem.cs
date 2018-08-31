using System.ComponentModel.DataAnnotations;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class PCTerritoryFlagsStructuresItem
    {
        [Key]
        public long PCStructureItemID { get; set; }

        public long PCStructureID { get; set; }

        [Required]
        public string ItemName { get; set; }

        [Required]
        [StringLength(64)]
        public string ItemTag { get; set; }

        [Required]
        [StringLength(16)]
        public string ItemResref { get; set; }

        [Required]
        public string ItemObject { get; set; }

        [Required]
        [StringLength(60)]
        public string GlobalID { get; set; }

        public virtual PCTerritoryFlagsStructure PCTerritoryFlagsStructure { get; set; }
    }
}
