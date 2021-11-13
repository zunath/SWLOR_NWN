using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Entity
{
    public class StarshipProperty: PropertyBase, IPositionedProperty
    {
        public StarshipProperty()
        {
            Permissions = new Dictionary<string, StarshipPermission>();
        }

        public string AreaResref { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Orientation { get; set; }

        public Dictionary<string, StarshipPermission> Permissions { get; set; }
    }
}
