
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[NPCGroups]")]
    public class NPCGroup: IEntity
    {
        [ExplicitKey]
        public int NPCGroupID { get; set; }
        public string Name { get; set; }
    }
}
