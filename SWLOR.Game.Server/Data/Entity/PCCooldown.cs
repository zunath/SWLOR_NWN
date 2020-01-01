

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PCCooldown: IEntity
    {
        public PCCooldown()
        {
            ID = Guid.NewGuid();
        }
        [Key]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public int CooldownCategoryID { get; set; }
        public DateTime DateUnlocked { get; set; }
    }
}
