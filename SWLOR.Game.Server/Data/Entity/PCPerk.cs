
using System;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PCPerk: IEntity
    {
        public PCPerk()
        {
            ID = Guid.NewGuid();
        }
        [Key]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public DateTime AcquiredDate { get; set; }
        public PerkType PerkID { get; set; }
        public int PerkLevel { get; set; }
    }
}
