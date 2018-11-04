
using System.Collections.Generic;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[CraftBlueprintCategories]")]
    public class CraftBlueprintCategory: IEntity
    {
        [ExplicitKey]
        public long CraftBlueprintCategoryID { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
    
    }
}
