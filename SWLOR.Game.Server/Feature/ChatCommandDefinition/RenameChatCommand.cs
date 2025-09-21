using System.Collections.Generic;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
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
