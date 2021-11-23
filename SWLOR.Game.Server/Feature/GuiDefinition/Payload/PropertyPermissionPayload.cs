using System.Collections.Generic;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class PropertyPermissionPayload: GuiPayloadBase
    {
        public PropertyType PropertyType { get; set; }
        public string PropertyId { get; set; }
        public List<PropertyPermissionType> AvailablePermissions { get; set; }
        public bool IsCategory { get; set; }

        public PropertyPermissionPayload(
            PropertyType propertyType, 
            string propertyId, 
            bool isCategory, 
            List<PropertyPermissionType> availablePermissions)
        {
            PropertyType = propertyType;
            PropertyId = propertyId;
            AvailablePermissions = availablePermissions;
            IsCategory = isCategory;
        }

    }
}
