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

    [Test]
    public void NameCommands_AuditIdentityMutations()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "ChatCommandDefinition",
            "CharacterChatCommand.cs"));

        source.Should().Contain("PlayerName.SetKnownName(user, target, name);");
        source.Should().Contain("PlayerName.ForgetKnownName(user, target);");
        source.Should().Contain("Log.WriteStructured(");
        source.Should().Contain("LogGroup.PlayerName");
        source.Should().Contain("\"name-set\"");
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
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "ChatCommandDefinition",
            "CharacterChatCommand.cs"));

        source.Should().Contain("PlayerName.ValidateKnownName(name)");
        source.Should().Contain("SendMessageToPC(user, ColorToken.Red(validationError));");

        source.Should().Contain("if (!GetIsObjectValid(target) || !GetIsPC(target) || GetIsDM(target))");
        source.Should().Contain("You may only name player characters.");
        source.Should().Contain("You may only forget names for player characters.");

        source.Should().Contain("if (target == user)");
        source.Should().Contain("You already know your own name.");
        source.Should().Contain("You cannot forget your own name.");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
