
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCCraftedBlueprints")]
    public partial class PCCraftedBlueprint: IEntity
    {
        [Key]
        public int PCCraftedBlueprintID { get; set; }
        public string PlayerID { get; set; }
        public long CraftBlueprintID { get; set; }
        public System.DateTime DateFirstCrafted { get; set; }
    
        public virtual CraftBlueprint CraftBlueprint { get; set; }
        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
