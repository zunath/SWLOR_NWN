using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Communication.Service;
using SWLOR.Component.Communication.UI.Payload;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Domain.Admin.Enums;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Communication.ValueObjects;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Component.Communication.Definitions.ChatCommandDefinition
{
    public class RenameChatCommand : IChatCommandListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private IGuiService GuiService => _serviceProvider.GetRequiredService<IGuiService>();

        public RenameChatCommand(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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
                    GuiService.TogglePlayerWindow(user, GuiWindowType.RenameItem, payload);
                });

            return builder.Build();
        }
    }
}
