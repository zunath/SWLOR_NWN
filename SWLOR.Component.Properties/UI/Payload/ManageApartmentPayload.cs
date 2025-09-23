using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Properties.UI.Payload
{
    public class ManageApartmentPayload: IGuiPayload
    {
        public string SpecificPropertyId { get; set; }

        public ManageApartmentPayload(string specificPropertyId)
        {
            SpecificPropertyId = specificPropertyId;
        }
    }
}
