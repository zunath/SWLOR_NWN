using System.Collections.Generic;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[Associations]")]
    public class Association: IEntity
    {
        [ExplicitKey]
        public int AssociationID { get; set; }
        public string Name { get; set; }
    }
}
