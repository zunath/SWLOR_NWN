

using System;
using SWLOR.Game.Server.Data.Contracts;

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
