using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    [Table("StructureQuickBuildAudit")]
    public partial class StructureQuickBuildAudit
    {
        [Key]
        public int StructureQuickBuildID { get; set; }

        public int? PCTerritoryFlagID { get; set; }

        public long? PCTerritoryFlagStructureID { get; set; }

        [Required]
        [StringLength(200)]
        public string DMName { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime DateBuilt { get; set; }

        public virtual PCTerritoryFlag PCTerritoryFlag { get; set; }

        public virtual PCTerritoryFlagsStructure PCTerritoryFlagsStructure { get; set; }
    }
}
