using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Perks;

public class BeastSpawnHealthTests
{
    [Test]
    public void CallBeast_SpawnsAtFullHealthWithoutWaitingForTheDelayedCorrection()
    {
        var root = FindRepositoryRoot();
        var callBeast = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "Beastmaster",
            "CallBeastAbilityDefinition.cs"));
        var beastMastery = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "BeastMastery.cs"));

        callBeast.Should().Contain(
            "BeastMastery.SpawnBeast(activator, dbPlayer.ActiveBeastId, 100);",
            "Call Beast should request a full-health companion");

        var immediateRestore = beastMastery.IndexOf(
            "if (percentHeal >= 100)",
            StringComparison.Ordinal);
        var delayedCorrection = beastMastery.IndexOf(
            "DelayCommand(4f",
            StringComparison.Ordinal);
        var immediateSetMax = beastMastery.IndexOf(
            "SetCurrentHitPoints(beast, GetMaxHitPoints(beast));",
            StringComparison.Ordinal);

        immediateRestore.Should().BeGreaterThan(-1);
        immediateSetMax.Should().BeGreaterThan(immediateRestore,
            "the full-health branch should restore the beast to its maximum hit points");
        delayedCorrection.Should().BeGreaterThan(immediateRestore,
            "full-health spawns should be restored before the delayed HP correction runs");
        immediateSetMax.Should().BeLessThan(delayedCorrection,
            "the beast should reach full health immediately rather than after the delayed correction");
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
