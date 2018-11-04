
using System;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCCraftedBlueprints]")]
    public class PCCraftedBlueprint: IEntity
    {
        [Key]
        public int PCCraftedBlueprintID { get; set; }
        public string PlayerID { get; set; }
        public long CraftBlueprintID { get; set; }
        public DateTime DateFirstCrafted { get; set; }
    }
}
