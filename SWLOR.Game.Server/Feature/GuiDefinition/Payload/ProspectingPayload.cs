using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Crafting;

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
