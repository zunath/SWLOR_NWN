using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class PlayerFacingNameBroadcastTests
{
    [Test]
    public void CombatAndSpaceBroadcasts_DoNotInterpolateRawPlayerNames()
    {
        var root = FindRepositoryRoot();
        var paths = new List<string>
        {
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "UsePerkFeat.cs"),
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Space.cs")
        };

        paths.AddRange(Directory.GetFiles(
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "ShipModuleDefinition"),
            "*.cs"));

        foreach (var path in paths)
        {
            var rawNameBroadcastLines = File.ReadAllLines(path)
                .Where(line => line.Contains("Messaging.SendMessageNearbyToPlayers") && line.Contains("GetName("))
                .ToList();

            rawNameBroadcastLines.Should().BeEmpty($"{Path.GetFileName(path)} should render player-facing broadcast names per receiver");
        }
    }

    [Test]
    public void HoloComDirectory_UsesObserverSpecificDisplayNames()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "DialogDefinition",
            "HoloComDialog.cs"));

        source.Should().NotContain("GetName(");
        source.Should().Contain("PlayerName.GetDisplayName(player, pc)");
        source.Should().Contain("PlayerName.GetDisplayName(player, callSender)");
        source.Should().Contain("PlayerName.GetDisplayName(player, callReceiver)");
        source.Should().Contain("PlayerName.GetDisplayName(sender, receiver)");
    }

    [Test]
    public void PublicPlayerFacingSurfaces_DoNotExposeCanonicalPlayerNames()
    {
        var root = FindRepositoryRoot();

        var holoNetSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "HoloNetViewModel.cs"));
        holoNetSource.Should().Contain("PlayerName.GetDisplayName(onlinePlayer, Player)");
        holoNetSource.Should().Contain("\"HoloNet Broadcast\"");
        holoNetSource.Should().NotContain("authorName + \" broadcasts a new HoloNet message");

        var statusEffectSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "StatusEffect.cs"));
        statusEffectSource.Should().Contain("PlayerName.GetDisplayName(receiver, creature)");
        statusEffectSource.Should().Contain("PlayerName.GetDisplayName(receiver, source)");
        statusEffectSource.Should().NotContain("var name = GetName(creature);");

        var propertyPermissionsSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "PropertyPermissionsViewModel.cs"));
        propertyPermissionsSource.Should().Contain("PlayerNameService.SearchKnownPlayerIdsByName");
        propertyPermissionsSource.Should().Contain("PlayerNameService.GetDisplayNameByPlayerId");
        propertyPermissionsSource.Should().NotContain("nameof(Entity.Player.Name)");

        var electionSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "ElectionViewModel.cs"));
        electionSource.Should().Contain("PlayerName.GetDisplayNameByPlayerId(Player, candidate.Id, candidate.Name)");

        var citySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "ManageCityViewModel.cs"));
        citySource.Should().Contain("PlayerName.GetDisplayNameByPlayerId(Player, citizen.Id, citizen.Name)");

        var propertySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Property.cs"));
        propertySource.Should().NotContain("$\"{GetName(player)}'s Apartment\"");
        propertySource.Should().NotContain("$\"{GetName(player)}'s Starship\"");
        propertySource.Should().NotContain("$\"{GetName(player)}'s City\"");
        propertySource.Should().NotContain("GetPlayerName(");
        propertySource.Should().NotContain("**Mayor**:");
        propertySource.Should().NotContain("**New Mayor**:");
        propertySource.Should().NotContain("**Founding Mayor**:");
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
