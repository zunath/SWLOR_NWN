using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class HuttballChatCommand : IChatCommandListDefinition
    {
        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            var builder = new ChatCommandBuilder();

            builder.Create("huttball")
                .Description("Opens the Huttball match panel. DMs run matches from it.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    // DMs keep the window on whatever they possess, so they can run the match through a creature.
                    if (GetIsDM(DMPresence.GetDM(user)))
                        DMPresence.ToggleWindow(user, GuiWindowType.Huttball);
                    else
                        Gui.TogglePlayerWindow(user, GuiWindowType.Huttball);
                });

            return builder.Build();
        }
    }
}
