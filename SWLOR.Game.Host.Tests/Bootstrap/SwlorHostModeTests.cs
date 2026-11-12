using FluentAssertions;
using Nwn.Hosting.Bootstrap;
using NUnit.Framework;
using SWLOR.Game.Host.Tests.Support;

namespace SWLOR.Game.Host.Tests.Bootstrap;

/// <summary>
/// SWLOR's host mode with the package's real <c>ReloadHost</c>, loading a compiled fixture build with SWLOR's load
/// options: the first generation activates on the engine thread before <c>LoadGame</c> returns, scripts come back with the
/// engine's -1/0/1 results, signals and frames reach the generation, and a failed boot stops the bootstrap.
/// </summary>
[TestFixture]
public class SwlorHostModeTests
{
    private string _root = null!;
    private string _build = null!;
    private string _failingBuild = null!;

    [OneTimeSetUp]
    public void BuildFixtures()
    {
        _root = Path.Combine(Path.GetTempPath(), "swlor-host-tests", Guid.NewGuid().ToString("N"));
        _build = FixtureGenerationBuilder.Build(_root);
        _failingBuild = FixtureGenerationBuilder.Build(_root, failActivation: true);
    }

    [OneTimeTearDown]
    public void DeleteFixtures()
    {
        try
        {
            Directory.Delete(_root, true);
        }
        catch (IOException)
        {
            // A collectible context may still be releasing the image; the temp folder is cleaned by the OS.
        }
    }

    [Test]
    public void The_game_is_validated_off_the_engine_thread_and_activated_on_it_before_LoadGame_returns()
    {
        var fixture = new HostedFixture(_build);

        fixture.StartAndLoad();

        var engineThread = Environment.CurrentManagedThreadId;
        var activation = fixture.Record.Single(line => line.StartsWith("activate ", StringComparison.Ordinal));
        activation.Should().Contain("generation=1").And.Contain($"thread={engineThread} ");
        activation.Should().NotContain($"validated={engineThread} ", "validation runs on a pool thread, never the engine thread");
        activation.Should().Contain("source=" + Path.GetDirectoryName(_build));
        fixture.Mode.ActiveGeneration.Should().Be(1);
        fixture.Record.Should().Contain("services redis=True closures=True");
    }

    [Test]
    public void The_generation_gets_the_hosts_redis_connection_and_its_closures_belong_to_it()
    {
        var fixture = new HostedFixture(_build);

        fixture.StartAndLoad();

        fixture.Mode.Services.Resolve(typeof(SWLOR.Game.Contracts.IRedisConnectionSource)).Should().BeSameAs(fixture.Redis);
        fixture.Redis.Opened.Should().Be(0, "the connection opens when the game's boot handler asks for it, not at activation");
        fixture.CoreFunctions.SchedulingGeneration.Should().Be(1, "closures scheduled from the activation on are the generation's");
        fixture.Scheduler.Scheduled.Should().ContainSingle(closure => closure.Kind == "DelayCommand" && closure.Object == 7);
    }

    [Test]
    public void The_generation_gets_the_hosts_native_hooks_to_enable_them_and_bind_its_sinks()
    {
        var fixture = new HostedFixture(_build);

        fixture.Mode.Services.Resolve(typeof(SWLOR.Game.Contracts.IHookControl)).Should().BeSameAs(fixture.Hooks);
        fixture.Mode.Services.Resolve(typeof(SWLOR.Game.Contracts.INativeSinkBinding)).Should().BeSameAs(fixture.Hooks,
            "the game installs and binds through the one table whose extensions resolved the engine functions");
    }

    [TestCase("act", NativeHost.ScriptHandled)]
    [TestCase("cond_t", NativeHost.ScriptConditionTrue)]
    [TestCase("cond_f", NativeHost.ScriptHandled)]
    [TestCase("unknown", NativeHost.ScriptNotHandled)]
    [TestCase("throw", NativeHost.ScriptNotHandled)]
    public void A_script_returns_the_generations_result_to_the_engine(string script, int expected)
    {
        var fixture = new HostedFixture(_build);
        fixture.StartAndLoad();

        var result = fixture.Mode.OnRunScriptResult(script, 0x42);

        result.Should().Be(expected);
        fixture.Mode.OnRunScript(script, 0x42).Should().Be(expected != NativeHost.ScriptNotHandled);
        fixture.Record.Should().Contain($"script {script} self={0x42}");
    }

    [Test]
    public void A_script_before_the_game_is_loaded_is_not_handled()
    {
        var fixture = new HostedFixture(_build);

        fixture.Mode.OnRunScriptResult("act", 1).Should().Be(NativeHost.ScriptNotHandled);

        fixture.Mode.Start();
        fixture.Mode.OnRunScriptResult("act", 1).Should().Be(NativeHost.ScriptNotHandled);
    }

    [Test]
    public void Every_signal_reaches_the_generations_handler()
    {
        var fixture = new HostedFixture(_build);
        fixture.StartAndLoad();

        fixture.Mode.OnSignal("ON_MODULE_LOAD_FINISH");
        fixture.Mode.OnSignal("ON_NWNX_LOADED");

        fixture.Record.Should().ContainInOrder("signal ON_MODULE_LOAD_FINISH", "signal ON_NWNX_LOADED");
        fixture.Mode.Callbacks.HasSignalHandler.Should().BeTrue();
    }

    [Test]
    public void Each_frame_ticks_the_generation_with_the_frame_number_NWNX_passed()
    {
        var fixture = new HostedFixture(_build);
        fixture.StartAndLoad();
        fixture.ClearRecord();

        fixture.Mode.BeforeFrame().Should().BeTrue();
        fixture.Mode.RunFrame(41);
        fixture.Mode.RunFrame(42);

        fixture.Record.Should().Equal("tick frame=41", "tick frame=42");
        fixture.Mode.Callbacks.Frame.Should().Be(42);
    }

    [Test]
    public void A_frame_before_the_game_is_loaded_ticks_nothing()
    {
        var fixture = new HostedFixture(_build);
        fixture.Mode.Start();

        fixture.Mode.RunFrame(1);

        fixture.Record.Should().BeEmpty();
        fixture.Mode.Callbacks.Frame.Should().Be(1);
    }

    [Test]
    public void After_the_server_is_destroyed_no_frame_runs_but_scripts_still_dispatch()
    {
        var fixture = new HostedFixture(_build);
        fixture.StartAndLoad();

        fixture.Mode.OnDestroyServer();

        fixture.Mode.BeforeFrame().Should().BeFalse();
        fixture.Mode.OnRunScriptResult("cond_t", 3).Should().Be(NativeHost.ScriptConditionTrue,
            "the native entry keeps answering scripts during the engine's teardown");
        fixture.Log.Should().Contain(line => line.StartsWith("destroy ", StringComparison.Ordinal));
    }

    [Test]
    public void A_build_that_fails_to_activate_stops_the_bootstrap()
    {
        var fixture = new HostedFixture(_failingBuild);
        fixture.Mode.Start();

        var load = () => fixture.Mode.LoadGame();

        load.Should().Throw<InvalidOperationException>().WithMessage("*did not activate (RestartRequired)*activation failed*");
        fixture.Mode.ActiveGeneration.Should().BeNull();
    }

    [Test]
    public void A_missing_build_stops_the_bootstrap()
    {
        var fixture = new HostedFixture(Path.Combine(_root, "missing", "SWLOR.Game.Server.dll"));
        fixture.Mode.Start();

        var load = () => fixture.Mode.LoadGame();

        load.Should().Throw<InvalidOperationException>().WithMessage("*did not activate (RejectedBeforeTeardown)*");
    }

    [Test]
    public void A_host_that_failed_to_start_refuses_to_load_the_game()
    {
        var fixture = new HostedFixture(_build);
        var failure = new InvalidOperationException("NWNX refused a callback");

        fixture.Mode.OnCallbackFailure("bootstrap", failure);
        var load = () => fixture.Mode.LoadGame();

        load.Should().Throw<InvalidOperationException>().WithMessage("The SWLOR host failed to start.").WithInnerException<InvalidOperationException>();
        fixture.Log.Should().Contain(line => line.StartsWith("failure bootstrap:", StringComparison.Ordinal));
    }

    [Test]
    public void LoadGame_needs_the_mode_to_have_started()
    {
        var fixture = new HostedFixture(_build);

        var load = () => fixture.Mode.LoadGame();

        load.Should().Throw<InvalidOperationException>().WithMessage("The SWLOR host was not started.");
    }

    [Test]
    public void A_non_collectible_build_loads_once()
    {
        var fixture = new HostedFixture(_build, collectible: false);

        fixture.StartAndLoad();

        fixture.Mode.ActiveGeneration.Should().Be(1);
        fixture.Mode.OnRunScriptResult("act", 1).Should().Be(NativeHost.ScriptHandled);
    }

    [Test]
    public void Callback_failures_are_logged_and_the_host_keeps_running()
    {
        var fixture = new HostedFixture(_build);
        fixture.StartAndLoad();

        fixture.Mode.OnCallbackFailure("closure", new InvalidOperationException("closure body"));
        fixture.Mode.OnFrameScheduleFailure(new InvalidOperationException("frame"));

        fixture.Log.Should().Contain(line => line.StartsWith("failure closure:", StringComparison.Ordinal));
        fixture.Log.Should().Contain(line => line.StartsWith("failure main-loop-schedule:", StringComparison.Ordinal));
        fixture.Mode.OnRunScriptResult("act", 1).Should().Be(NativeHost.ScriptHandled);
    }
}
