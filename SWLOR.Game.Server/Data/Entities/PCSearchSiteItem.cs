using System.ComponentModel.DataAnnotations;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class PCSearchSiteItem
    {
        public long PCSearchSiteItemID { get; set; }

        [Required]
        [StringLength(60)]
        public string PlayerID { get; set; }

        public int SearchSiteID { get; set; }

        [Required]
        public string SearchItem { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
