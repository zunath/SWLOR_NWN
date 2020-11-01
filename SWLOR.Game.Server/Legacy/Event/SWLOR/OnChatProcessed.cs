using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Event.SWLOR
{
    public class OnChatProcessed
    {
        public NWObject Sender { get; set; }
        public ChatChannel Channel { get; set; }
        public bool IsOutOfCharacter { get; set; }

        public OnChatProcessed(NWObject sender, ChatChannel channel, bool isOutOfCharacter)
        {
            Sender = sender;
            Channel = channel;
            IsOutOfCharacter = isOutOfCharacter;
        }
    }
}
