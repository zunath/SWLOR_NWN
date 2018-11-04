
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[Spawns]")]
    public class Spawn: IEntity
    {
        [ExplicitKey]
        public int SpawnID { get; set; }
        public string Name { get; set; }
        public int SpawnObjectTypeID { get; set; }
    }
}
