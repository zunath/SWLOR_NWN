

using System;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PCRegionalFame: IEntity
    {
        public PCRegionalFame()
        {
            ID = Guid.NewGuid();
        }
        [Key]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public FameRegion FameRegionID { get; set; }
        public int Amount { get; set; }
    }
}
