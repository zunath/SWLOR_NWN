
using System.Collections.Generic;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[ComponentTypes]")]
    public class ComponentType: IEntity
    {
        public ComponentType()
        {
            Name = "";
        }

        [ExplicitKey]
        public int ComponentTypeID { get; set; }
        public string Name { get; set; }
    
    }
}
