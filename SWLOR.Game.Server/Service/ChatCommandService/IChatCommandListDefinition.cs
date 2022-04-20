using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.ChatCommandService
{
    public interface IChatCommandListDefinition
    {
        public Dictionary<string, ChatCommandDetail> BuildChatCommands();
    }
}
