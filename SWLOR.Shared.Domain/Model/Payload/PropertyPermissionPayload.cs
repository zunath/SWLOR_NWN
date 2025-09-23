using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Shared.Domain.Model.Payload
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
