using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.ScavengeService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    /// <summary>Opens the Search window on the visit a scavenge site started.</summary>
    public class SearchPayload : GuiPayloadBase
    {
        public ScavengeSession Session { get; }

        public SearchPayload(ScavengeSession session)
        {
            Session = session;
        }
    }
}
