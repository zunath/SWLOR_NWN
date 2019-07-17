
using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[SpaceStarport]")]
    public class SpaceStarport: IEntity
    {
        [ExplicitKey]
        public Guid ID { get; set; }
        public string Planet { get; set; }
        public int CustomsDC { get; set; }
        public int Cost { get; set; }
        public string Name { get; set; }
        public string Waypoint { get; set; }

        public IEntity Clone()
        {
            return new SpaceStarport
            {
                ID = ID,
                Planet = Planet,
                CustomsDC = CustomsDC,
                Cost = Cost,
                Name = Name,
                Waypoint = Waypoint
            };
        }
    }
}
