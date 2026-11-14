using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Social
{
    public interface IChatCommandListDefinition
    {
        public Dictionary<string, ChatCommandDetail> BuildChatCommands();
    }
}
