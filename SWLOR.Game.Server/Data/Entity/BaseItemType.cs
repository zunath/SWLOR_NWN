using System.Collections.Generic;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[BaseItemTypes]")]
    public class BaseItemType: IEntity
    {
        [ExplicitKey]
        public int BaseItemTypeID { get; set; }
        public string Name { get; set; }
    }
}
