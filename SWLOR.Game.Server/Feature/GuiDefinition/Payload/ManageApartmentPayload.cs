using SWLOR.Shared.UI.Model;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
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
