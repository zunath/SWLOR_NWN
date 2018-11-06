

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCCooldowns]")]
    public class PCCooldown: IEntity
    {
        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public int CooldownCategoryID { get; set; }
        public DateTime DateUnlocked { get; set; }
    }
}
