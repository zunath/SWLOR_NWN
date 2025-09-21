using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Shared.Abstractions;

namespace SWLOR.Game.Server.Entity
{
    public class WorldPropertyPermission: EntityBase
    {
        public WorldPropertyPermission()
        {
            Permissions = new Dictionary<PropertyPermissionType, bool>();
            GrantPermissions = new Dictionary<PropertyPermissionType, bool>();
        }

        [Indexed]
        public string PropertyId { get; set; }
        [Indexed]
        public string PlayerId { get; set; }

        public Dictionary<PropertyPermissionType, bool> Permissions { get; set; }
        public Dictionary<PropertyPermissionType, bool> GrantPermissions { get; set; }
    }
}
