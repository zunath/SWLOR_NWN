using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    [Table("PCRegionalFame")]
    public partial class PCRegionalFame
    {
        public int PCRegionalFameID { get; set; }

        [Required]
        [StringLength(60)]
        public string PlayerID { get; set; }

        public int FameRegionID { get; set; }

        public int Amount { get; set; }

        public virtual FameRegion FameRegion { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
