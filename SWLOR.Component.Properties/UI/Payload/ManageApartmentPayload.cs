using SWLOR.Shared.UI.Model;

namespace SWLOR.Component.Properties.UI.Payload
{
    public class ManageApartmentPayload: GuiPayloadBase
    {
        public string SpecificPropertyId { get; set; }

        public ManageApartmentPayload(string specificPropertyId)
        {
            SpecificPropertyId = specificPropertyId;
        }
    }
}
