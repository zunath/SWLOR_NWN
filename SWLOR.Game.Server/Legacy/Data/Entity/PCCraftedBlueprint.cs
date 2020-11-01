using System;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Legacy.Data.Contracts;

namespace SWLOR.Game.Server.Legacy.Data.Entity
{
    [Table("PCCraftedBlueprint")]
    public class PCCraftedBlueprint: IEntity
    {
        public PCCraftedBlueprint()
        {
            ID = Guid.NewGuid();
        }
        [ExplicitKey]
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
