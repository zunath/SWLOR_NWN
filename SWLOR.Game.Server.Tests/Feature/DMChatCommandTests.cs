using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Feature;

public class DMChatCommandTests
{
    [Test]
    public void TeleportToTarget_AllowsStaffAndTestUsersToTargetCreaturesOrGround()
    {
        var commands = new DMChatCommand().BuildChatCommands();

        commands.Should().ContainKey("tpto");

        var command = commands["tpto"];
        command.Authorization.Should().Be(AuthorizationLevel.DM | AuthorizationLevel.Admin);
        command.AvailableToAllOnTestEnvironment.Should().BeTrue();
        command.RequiresTarget.Should().BeTrue();
        command.ValidTargetTypes.Should().Be(ObjectType.Creature | ObjectType.Tile);
        command.AllowsLocationTarget.Should().BeTrue();
    }

    [Test]
    public void LocationTargeting_IsOptInForChatCommands()
    {
        var root = RepoPaths.FindRepositoryRoot();
        var chatCommandSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "ChatCommand.cs"));
        var targetingSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Targeting.cs"));
        var dmChatCommandSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "ChatCommandDefinition",
            "DMChatCommand.cs"));

        chatCommandSource.Should().Contain("chatCommand.AllowsLocationTarget");
        targetingSource.Should().Contain("targetingAction.AllowsLocationTarget && targetedLocation != Vector3()");
        targetingSource.Should().Contain("targetingAction.SelectionAction(OBJECT_INVALID);");
        dmChatCommandSource.Should().Contain("_builder.Create(\"tpto\")");
        dmChatCommandSource.Should().Contain(".RequiresTarget(ObjectType.Creature | ObjectType.Tile)");
        dmChatCommandSource.Should().Contain(".AllowsLocationTarget()");
        dmChatCommandSource.Should().Contain("ActionJumpToLocation(location)");
    }

}
