using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class PlanterPayload: GuiPayloadBase
    {
        public string PropertyId { get; set; }
        public string FarmingJobId { get; set; }

        public PlanterPayload(string propertyId, string farmingJobId)
        {
            PropertyId = propertyId;
            FarmingJobId = farmingJobId;
        }
    }
}
