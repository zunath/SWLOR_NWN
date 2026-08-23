using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;
using SWLOR.NWN.API.NWScript.Enum;

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
    public void SpawnBeastEgg_AllowsStaffAndTestUsersToCreateTypedEggs()
    {
        var commands = new DMChatCommand().BuildChatCommands();

        commands.Should().ContainKey("spawnegg");

        var command = commands["spawnegg"];
        command.Authorization.Should().Be(AuthorizationLevel.DM | AuthorizationLevel.Admin);
        command.AvailableToAllOnTestEnvironment.Should().BeTrue();
        command.RequiresTarget.Should().BeFalse();
        command.Description.Should().Contain("/spawnegg help");
        command.ValidateArguments.Should().NotBeNull();
        command.DoAction.Should().NotBeNull();
        command.ValidateArguments(0, "help").Should().BeEmpty();
    }

    [Test]
    public void SpawnBeastEgg_HelpListsConfiguredTypesAndEggsReceiveTheDNATypeProperty()
    {
        var root = FindRepositoryRoot();
        var dmChatCommandSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "ChatCommandDefinition",
            "DMChatCommand.cs"));
        var beastMasterySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "BeastMastery.cs"));

        dmChatCommandSource.Should().Contain("BeastMastery.GetAllBeastTypes()")
            .And.Contain("Usage: /spawnegg <beast type>")
            .And.Contain("[NWNEventHandler(ScriptName.OnModuleCacheAfter)]")
            .And.Contain("_spawnBeastEggHelpMessages = messages.ToArray()")
            .And.Contain("foreach (var message in _spawnBeastEggHelpMessages)")
            .And.Contain("BeastMastery.CreateBeastEgg(beastType, user)");
        beastMasterySource.Should().Contain("ItemPropertyCustom(ItemPropertyType.DNAType, (int)beastType)");
    }

    [Test]
    public void LocationTargeting_IsOptInForChatCommands()
    {
        var root = FindRepositoryRoot();
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

    [Test]
    public void ResetCooldowns_IncludesThePerkRefundTimer()
    {
        var commands = new DMChatCommand().BuildChatCommands();

        commands.Should().ContainKey("resetcooldowns");
        commands["resetcooldowns"].Description.Should().Contain("perk refund");

        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "ChatCommandDefinition",
            "DMChatCommand.cs"));
        var methodStart = source.IndexOf("private void ResetAbilityRecastTimers()", StringComparison.Ordinal);
        var methodEnd = source.IndexOf("private void AdjustFactionStanding()", methodStart, StringComparison.Ordinal);
        var method = source[methodStart..methodEnd];

        method.Should().Contain("dbPlayer.DatePerkRefundAvailable = DateTime.UtcNow;");
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
