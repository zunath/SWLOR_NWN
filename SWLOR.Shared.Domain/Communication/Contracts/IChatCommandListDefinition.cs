using SWLOR.Shared.Domain.Communication.ValueObjects;

namespace SWLOR.Shared.Domain.Communication.Contracts
{
    public interface IChatCommandListDefinition
    {
        public Dictionary<string, ChatCommandDetail> BuildChatCommands();
    }
}
