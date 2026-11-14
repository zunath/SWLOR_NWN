using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.ChatCommandService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition;

public class GripChatCommand : IChatCommandListDefinition
{
    public Dictionary<string, ChatCommandDetail> BuildChatCommands() => new ChatCommandBuilder()
        .Create("grip")
        .Description("Toggle two-handed combat animations for supported one-handed blades. Requires an empty offhand.")
        .Permissions(AuthorizationLevel.All)
        .Action((user, target, location, args) => Grip.Toggle(user))
        .Build();
}
