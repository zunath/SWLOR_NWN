
using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PCBaseStructureItem: IEntity
    {
        public PCBaseStructureItem()
        {
            ID = Guid.NewGuid();
        }
        [Key]
        public Guid ID { get; set; }
        public Guid PCBaseStructureID { get; set; }
        public string ItemGlobalID { get; set; }
        public string ItemName { get; set; }
        public string ItemTag { get; set; }
        public string ItemResref { get; set; }
        public string ItemObject { get; set; }
    }
}
