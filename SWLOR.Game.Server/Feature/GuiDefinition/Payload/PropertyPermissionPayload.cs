using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class PropertyPermissionPayload: GuiPayloadBase
    {
        public PropertyType PropertyType { get; set; }
        public string PropertyId { get; set; }
        public bool IsCategory { get; set; }
        public string CityId { get; set; }

        public PropertyPermissionPayload(
            PropertyType propertyType, 
            string propertyId, 
            string cityId,
            bool isCategory)
        {
            PropertyType = propertyType;
            PropertyId = propertyId;
            CityId = cityId;
            IsCategory = isCategory;
        }

    }
}
