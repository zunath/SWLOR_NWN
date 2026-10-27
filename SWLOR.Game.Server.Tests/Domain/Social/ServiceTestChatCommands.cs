using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Social;

/// <summary>
/// Chat commands for <see cref="ChatCommandServiceTests"/>. The service finds every <c>IChatCommandListDefinition</c> in
/// the loaded assemblies, so this test assembly's commands load beside the server's own, and every command records
/// that it ran in <see cref="Calls"/>.
/// </summary>
public sealed class ServiceTestChatCommands : IChatCommandListDefinition
{
    /// <summary>The commands that ran: the command name, the user, the target and the arguments.</summary>
    public static readonly List<(string Command, uint User, uint Target, string[] Args)> Calls = new();

    public Dictionary<string, ChatCommandDetail> BuildChatCommands()
    {
        var builder = new ChatCommandBuilder();

        builder.Create("svcplain", "svcalias")
            .Description("A plain command.")
            .Permissions(AuthorizationLevel.Admin)
            .Action((user, target, location, args) => Calls.Add(("svcplain", user, target, args)));

        builder.Create("svcchecked")
            .Description("A command that needs an argument.")
            .Permissions(AuthorizationLevel.All)
            .Validate((user, args) => args.Length == 0 ? "Give an argument." : null)
            .Action((user, target, location, args) => Calls.Add(("svcchecked", user, target, args)));

        builder.Create("svcplayeronly")
            .Description("A command for players.")
            .Permissions(AuthorizationLevel.Player)
            .Action((user, target, location, args) => Calls.Add(("svcplayeronly", user, target, args)));

        builder.Create("svcdmonly")
            .Description("A command for DMs.")
            .Permissions(AuthorizationLevel.DM)
            .Action((user, target, location, args) => Calls.Add(("svcdmonly", user, target, args)));

        builder.Create("svcemote")
            .Description("An emote.")
            .Permissions(AuthorizationLevel.All)
            .IsEmote()
            .AnimationAction(Animation.Salute);

        builder.Create("svcadminemote")
            .Description("An emote for admins.")
            .Permissions(AuthorizationLevel.Admin)
            .IsEmote()
            .AnimationLoopingAction(Animation.LoopingMeditate);

        builder.Create("svctarget")
            .Description("A command that needs a target.")
            .Permissions(AuthorizationLevel.Admin)
            .RequiresTarget(ObjectType.Creature)
            .AllowsLocationTarget()
            .Validate((user, args) => args.Length > 1 ? "Too many arguments." : null)
            .Action((user, target, location, args) => Calls.Add(("svctarget", user, target, args)));

        builder.Create("svctargetdmonly")
            .Description("A targeted command for DMs.")
            .Permissions(AuthorizationLevel.DM)
            .RequiresTarget(ObjectType.Creature)
            .Action((user, target, location, args) => Calls.Add(("svctargetdmonly", user, target, args)));

        return builder.Build();
    }
}
