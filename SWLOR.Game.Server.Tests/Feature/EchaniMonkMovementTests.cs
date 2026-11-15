using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class EchaniMonkMovementTests
{
    [Test]
    public void BlockedMovementRepathsThenBacktracksWithoutWaitingForTheActionToFinish()
    {
        var stalled = 0;
        var retries = 0;
        var repaths = new List<int>();
        var backtracks = new List<int>();
        for (var heartbeat = 1; heartbeat <= 6; heartbeat++)
        {
            var decision = Evaluate(0, stalled, retries);
            stalled = decision.Stalled;
            retries = decision.Retries;
            if (decision.Repath) repaths.Add(heartbeat);
            if (decision.Backtrack) backtracks.Add(heartbeat);
        }

        repaths.Should().Equal(3, 6);
        backtracks.Should().Equal(6);
    }

    [Test]
    public void MovingSidewaysAroundAnObstacleDoesNotTriggerRecovery()
    {
        for (var heartbeat = 0; heartbeat < 20; heartbeat++)
        {
            var decision = Evaluate(0.5f * 0.5f, 0, 0);
            decision.Repath.Should().BeFalse();
            decision.Backtrack.Should().BeFalse();
        }
    }

    [Test]
    public void SmallCollisionJitterDoesNotKeepAStalledMoveAlive()
    {
        var first = Evaluate(0.1f * 0.1f, 0, 0);
        var second = Evaluate(0.15f * 0.15f, first.Stalled, first.Retries);
        var third = Evaluate(0.2f * 0.2f, second.Stalled, second.Retries);
        third.Repath.Should().BeTrue();
    }

    [Test]
    public void ResumedWalkingClearsStallAndRetryHistory()
    {
        var moving = Evaluate(1, 2, 1);
        moving.Stalled.Should().Be(0);
        moving.Retries.Should().Be(0);
        var stoppedAgain = Evaluate(0, moving.Stalled, moving.Retries);
        stoppedAgain.Repath.Should().BeFalse();
        stoppedAgain.Backtrack.Should().BeFalse();
    }

    [Test]
    public void BothActiveAndFailedIdleOwnedMovesCanBeRecovered()
    {
        CanManage(ActionType.MoveToPoint, true).Should().BeTrue();
        CanManage(ActionType.Invalid, true).Should().BeTrue();
        CanManage(ActionType.Invalid, false).Should().BeTrue();
    }

    [TestCase(ActionType.MoveToPoint)]
    [TestCase(ActionType.DialogObject)]
    [TestCase(ActionType.AttackObject)]
    [TestCase(ActionType.CastSpell)]
    [TestCase(ActionType.Wait)]
    [TestCase(ActionType.Sit)]
    public void UnownedActionsCannotBeReplacedByRouteRecovery(ActionType action)
    {
        CanManage(action, false).Should().BeFalse();
    }

    [TestCase(ActionType.DialogObject)]
    [TestCase(ActionType.AttackObject)]
    [TestCase(ActionType.CastSpell)]
    [TestCase(ActionType.Wait)]
    public void StaleMovementOwnershipDoesNotClaimANewAction(ActionType action)
    {
        CanManage(action, true).Should().BeFalse();
    }

    [Test]
    public void GroupSceneWaitsForTheFollowerStillOnAnEarlierRoadPoint()
    {
        CanStart(new uint[] { 12, 11, 12 }, new[] { true, true, true }).Should().BeFalse();
        CanStart(new uint[] { 12, 12, 12 }, new[] { true, false, true }).Should().BeFalse();
        CanStart(new uint[] { 12, 12, 12 }, new[] { true, true, true }).Should().BeTrue();
    }

    [Test]
    public void MissingGroupMembersCannotStartASceneOrAreaTransfer()
    {
        CanStart(Array.Empty<uint>(), Array.Empty<bool>()).Should().BeFalse();
        CanStart(new uint[] { 12, 12, 12 }, new[] { true, true }).Should().BeFalse();
    }

    private static (int Stalled, int Retries, bool Repath, bool Backtrack) Evaluate(
        float distanceMovedSquared, int stalled, int retries) =>
        Invoke<(int, int, bool, bool)>("EvaluateMovement", distanceMovedSquared, stalled, retries);

    private static bool CanManage(ActionType action, bool owned) =>
        Invoke<bool>("CanManageMovement", action, owned);

    private static bool CanStart(uint[] stops, bool[] arrived) =>
        Invoke<bool>("CanStartGroupStop", stops, arrived);

    private static T Invoke<T>(string name, params object[] arguments)
    {
        var method = typeof(EchaniTempleMonks).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(typeof(EchaniTempleMonks).FullName, name);
        return (T)method.Invoke(null, arguments)!;
    }
}
