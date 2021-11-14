using System.Collections.Generic;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class PropertyPermissionPayload: GuiPayloadBase
    {
        public string PropertyId { get; set; }
        public List<PropertyPermissionType> AvailablePermissions { get; set; }
    }
}
