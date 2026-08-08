using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public sealed class ConversationPayload : GuiPayloadBase
    {
        public uint ControllerPlayer { get; }
        public IConversationSession Session { get; }

        public ConversationPayload(uint controllerPlayer, IConversationSession session)
        {
            ControllerPlayer = controllerPlayer;
            Session = session ?? throw new ArgumentNullException(nameof(session));
        }
    }
}
