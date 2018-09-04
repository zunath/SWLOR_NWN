using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class Area
    {
        [StringLength(60)]
        public string AreaID { get; set; }

        [Required]
        [StringLength(16)]
        public string Resref { get; set; }

        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        [Required]
        [StringLength(32)]
        public string Tag { get; set; }

        public int ResourceQuality { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public bool IsBuildable { get; set; }

        [StringLength(60)]
        public string NorthwestOwner { get; set; }

        [StringLength(60)]
        public string NortheastOwner { get; set; }

        [StringLength(60)]
        public string SouthwestOwner { get; set; }

        [StringLength(60)]
        public string SoutheastOwner { get; set; }

        public bool IsActive { get; set; }

        public int PurchasePrice { get; set; }

        public int DailyUpkeep { get; set; }


        public virtual PlayerCharacter NortheastOwnerPlayer { get; set; }

        public virtual PlayerCharacter NorthwestOwnerPlayer { get; set; }

        public virtual PlayerCharacter SoutheastOwnerPlayer { get; set; }

        public virtual PlayerCharacter SouthwestOwnerPlayer { get; set; }
    }
}
