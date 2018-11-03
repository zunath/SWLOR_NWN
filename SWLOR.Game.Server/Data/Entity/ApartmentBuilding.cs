using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("ApartmentBuildings")]
    public class ApartmentBuilding: IEntity, ICacheable
    {
        [ExplicitKey]
        public int ApartmentBuildingID { get; set; }
        public string Name { get; set; }
    }
}
