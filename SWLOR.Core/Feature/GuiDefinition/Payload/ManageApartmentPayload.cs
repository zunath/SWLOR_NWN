using SWLOR.Core.Service.GuiService;

namespace SWLOR.Core.Feature.GuiDefinition.Payload
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
