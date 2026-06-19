using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class PlayerNameChatCommandTests
{
    [Test]
    public void NameCommands_AreAvailableToStaffAuthorizedPlayerAccounts()
    {
        var commands = new CharacterChatCommand().BuildChatCommands();

        commands["name"].Authorization.Should().Be(AuthorizationLevel.All);
        commands["name"].RequiresTarget.Should().BeTrue();
        commands["name"].ValidTargetTypes.Should().Be(ObjectType.Creature);

        commands["forgetname"].Authorization.Should().Be(AuthorizationLevel.All);
        commands["forgetname"].RequiresTarget.Should().BeTrue();
        commands["forgetname"].ValidTargetTypes.Should().Be(ObjectType.Creature);
    }
}
