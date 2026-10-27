using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class CustomsScanPayload : GuiPayloadBase
    {
        /// <summary>The scan the window answers; a stale window cannot answer a newer scan.</summary>
        public string ScanId { get; set; }

        public CustomsScanPayload(string scanId)
        {
            ScanId = scanId;
        }
    }
}
