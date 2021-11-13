using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Entity
{
    public class ApartmentProperty: PropertyBase
    {
        public ApartmentProperty()
        {
            Permissions = new Dictionary<string, ApartmentPermission>();
        }

        public Dictionary<string, ApartmentPermission> Permissions { get; set; }
    }
}
