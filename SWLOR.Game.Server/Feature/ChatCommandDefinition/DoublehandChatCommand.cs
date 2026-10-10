using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.ChatCommandService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition;

public class DoublehandChatCommand : IChatCommandListDefinition
{
    public Dictionary<string, ChatCommandDetail> BuildChatCommands() => new ChatCommandBuilder()
        .Create("stance")
        .Description("Toggle full combat style: blades to two hands; staffs, spears and double blades to one hand. Requires an empty offhand.")
        .Permissions(AuthorizationLevel.All)
        .Action((user, target, location, args) => DoublehandStance.Toggle(user))
        .Build();
}
