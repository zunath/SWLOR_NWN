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
                .Description("Opens your Huttball window. Staff get the match controls; /huttball player opens the player window " +
                             "and /huttball captain the captain's window.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    var option = args.Length > 0 ? args[0].ToLowerInvariant() : string.Empty;
                    var wantsPlayerWindow = option == "player";

                    // DMs keep the window on whatever they possess, so they can run the match through a creature.
                    if (GetIsDM(DMPresence.GetDM(user)) && !wantsPlayerWindow)
                    {
                        DMPresence.ToggleWindow(user, GuiWindowType.Huttball);
                        return;
                    }

                    if (option == "captain" && !Huttball.IsStaff(user))
                    {
                        var match = Huttball.GetMatch(GetArea(user));
                        if (match != null && !match.IsCaptain(user))
                        {
                            SendMessageToPC(user, ColorToken.Red("Only a team captain can open the captain's window."));
                            return;
                        }

                        Gui.TogglePlayerWindow(user, GuiWindowType.Huttball);
                        return;
                    }

                    var window = Huttball.IsStaff(user) && !wantsPlayerWindow
                        ? GuiWindowType.Huttball
                        : GuiWindowType.HuttballPlayer;
                    Gui.TogglePlayerWindow(user, window);
                });

            return builder.Build();
        }
    }
}
