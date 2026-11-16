using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class InitiativeTrackerChatCommand : IChatCommandListDefinition
    {
        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            var builder = new ChatCommandBuilder();

            builder.Create("tracker")
                .Description("Opens the initiative tracker for the area you are in.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    InitiativeTracker.ToggleWindow(user, GuiWindowType.InitiativeTracker);
                });

            return builder.Build();
        }
    }
}
