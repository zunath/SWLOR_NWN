using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Entity
{
    public abstract class PropertyBase: EntityBase
    {
        protected PropertyBase()
        {
            ParentPropertyId = string.Empty;
            ChildPropertyIds = new List<string>();
            Permissions = new Dictionary<string, Dictionary<PropertyPermissionType, bool>>();
        }

        [Indexed]
        public string CustomName { get; set; }

        [Indexed]
        public PropertyType PropertyType { get; set; }

        [Indexed]
        public string OwnerPlayerId { get; set; }

        [Indexed]
        public string ParentPropertyId { get; set; }

        public List<string> ChildPropertyIds { get; set; }

        public Dictionary<string, Dictionary<PropertyPermissionType, bool>> Permissions { get; set; }

        public abstract void SpawnIntoWorld(uint area);

        public bool IsPubliclyAccessible { get; set; }

        public PropertyLayoutType InteriorLayout { get; set; }
    }
}
