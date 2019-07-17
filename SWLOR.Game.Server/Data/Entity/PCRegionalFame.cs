

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCRegionalFame]")]
    public class PCRegionalFame: IEntity
    {
        public PCRegionalFame()
        {
            ID = Guid.NewGuid();
        }
        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public int FameRegionID { get; set; }
        public int Amount { get; set; }

        public IEntity Clone()
        {
            return new PCRegionalFame
            {
                ID = ID,
                PlayerID = PlayerID,
                FameRegionID = FameRegionID,
                Amount = Amount
            };
        }
    }
}
