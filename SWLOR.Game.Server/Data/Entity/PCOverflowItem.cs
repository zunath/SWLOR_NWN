

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCOverflowItem]")]
    public class PCOverflowItem: IEntity
    {
        public PCOverflowItem()
        {
            ID = Guid.NewGuid();
        }
        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public string ItemName { get; set; }
        public string ItemTag { get; set; }
        public string ItemResref { get; set; }
        public string ItemObject { get; set; }

        public IEntity Clone()
        {
            return new PCOverflowItem
            {
                ID = ID,
                PlayerID = PlayerID,
                ItemName = ItemName,
                ItemTag = ItemTag,
                ItemResref = ItemResref,
                ItemObject = ItemObject
            };
        }
    }
}
