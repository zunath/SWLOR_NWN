
using System;

using SWLOR.Game.Server.Data.Contracts;

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
        public int CraftBlueprintID { get; set; }
        public DateTime DateFirstCrafted { get; set; }

        public IEntity Clone()
        {
            return new PCCraftedBlueprint
            {
                ID = ID,
                PlayerID = PlayerID,
                CraftBlueprintID = CraftBlueprintID,
                DateFirstCrafted = DateFirstCrafted
            };
        }
    }
}
