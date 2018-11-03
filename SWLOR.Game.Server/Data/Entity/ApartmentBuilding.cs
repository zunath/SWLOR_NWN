using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data
{
    public partial class ApartmentBuilding: IEntity
    {
        [ExplicitKey]
        public int ApartmentBuildingID { get; set; }
        public string Name { get; set; }
    }
}
