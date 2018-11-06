

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCPerkRefunds]")]
    public class PCPerkRefund: IEntity
    {
        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public int PerkID { get; set; }
        public int Level { get; set; }
        public DateTime DateAcquired { get; set; }
        public DateTime DateRefunded { get; set; }
    }
}
