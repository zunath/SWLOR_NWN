using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.ProspectingService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    /// <summary>Opens the Prospecting window on the session a harvester began.</summary>
    public class ProspectingPayload : GuiPayloadBase
    {
        public ProspectingSession Session { get; }

        public ProspectingPayload(ProspectingSession session)
        {
            Session = session;
        }
    }
}
