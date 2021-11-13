using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Entity
{
    public class BuildingProperty: PropertyBase, IPositionedProperty
    {
        public BuildingProperty()
        {
            Permissions = new Dictionary<string, BuildingPermission>();
        }

        public string AreaResref { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Orientation { get; set; }

        public Dictionary<string, BuildingPermission> Permissions { get; set; }

    }
}
