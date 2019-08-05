
using System;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCImpoundedItem]")]
    public class PCImpoundedItem: IEntity
    {
        public PCImpoundedItem()
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
        public DateTime DateImpounded { get; set; }
        public DateTime? DateRetrieved { get; set; }

        public IEntity Clone()
        {
            return new PCImpoundedItem
            {
                ID = ID,
                PlayerID = PlayerID,
                ItemName = ItemName,
                ItemTag = ItemTag,
                ItemResref = ItemResref,
                ItemObject = ItemObject,
                DateImpounded = DateImpounded,
                DateRetrieved = DateRetrieved
            };
        }
    }
}
