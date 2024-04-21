namespace SWLOR.Core.Service.ChatCommandService
{
    public interface IChatCommandListDefinition
    {
        public Dictionary<string, ChatCommandDetail> BuildChatCommands();
    }
}
