using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Messaging.Messages
{
    public class ChatProcessedMessage
    {
        public NWObject Sender { get; set; }
        public ChatChannelType Channel { get; set; }
        public bool IsOutOfCharacter { get; set; }

        public ChatProcessedMessage(NWObject sender, ChatChannelType channel, bool isOutOfCharacter)
        {
            Sender = sender;
            Channel = channel;
            IsOutOfCharacter = isOutOfCharacter;
        }
    }
}
