using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("Attributes")]
    public class Attribute: IEntity, ICacheable
    {
        public Attribute()
        {
            Name = "";
        }

        [ExplicitKey]
        public int AttributeID { get; set; }
        public int NWNValue { get; set; }
        public string Name { get; set; }
    }
}
