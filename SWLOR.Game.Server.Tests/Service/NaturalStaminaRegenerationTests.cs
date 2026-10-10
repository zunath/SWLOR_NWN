using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class NaturalStaminaRegenerationTests
{
    [TestCase(0, 0, 0, 10)]
    [TestCase(10, 0, 0, 12)]
    [TestCase(26, 0, 0, 16)]
    [TestCase(10, 3, 5, 20)]
    [TestCase(-5, 0, 0, 10)]
    [TestCase(10, -20, 0, 0)]
    public void PlayerRecovery_DistributesTheEntireBudgetWithoutMultiplyingBonuses(
        int might, int persisted, int bonus, int expected)
    {
        var remainder = 0;
        var restored = 0;
        for (var heartbeat = 0; heartbeat < 5; heartbeat++)
            restored += NaturalRegeneration.GetStaminaRegenPerHeartbeat(might, persisted, bonus, ref remainder);

        restored.Should().Be(expected);
        remainder.Should().Be(0);
    }

    [Test]
    public void LowMightRecovery_PaysEveryHeartbeatAndCarriesFractions()
    {
        var remainder = 0;
        var amounts = Enumerable.Range(0, 10)
            .Select(_ => NaturalRegeneration.GetStaminaRegenPerHeartbeat(10, 0, 0, ref remainder))
            .ToArray();
        amounts.Should().Equal(2, 2, 3, 2, 3, 2, 2, 3, 2, 3);
    }

    [Test]
    public void ChangingFood_DoesNotBankAFullTickOrLoseFractionalRecovery()
    {
        var remainder = 0;
        NaturalRegeneration.GetStaminaRegenPerHeartbeat(10, 0, 0, ref remainder).Should().Be(2);
        NaturalRegeneration.GetStaminaRegenPerHeartbeat(10, 0, 5, ref remainder).Should().Be(3);
        NaturalRegeneration.GetStaminaRegenPerHeartbeat(10, 0, 0, ref remainder).Should().Be(3);
        remainder.Should().Be(1);
    }

    [TestCase(10, 25, 66)]
    [TestCase(26, 49, 96)]
    public void EmptyUnmodifiedPool_RecoversWithoutSeveralMinutesOfInactivity(int might, int maximum, int expectedSeconds)
    {
        var remainder = 0;
        var stamina = 0;
        var seconds = 0;
        while (stamina < maximum)
        {
            stamina += NaturalRegeneration.GetStaminaRegenPerHeartbeat(might, 0, 0, ref remainder);
            seconds += 6;
        }
        seconds.Should().Be(expectedSeconds);
    }

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

    [TestCase(0, 1)]
    [TestCase(9, 1)]
    [TestCase(10, 2)]
    [TestCase(25, 3)]
    [TestCase(34, 4)]
    [TestCase(-5, 1)]
    public void BeastNaturalRegeneration_ScalesWithGoverningAttribute(int attribute, int expected)
    {
        Stat.GetBeastNaturalRegenAmount(attribute).Should().Be(expected);
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
