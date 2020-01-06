
using System;

using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PCKeyItem: IEntity
    {
        public PCKeyItem()
        {
            ID = Guid.NewGuid();
        }
        [Key]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public KeyItem KeyItemID { get; set; }
        public DateTime AcquiredDate { get; set; }
    }
}
