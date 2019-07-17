
using System;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCKeyItem]")]
    public class PCKeyItem: IEntity
    {
        public PCKeyItem()
        {
            ID = Guid.NewGuid();
        }
        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public int KeyItemID { get; set; }
        public DateTime AcquiredDate { get; set; }

        public IEntity Clone()
        {
            return new PCKeyItem
            {
                ID = ID,
                PlayerID = PlayerID,
                KeyItemID = KeyItemID,
                AcquiredDate = AcquiredDate
            };
        }
    }
}
