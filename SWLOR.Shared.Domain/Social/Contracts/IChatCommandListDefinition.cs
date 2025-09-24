using SWLOR.Component.Communication.Model;

namespace SWLOR.Component.Communication.Contracts
{
    public interface IChatCommandListDefinition
    {
        public Dictionary<string, ChatCommandDetail> BuildChatCommands();
    }
}
