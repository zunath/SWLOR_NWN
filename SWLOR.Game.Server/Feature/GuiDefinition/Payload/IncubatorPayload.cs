using SWLOR.Shared.UI.Model;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class IncubatorPayload: GuiPayloadBase
    {
        public string PropertyId { get; set; }
        public string IncubationJobId { get; set; }

        public IncubatorPayload(string propertyId, string incubationJobId)
        {
            PropertyId = propertyId;
            IncubationJobId = incubationJobId;
        }
    }
}
