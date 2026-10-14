using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class AfkChatCommand : IChatCommandListDefinition
    {
        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            return new ChatCommandBuilder()
                .Create("afk")
                .Description("Toggles a purple [AFK] indicator on your displayed name. Use /afk again when you return.")
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) => !GetIsPC(user) || GetIsDM(user)
                    ? "This command can only be used on player characters."
                    : string.Empty)
                .Action((user, target, location, args) =>
                {
                    var isAfk = PlayerName.ToggleAfk(user);
                    SendMessageToPC(user, isAfk
                        ? ColorToken.Purple("You are now AFK. Use /afk again when you return.")
                        : "You are no longer AFK.");
                })
                .Build();
        }
    }
}
