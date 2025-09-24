using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Properties.Enums;

namespace SWLOR.Shared.Domain.UI.Payloads
{
    public class PropertyPermissionPayload: IGuiPayload
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
