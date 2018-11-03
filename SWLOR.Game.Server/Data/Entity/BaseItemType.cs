using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("BaseItemTypes")]
    public class BaseItemType: IEntity, ICacheable
    {
        [ExplicitKey]
        public int BaseItemTypeID { get; set; }
        public string Name { get; set; }
    }
}
