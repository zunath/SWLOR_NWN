using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.ConversationService
{
    public sealed class ConversationContext
    {
        public uint Player { get; }
        public uint Owner { get; }
        public Dictionary<string, string> Tokens { get; } = new();
        public Dictionary<string, object> State { get; } = new();

        public ConversationContext(uint player, uint owner)
        {
            Player = player;
            Owner = owner;
        }
    }
}
