
using System;
using System.Collections.Generic;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[ComponentType]")]
    public class ComponentType: IEntity
    {
        public ComponentType()
        {
            Name = "";
        }

        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
    
    }
}
