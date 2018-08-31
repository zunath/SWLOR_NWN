using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class PCCooldown
    {
        public int PCCooldownID { get; set; }

        [Required]
        [StringLength(60)]
        public string PlayerID { get; set; }

        public int CooldownCategoryID { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime DateUnlocked { get; set; }

        public virtual CooldownCategory CooldownCategory { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
