using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
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
                    Gui.TogglePlayerWindow(user, GuiWindowType.Dice);
                });

            return builder.Build();
        }
    }
}
