

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCRegionalFame]")]
    public class PCRegionalFame: IEntity
    {
        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public int FameRegionID { get; set; }
        public int Amount { get; set; }
    }
}
