using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Native;

namespace SWLOR.Game.Server.Tests.Native;

public class StaleDriveAfterJumpTests
{
    private const long JumpedAt = 10_000;

    // Live client, master 2026-11-05: Shadow Step landed at (115.10, 21.56). The next drive path
    // started at the cast position (115.09, 15.06), 6.5 m away, and pulled the creature back 6.3 m.
    // Measured client-to-server drift on current paths stayed under 2 m.

    [Test]
    public void FirstFarStartShortlyAfterAJump_StartsFromTheServerPosition()
    {
        var state = new StaleDriveState { JumpedAt = JumpedAt };

        StaleDriveAfterJump.Decide(ref state, JumpedAt + 200, 6.5f, 15).Should().Be(StaleDriveDecision.StartFromServer);

        state.Correcting.Should().BeTrue();
        state.LastCorrectionAt.Should().Be(JumpedAt + 200);
        state.LastPathNumber.Should().Be(15);
    }

    [TestCase(1.8f, 200, TestName = "Ordinary drift right after a jump is left alone")]
    [TestCase(0f, 1400, TestName = "A current start late in the window is left alone")]
    public void NearStartBeforeAnyCorrection_IsLeftAloneDuringTheWindow(float gap, long elapsed)
    {
        var state = new StaleDriveState { JumpedAt = JumpedAt };

        StaleDriveAfterJump.Decide(ref state, JumpedAt + elapsed, gap, 15).Should().Be(StaleDriveDecision.LeaveAlone);
        state.Correcting.Should().BeFalse();
    }

    [TestCase(6.5f, 1600, TestName = "A far start after the window is left to the engine")]
    [TestCase(0f, 1600, TestName = "A current start after the window ends the watch")]
    [TestCase(6.5f, -10, TestName = "A far start before the recorded jump is left to the engine")]
    public void StartsOutsideTheWindow_ReleaseTheWatch(float gap, long elapsed)
    {
        var state = new StaleDriveState { JumpedAt = JumpedAt };

        StaleDriveAfterJump.Decide(ref state, JumpedAt + elapsed, gap, 15).Should().Be(StaleDriveDecision.Release);
    }

    [Test]
    public void ClientStillDrivingFromItsOldTrack_KeepsStartingFromTheServerPosition()
    {
        // Live client, 4 s hold: the client kept sending starts about 6.4 m behind the server for
        // over a second, and the window alone let one through after 1.5 s (a 9 m snap back).
        var state = new StaleDriveState { JumpedAt = JumpedAt };
        StaleDriveAfterJump.Decide(ref state, JumpedAt + 200, 6.5f, 29);

        StaleDriveAfterJump.Decide(ref state, JumpedAt + 600, 6.3f, 31).Should().Be(StaleDriveDecision.StartFromServer);
        StaleDriveAfterJump.Decide(ref state, JumpedAt + 3000, 9.3f, 37).Should().Be(StaleDriveDecision.StartFromServer);

        state.LastCorrectionAt.Should().Be(JumpedAt + 3000);
        state.LastPathNumber.Should().Be(37);
    }

    [Test]
    public void EngineContinuationOfARestartedPath_IsLeftAlone()
    {
        // The engine continues the restarted path from the server position under the same number.
        var state = new StaleDriveState { JumpedAt = JumpedAt };
        StaleDriveAfterJump.Decide(ref state, JumpedAt + 200, 6.5f, 29);

        StaleDriveAfterJump.Decide(ref state, JumpedAt + 600, 0f, 29).Should().Be(StaleDriveDecision.LeaveAlone);
        state.Correcting.Should().BeTrue();
    }

    [Test]
    public void NewPathFromTheServerPosition_ReleasesTheWatch()
    {
        var state = new StaleDriveState { JumpedAt = JumpedAt };
        StaleDriveAfterJump.Decide(ref state, JumpedAt + 200, 6.5f, 29);

        StaleDriveAfterJump.Decide(ref state, JumpedAt + 700, 1.5f, 30).Should().Be(StaleDriveDecision.Release);
    }

    [Test]
    public void FarStartLongAfterTheLastCorrection_IsLeftToTheEngine()
    {
        var state = new StaleDriveState { JumpedAt = JumpedAt };
        StaleDriveAfterJump.Decide(ref state, JumpedAt + 200, 6.5f, 29);

        StaleDriveAfterJump.Decide(ref state,
                JumpedAt + 200 + StaleDriveAfterJump.CorrectionTimeoutMilliseconds + 1, 6.5f, 40)
            .Should().Be(StaleDriveDecision.Release);
    }

    [TestCase(115.10f, 15.06f, 115.10f, 21.56f, true, TestName = "Shadow Step jump is a teleport")]
    [TestCase(115.10f, 15.06f, 115.10f, 18.56f, true, TestName = "Force Leap jump is a teleport")]
    [TestCase(115.10f, 21.56f, 115.10f, 22.80f, false, TestName = "Short nudge is not a teleport")]
    public void IsTeleport_RequiresMoreThanClientDrift(float fromX, float fromY, float toX, float toY, bool teleport)
    {
        StaleDriveAfterJump.IsTeleport(fromX, fromY, toX, toY).Should().Be(teleport);
    }
}
