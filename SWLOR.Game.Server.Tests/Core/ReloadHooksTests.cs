using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// The hot reload's hooks: the game's own are found and well formed, and a run calls each hook of its phase in order, static or on the
/// container's instance, goes on past a hook that fails and names it. The run tests use hooks of this assembly, marked with phases the
/// game never runs, so nothing of the game's runs.
/// </summary>
[TestFixture]
[NonParallelizable]
public class ReloadHooksTests
{
    private static readonly Assembly Game = ScriptHandlerTestSupport.GameAssembly;
    private static readonly Assembly Tests = typeof(ReloadHooksTests).Assembly;

    internal const ReloadHookPhase TestPhase = (ReloadHookPhase)100;
    internal const ReloadHookPhase FailingPhase = (ReloadHookPhase)101;
    internal const ReloadHookPhase MalformedPhase = (ReloadHookPhase)102;

    internal static readonly List<string> Calls = new();

    [SetUp]
    public void ClearCalls() => Calls.Clear();

    [Test]
    public void TheQuiesceHooksEndTheWorkInFlightThatAReloadWouldStrand()
    {
        Names(ReloadHooks.Discover(Game, ReloadHookPhase.Quiesce)).Should().Contain(new[]
        {
            "AbilityService.QuiesceForReload",
            "ItemService.QuiesceForReload",
            "FishingService.QuiesceForReload",
            "StealthService.QuiesceForReload",
            "TrapsService.ClearTrapsForReload",
            "PairedRangedPresentation.ReleaseAllForReload",
            "DeviceAbilityEffects.EndFieldEngineerPulsesForReload",
        });
    }

    [Test]
    public void TheResumeHooksRestartWhatTheOldBuildsClosuresWouldHaveDone()
    {
        Names(ReloadHooks.Discover(Game, ReloadHookPhase.Resume)).Should().Contain(new[]
        {
            "DoorScripts.ResumeAutoRelockForReload",
            "SavedAnimationMappings.ResetCreatureMappingsForReload",
            "PlayerTemporaryEffects.RestoreGhostForReload",
            "NwnCraftSessionWorld.ExportPendingCharactersForReload",
            "LootService.ResumeCorpseCleanupForReload",
            "NwnScavengeWorld.SettleSitesForReload",
            "QuestService.ResumeItemCollectorSweepsForReload",
        });
    }

    [Test]
    public void ARunCallsEachHookOfItsPhaseByOrderThenClassWithThePlayers()
    {
        var owner = new InstanceHooks();
        var players = new uint[] { 7, 9 };

        ReloadHooks.Run(Tests, TestPhase, type => type == typeof(InstanceHooks) ? owner : null, players);

        Calls.Should().Equal(new[] { "instance 7,9", "first", "static" },
            "order 0 hooks run by class name (InstanceHooks before StaticHooks), the order 5 hook last");
    }

    [Test]
    public void ARunGoesOnPastAFailingHookAndNamesItAndAnyOwnerTheContainerCannotGive()
    {
        var run = () => ReloadHooks.Run(Tests, FailingPhase, _ => null, Array.Empty<uint>());

        run.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("FailingHooks.Throws").And.Contain("FailingHooks.OnAMissingInstance");
        Calls.Should().Equal(new[] { "after the failure" }, "the hooks after a failure still run");
    }

    [Test]
    public void AHookThatDoesNotTakeOnlyThePlayersIsRefused()
    {
        var discover = () => ReloadHooks.Discover(Tests, MalformedPhase);

        discover.Should().Throw<InvalidOperationException>().WithMessage("*MalformedHooks.TakesNothing*IReadOnlyList<uint>*");
    }

    private static IEnumerable<string> Names(IEnumerable<MethodInfo> hooks) =>
        hooks.Select(method => $"{method.DeclaringType!.Name}.{method.Name}");

    public static class StaticHooks
    {
        [ReloadHook(TestPhase, Order = 5)]
        public static void Last(IReadOnlyList<uint> players) => Calls.Add("static");

        [ReloadHook(TestPhase)]
        public static void First(IReadOnlyList<uint> players) => Calls.Add("first");
    }

    public class InstanceHooks
    {
        [ReloadHook(TestPhase)]
        public void OnTheContainersInstance(IReadOnlyList<uint> players) =>
            Calls.Add("instance " + string.Join(",", players));
    }

    public class FailingHooks
    {
        [ReloadHook(FailingPhase)]
        public static void Throws(IReadOnlyList<uint> players) => throw new InvalidOperationException("a hook failed");

        [ReloadHook(FailingPhase, Order = 1)]
        public void OnAMissingInstance(IReadOnlyList<uint> players) => Calls.Add("never");

        [ReloadHook(FailingPhase, Order = 2)]
        public static void AfterTheFailure(IReadOnlyList<uint> players) => Calls.Add("after the failure");
    }

    public static class MalformedHooks
    {
        [ReloadHook(MalformedPhase)]
        public static void TakesNothing() { }
    }
}
