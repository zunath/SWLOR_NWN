
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[KeyItems]")]
    public class KeyItem: IEntity
    {
        [ExplicitKey]
        public int KeyItemID { get; set; }
        public int KeyItemCategoryID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
