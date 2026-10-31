using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class PazaakChatCommand : IChatCommandListDefinition
    {
        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            var builder = new ChatCommandBuilder();

            builder.Create("pazaak")
                .Description("Opens the Pazaak table, where you can build your side deck and challenge a nearby player.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    if (GetIsDM(user) || GetIsDMPossessed(user))
                    {
                        SendMessageToPC(user, ColorToken.Red("Pazaak is only available to player characters."));
                        return;
                    }

                    Gui.TogglePlayerWindow(user, GuiWindowType.Pazaak);
                });

            return builder.Build();
        }
    }
}
