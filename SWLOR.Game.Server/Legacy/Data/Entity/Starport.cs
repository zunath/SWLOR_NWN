using System;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Legacy.Data.Contracts;

namespace SWLOR.Game.Server.Legacy.Data.Entity
{
    [Table("Starport")]
    public class Starport: IEntity
    {
        [Key]
        public int ID { get; set; }
        public Guid StarportID { get; set; }
        public string PlanetName { get; set; }
        public string Name { get; set; }
        public int CustomsDC { get; set; }
        public int Cost { get; set; }
        public string WaypointTag { get; set; }

        public IEntity Clone()
        {
            return new Starport
            {
                ID = ID,
                StarportID = StarportID,
                PlanetName = PlanetName,
                Name = Name,
                CustomsDC = CustomsDC,
                Cost = Cost,
                WaypointTag = WaypointTag
            };
        }
    }
}
