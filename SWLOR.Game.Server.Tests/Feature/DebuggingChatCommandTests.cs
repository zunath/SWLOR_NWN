using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;

namespace SWLOR.Game.Server.Tests.Feature;

public class DebuggingChatCommandTests
{
    [Test]
    public void EnmityDebugger_IsAvailableToPlayersOnlyOnTheTestServer()
    {
        var commands = new DebuggingChatCommand().BuildChatCommands();

        commands.Should().ContainKey("enmitydebugger");

        var command = commands["enmitydebugger"];
        command.Authorization.Should().Be(AuthorizationLevel.Admin);
        command.AvailableToAllOnTestEnvironment.Should().BeTrue();

        commands["objectid"].AvailableToAllOnTestEnvironment.Should().BeFalse();
        commands["resetbeast"].AvailableToAllOnTestEnvironment.Should().BeFalse();
    }
}
