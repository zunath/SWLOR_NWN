using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Crafting;

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
