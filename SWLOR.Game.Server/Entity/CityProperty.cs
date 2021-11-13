using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Entity
{
    public class CityProperty: PropertyBase
    {
        public CityProperty()
        {
            Permissions = new Dictionary<string, CityPermission>();
        }

        public Dictionary<string, CityPermission> Permissions { get; set; }
    }
}
