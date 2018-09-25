using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class PCPerkRefund
    {
        public int PCPerkRefundID { get; set; }

        [Required]
        [StringLength(60)]
        public string PlayerID { get; set; }

        public int PerkID { get; set; }

        public int Level { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime DateAcquired { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime DateRefunded { get; set; }

        public virtual Perk Perk { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
