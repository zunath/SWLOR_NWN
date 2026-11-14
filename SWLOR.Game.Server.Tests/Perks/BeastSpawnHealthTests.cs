using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Perks;

public class BeastSpawnHealthTests
{
    [Test]
    public void CallBeast_SpawnsAtFullHealthWithoutWaitingForTheDelayedCorrection()
    {
        // The spawn itself (the immediate restore before the delayed correction) is pinned by
        // BeastMasteryServiceTests; this test pins what Call Beast asks for.
        var root = FindRepositoryRoot();
        var callBeast = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "Beastmaster",
            "CallBeastAbilityDefinition.cs"));

        callBeast.Should().Contain(
            "BeastMastery.SpawnBeast(activator, dbPlayer.ActiveBeastId, 100);",
            "Call Beast should request a full-health companion");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
