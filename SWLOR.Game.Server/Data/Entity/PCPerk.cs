
using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCPerk]")]
    public class PCPerk: IEntity
    {
        public PCPerk()
        {
            ID = Guid.NewGuid();
        }
        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public DateTime AcquiredDate { get; set; }
        public int PerkID { get; set; }
        public int PerkLevel { get; set; }

        public IEntity Clone()
        {
            return new PCPerk
            {
                ID = ID,
                PlayerID = PlayerID,
                AcquiredDate = AcquiredDate,
                PerkID = PerkID,
                PerkLevel = PerkLevel
            };
        }
    }
}
