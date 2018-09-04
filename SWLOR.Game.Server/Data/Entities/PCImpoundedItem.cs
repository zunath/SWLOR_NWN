using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    [Table("PCImpoundedItems")]
    public partial class PCImpoundedItem
    {
        public int PCImpoundedItemID { get; set; }

        [Required]
        [StringLength(60)]
        public string PlayerID { get; set; }

        [Required]
        [StringLength(64)]
        public string ItemName { get; set; }

        [Required]
        [StringLength(32)]
        public string ItemTag { get; set; }

        [Required]
        [StringLength(16)]
        public string ItemResref { get; set; }

        [Required]
        public string ItemObject { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime DateImpounded { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? DateRetrieved { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
