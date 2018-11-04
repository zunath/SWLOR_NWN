using System.Collections.Generic;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[BuildingTypes]")]
    public class BuildingType: IEntity
    {
        [ExplicitKey]
        public int BuildingTypeID { get; set; }
        public string Name { get; set; }
   
    }
}
