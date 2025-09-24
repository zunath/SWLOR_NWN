using SWLOR.Component.Communication.Contracts;
using SWLOR.Component.Communication.Dialog;
using SWLOR.Component.Communication.Model;
using SWLOR.Component.Communication.Service;
using SWLOR.Shared.Dialog.Contracts;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Communication.Feature.ChatCommandDefinition
{
    public class DiceChatCommand: IChatCommandListDefinition
    {
        private readonly IDialogService _dialog;
        public DiceChatCommand(IDialogService dialog)
        {
            _dialog = dialog;
        }

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            var builder = new ChatCommandBuilder();

            builder.Create("dice")
                .Description("Opens the dice bag menu.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    _dialog.StartConversation(user, user, nameof(DiceDialog));
                });

            return builder.Build();
        }
    }
}
