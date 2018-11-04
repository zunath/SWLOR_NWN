
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCPerkRefunds]")]
    public class PCPerkRefund: IEntity
    {
        [Key]
        public int PCPerkRefundID { get; set; }
        public string PlayerID { get; set; }
        public int PerkID { get; set; }
        public int Level { get; set; }
        public System.DateTime DateAcquired { get; set; }
        public System.DateTime DateRefunded { get; set; }
    }
}
