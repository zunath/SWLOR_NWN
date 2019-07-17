

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCPerkRefund]")]
    public class PCPerkRefund: IEntity
    {
        public PCPerkRefund()
        {
            ID = Guid.NewGuid();
        }
        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public int PerkID { get; set; }
        public int Level { get; set; }
        public DateTime DateAcquired { get; set; }
        public DateTime DateRefunded { get; set; }

        public IEntity Clone()
        {
            return new PCPerkRefund
            {
                ID = ID,
                PlayerID = PlayerID,
                PerkID = PerkID,
                Level = Level,
                DateAcquired = DateAcquired,
                DateRefunded = DateRefunded
            };
        }
    }
}
