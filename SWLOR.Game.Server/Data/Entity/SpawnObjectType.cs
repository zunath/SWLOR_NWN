
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[SpawnObjectType]")]
    public class SpawnObjectType: IEntity
    {
        [ExplicitKey]
        public int SpawnObjectTypeID { get; set; }
        public string Name { get; set; }
    }
}
