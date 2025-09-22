using SWLOR.Component.Communication.Contracts;
using SWLOR.Component.Communication.Model;
using SWLOR.Component.Communication.Service;

namespace SWLOR.Component.Communication.Feature.ChatCommandDefinition
{
    public class DiceChatCommand: IChatCommandListDefinition
    {
        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            var builder = new ChatCommandBuilder();

            builder.Create("dice")
                .Description("Opens the dice bag menu.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    Shared.Dialog.Service.Dialog.StartConversation(user, user, nameof(DiceDialog));
                });

            return builder.Build();
        }
    }
}
