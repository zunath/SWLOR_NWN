using System.ComponentModel.DataAnnotations;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class PCCorpseItem
    {
        public long PCCorpseItemID { get; set; }

        public long PCCorpseID { get; set; }

        [Required]
        public string NWItemObject { get; set; }

        [Required]
        public string ItemName { get; set; }

        [Required]
        public string ItemTag { get; set; }

        [Required]
        public string ItemResref { get; set; }

        [Required]
        public string GlobalID { get; set; }

        public virtual PCCorpse PcCorpse { get; set; }
    }
}
