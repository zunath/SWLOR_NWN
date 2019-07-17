

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCCooldown]")]
    public class PCCooldown: IEntity
    {
        public PCCooldown()
        {
            ID = Guid.NewGuid();
        }
        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public int CooldownCategoryID { get; set; }
        public DateTime DateUnlocked { get; set; }

        public IEntity Clone()
        {
            return new PCCooldown
            {
                ID = ID,
                PlayerID = PlayerID,
                CooldownCategoryID = CooldownCategoryID,
                DateUnlocked = DateUnlocked
            };
        }
    }
}
