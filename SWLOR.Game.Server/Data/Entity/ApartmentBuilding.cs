
using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[ApartmentBuildings]")]
    public class ApartmentBuilding: IEntity
    {
        [ExplicitKey]
        public Guid ApartmentBuildingID { get; set; }
        public string Name { get; set; }
    }
}
