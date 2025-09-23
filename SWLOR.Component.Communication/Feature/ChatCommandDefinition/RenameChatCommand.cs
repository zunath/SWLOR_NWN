using SWLOR.Component.Communication.Contracts;
using SWLOR.Component.Communication.Model;
using SWLOR.Component.Communication.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Component.Communication.Feature.ChatCommandDefinition
{
    public class RenameChatCommand : IChatCommandListDefinition
    {
        private readonly IGuiService _guiService;

        public RenameChatCommand(IGuiService guiService)
        {
            _guiService = guiService;
        }

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            var builder = new ChatCommandBuilder();

            builder.Create("rename")
                .Description("Renames the target.")
                .Permissions(AuthorizationLevel.All)
                .RequiresTarget()
                .Action((user, target, location, args) =>
                {
                    var isDM = GetIsDM(user) || GetIsDMPossessed(user);

                    if (!isDM)
                    {
                        if (GetObjectType(target) != ObjectType.Item)
                        {
                            SendMessageToPC(user, "You can only rename items with this command.");
                            return;
                        }
                    }

                    if (GetIsDM(target))
                    {
                        SendMessageToPC(user, "DMs cannot be renamed.");
                        return;
                    }

                    var payload = new RenameItemPayload(target);
                    _guiService.TogglePlayerWindow(user, GuiWindowType.RenameItem, payload);
                });

            return builder.Build();
        }
    }
}
