using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class IncubatorPayload: GuiPayloadBase
    {
        public string PropertyId { get; set; }

        public IncubatorPayload(string propertyId)
        {
            PropertyId = propertyId;
        }
    }
}
