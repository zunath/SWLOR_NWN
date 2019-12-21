
using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
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
