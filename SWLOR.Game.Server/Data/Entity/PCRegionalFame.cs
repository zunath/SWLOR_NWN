
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCRegionalFame")]
    public class PCRegionalFame: IEntity
    {
        [Key]
        public int PCRegionalFameID { get; set; }
        public string PlayerID { get; set; }
        public int FameRegionID { get; set; }
        public int Amount { get; set; }
    }
}
