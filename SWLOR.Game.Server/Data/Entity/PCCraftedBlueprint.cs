
using System;

using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PCCraftedBlueprint: IEntity
    {
        public PCCraftedBlueprint()
        {
            ID = Guid.NewGuid();
        }
        [Key]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public CraftBlueprint CraftBlueprintID { get; set; }
        public DateTime DateFirstCrafted { get; set; }
    }
}
