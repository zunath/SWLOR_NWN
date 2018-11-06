
using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCBaseStructureItems]")]
    public class PCBaseStructureItem: IEntity
    {
        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PCBaseStructureID { get; set; }
        public string ItemGlobalID { get; set; }
        public string ItemName { get; set; }
        public string ItemTag { get; set; }
        public string ItemResref { get; set; }
        public string ItemObject { get; set; }
    }
}
