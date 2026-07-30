using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Tests.Support;

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

    [Test]
    public void NameCommands_AuditIdentityMutations()
    {
        var root = RepoPaths.FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "ChatCommandDefinition",
            "CharacterChatCommand.cs"));

        source.Should().Contain("PlayerName.SetKnownName(user, target, name);");
        source.Should().Contain("PlayerDescriptor.SetUnknownDisplayName(user, name);");
        source.Should().Contain("PlayerName.ForgetKnownName(user, target);");
        source.Should().Contain("Log.WriteStructured(");
        source.Should().Contain("LogGroup.PlayerName");
        source.Should().Contain("\"name-set\"");
        source.Should().Contain("\"unknown-name-set\"");
        source.Should().Contain("\"name-forget\"");
        source.Should().Contain("ObserverPlayerId={ObserverPlayerId}");
        source.Should().Contain("TargetPlayerId={TargetPlayerId}");
        source.Should().Contain("Name={Name}");
        source.Should().NotContain("LogGroup.Chat,\r\n                        \"Player identity name change");
        source.Should().NotContain("LogGroup.Chat,\n                        \"Player identity name change");
    }

    [Test]
    public void NameCommands_DefinePlayerTargetSelfTargetAndInvalidNameGuardrails()
    {
        var root = RepoPaths.FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "ChatCommandDefinition",
            "CharacterChatCommand.cs"));

        source.Should().Contain("PlayerName.ValidateKnownNameInput(rawName)");
        source.Should().Contain("SendMessageToPC(user, ColorToken.Red(validationError));");
        source.Should().Contain("PlayerName.ValidateKnownNameAssignment(user, target, name)");
        source.Should().Contain("PlayerDescriptor.SetUnknownDisplayName(user, name);");
        source.Should().NotContain("catch (ArgumentException ex)");
        source.Should().NotContain("ColorToken.Red(ex.Message)");

        source.Should().Contain("if (!GetIsObjectValid(target) || !GetIsPC(target) || GetIsDM(target))");
        source.Should().Contain("You may only name player characters.");
        source.Should().Contain("You may only forget names for player characters.");

        source.Should().Contain("if (target == user)");
        source.Should().Contain("Players who have not named you will now see you as");
        source.Should().NotContain("You already know your own name.");
        source.Should().Contain("You cannot forget your own name.");
    }

}
