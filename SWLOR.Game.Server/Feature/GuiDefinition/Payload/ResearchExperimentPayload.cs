using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    /// <summary>Opens the Experiment window for a research job that has just started.</summary>
    public class ResearchExperimentPayload: GuiPayloadBase
    {
        public string JobId { get; set; }

        public ResearchExperimentPayload(string jobId)
        {
            JobId = jobId;
        }
    }
}
