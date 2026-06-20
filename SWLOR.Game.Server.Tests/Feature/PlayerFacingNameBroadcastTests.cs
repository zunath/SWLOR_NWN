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
