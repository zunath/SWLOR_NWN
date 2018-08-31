using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class PCKeyItem
    {
        public int PCKeyItemID { get; set; }

        [Required]
        [StringLength(60)]
        public string PlayerID { get; set; }

        public int KeyItemID { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime AcquiredDate { get; set; }

        public virtual KeyItem KeyItem { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
