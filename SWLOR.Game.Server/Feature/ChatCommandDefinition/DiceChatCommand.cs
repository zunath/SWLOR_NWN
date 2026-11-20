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
                    // A DM's window can still be open on the avatar or a creature they possessed before; DMPresence
                    // moves it to the creature they control now instead of rebinding it under the open copy.
                    if (DMPresence.IsControlledByDM(user))
                        DMPresence.ToggleWindow(user, GuiWindowType.Dice);
                    else
                        Gui.TogglePlayerWindow(user, GuiWindowType.Dice);
                });

            return builder.Build();
        }
    }
}
