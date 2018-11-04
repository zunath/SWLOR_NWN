
using System.Collections.Generic;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[KeyItemCategories]")]
    public class KeyItemCategory: IEntity
    {
        [ExplicitKey]
        public int KeyItemCategoryID { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }
}
