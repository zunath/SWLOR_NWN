using System;
using System.ComponentModel.DataAnnotations;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class PCSearchSite
    {
        public int PCSearchSiteID { get; set; }

        [Required]
        [StringLength(60)]
        public string PlayerID { get; set; }

        public int SearchSiteID { get; set; }

        public DateTime UnlockDateTime { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
