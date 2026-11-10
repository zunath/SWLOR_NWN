using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Tests.Service;

public class PerformanceHotbarRecoveryTests
{
    [TestCase(0, 0, false)]
    [TestCase(8, 7, true)]
    [TestCase(8, 9, true)]
    public void NoRecoveryCopy_NeverChangesTheHotbar(int restored, int loaded, bool atLogin)
    {
        PerformanceHotbarRecovery.Decide(null, restored, loaded, atLogin)
            .Should().Be(PerformanceHotbarRecoveryAction.None);
    }

    [TestCase("original", 0, false)]
    [TestCase("original", 20, true)]
    [TestCase("", 0, true)]
    public void AnInterruptedShowOrLegacyBackup_RestoresEvenWithoutACheckpoint(string saved, int loaded, bool atLogin)
    {
        PerformanceHotbarRecovery.Decide(saved, 0, loaded, atLogin)
            .Should().Be(PerformanceHotbarRecoveryAction.Restore);
    }

    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void StartingAnotherShow_KeepsEditsMadeAfterThePreviousShowStopped(int loaded)
    {
        PerformanceHotbarRecovery.Decide("older bar", 8, loaded, atLogin: false)
            .Should().Be(PerformanceHotbarRecoveryAction.None);
    }

    [Test]
    public void ACrashBeforeTheRestoreExport_StillHasARecoveryCopyOnEveryRetry()
    {
        PerformanceHotbarRecovery.Decide("original", 8, 7, atLogin: true)
            .Should().Be(PerformanceHotbarRecoveryAction.Restore);
        PerformanceHotbarRecovery.Decide("original", 9, 7, atLogin: true)
            .Should().Be(PerformanceHotbarRecoveryAction.Restore);
        PerformanceHotbarRecovery.Decide("original", 9, 9, atLogin: true)
            .Should().Be(PerformanceHotbarRecoveryAction.Forget);
    }

    [TestCase(8)]
    [TestCase(12)]
    public void AFileThatHoldsTheRestoration_KeepsAnyNewerHotbarEdits(int loaded)
    {
        PerformanceHotbarRecovery.Decide("older bar", 8, loaded, atLogin: true)
            .Should().Be(PerformanceHotbarRecoveryAction.Forget);
    }

    [Test]
    public void ANewShowResetsTheCheckpoint_SoItsOwnBackupIsRecoveredAfterACrash()
    {
        PerformanceHotbarRecovery.Decide("newer customized bar", 0, 8, atLogin: true)
            .Should().Be(PerformanceHotbarRecoveryAction.Restore);
    }
}
