using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class NaturalStaminaRegenerationTests
{
    [Test]
    public void BeastNaturalStaminaRegeneration_WaitsSixSecondsAfterStaminaSpend()
    {
        var spentAt = new DateTime(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);
        var availableAt = spentAt.AddSeconds(Stat.BeastNaturalStaminaRegenDelaySeconds);

        Stat.IsNaturalStaminaRegenerationAvailable(availableAt.Ticks, spentAt.Ticks)
            .Should().BeFalse();
        Stat.IsNaturalStaminaRegenerationAvailable(availableAt.Ticks, availableAt.AddTicks(-1).Ticks)
            .Should().BeFalse();
        Stat.IsNaturalStaminaRegenerationAvailable(availableAt.Ticks, availableAt.Ticks)
            .Should().BeTrue();
    }

    [Test]
    public void BeastHeartbeat_UsesDelayedStaminaRegenerationWithoutCombatGating()
    {
        var root = FindRepositoryRoot();
        var beastMastery = File.ReadAllText(Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Service",
            "BeastMastery.cs"));
        var stat = File.ReadAllText(Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Service",
            "Stat.cs"));

        beastMastery.Should().Contain("Stat.RestoreBeastStats();");
        stat.Should().Contain("RestoreNPCStats(false, true);");
        stat.Should().Contain("BeastMastery.IsPlayerBeast(creature)");

        var restoreBeastStats = stat.Substring(
            stat.IndexOf("public static void RestoreBeastStats()", StringComparison.Ordinal),
            stat.IndexOf("public static bool IsNaturalStaminaRegenerationAvailable", StringComparison.Ordinal) -
            stat.IndexOf("public static void RestoreBeastStats()", StringComparison.Ordinal));
        restoreBeastStats.Should().NotContain("GetIsInCombat");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
