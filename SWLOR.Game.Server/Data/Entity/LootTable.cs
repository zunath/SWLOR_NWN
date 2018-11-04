
using System.Collections.Generic;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[LootTables]")]
    public class LootTable: IEntity
    {
        [ExplicitKey]
        public int LootTableID { get; set; }
        public string Name { get; set; }
    }
}
