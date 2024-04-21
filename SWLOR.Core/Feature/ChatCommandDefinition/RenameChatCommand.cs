using SWLOR.Core.Enumeration;
using SWLOR.Core.Feature.GuiDefinition.Payload;
using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.Service;
using SWLOR.Core.Service.ChatCommandService;
using SWLOR.Core.Service.GuiService;

namespace SWLOR.Core.Feature.ChatCommandDefinition
{
    public class RenameChatCommand : IChatCommandListDefinition
    {
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
                    Gui.TogglePlayerWindow(user, GuiWindowType.RenameItem, payload);
                });

            return builder.Build();
        }
    }
}
