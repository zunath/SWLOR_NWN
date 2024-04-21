using SWLOR.Core.Service.GuiService;

namespace SWLOR.Core.Feature.GuiDefinition.Payload
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
