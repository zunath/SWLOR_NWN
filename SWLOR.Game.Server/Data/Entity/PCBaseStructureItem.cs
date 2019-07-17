
using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCBaseStructureItem]")]
    public class PCBaseStructureItem: IEntity
    {
        public PCBaseStructureItem()
        {
            ID = Guid.NewGuid();
        }
        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PCBaseStructureID { get; set; }
        public string ItemGlobalID { get; set; }
        public string ItemName { get; set; }
        public string ItemTag { get; set; }
        public string ItemResref { get; set; }
        public string ItemObject { get; set; }

        public IEntity Clone()
        {
            return new PCBaseStructureItem
            {
                ID = ID,
                PCBaseStructureID = PCBaseStructureID,
                ItemGlobalID = ItemGlobalID,
                ItemName = ItemName,
                ItemTag = ItemTag,
                ItemResref = ItemResref,
                ItemObject = ItemObject
            };
        }
    }
}
