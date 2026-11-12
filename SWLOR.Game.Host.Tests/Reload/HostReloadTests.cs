using FluentAssertions;
using Nwn.Hosting.Bootstrap;
using Nwn.Hosting.Contracts.Generations;
using Nwn.Hosting.Reload;
using Nwn.Hosting.Testing.Fixtures;
using NUnit.Framework;
using SWLOR.Game.Host.Bootstrap;
using SWLOR.Game.Host.Reload;
using SWLOR.Game.Host.Tests.Support;
using static Nwn.Hosting.Testing.Support.TestAssertions;

namespace SWLOR.Game.Host.Tests.Reload;

/// <summary>
/// The host mode replacing a running fixture build with a candidate, on the package's stand-in engine thread
/// (<see cref="SingleThreadDispatcher"/>), which the test drives frame by frame while the reload runs off it. From the old
/// build's quiescence to the new build's activation the engine thread stays inside one frame: no frame returns and nothing
/// ticks in between. A candidate rejected in validation leaves the old build running untouched; a failure after
/// quiescence leaves the host needing a restart. With the switch off, or refused, nothing of this exists.
/// </summary>
[TestFixture]
public class HostReloadTests
{
    private static readonly TimeSpan Poll = TimeSpan.FromMilliseconds(20);
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(60);

    private static readonly IReadOnlyDictionary<string, string?> ReloadOn = new Dictionary<string, string?>
    {
        [HostSettings.EnvironmentVariable] = "test",
        [HostSettings.HotReloadVariable] = "true"
    };

    private SingleThreadDispatcher _engine = null!;
    private string _root = null!;
    private string _hostDirectory = null!;
    private string _build = null!;
    private ReloadLayout _layout = null!;
    private ulong _frame;
    private readonly List<HostedFixture> _fixtures = [];

    [OneTimeSetUp]
    public void StartEngineThread()
    {
        _engine = new SingleThreadDispatcher();
    }

    [OneTimeTearDown]
    public async Task ParkEngineThread()
    {
        await _engine.DisposeAsync();
    }

    [SetUp]
    public void BuildHome()
    {
        _root = Path.Combine(Path.GetTempPath(), "swlor-host-reload-mode", Guid.NewGuid().ToString("N"));
        _hostDirectory = Path.Combine(_root, "dotnet");
        _build = FixtureGenerationBuilder.BuildInto(Path.Combine(_hostDirectory, "game"), "HostFixture.dll");
        _layout = ReloadLayout.ForServerHome(_root);
        _frame = 1000;
    }

    [TearDown]
    public async Task DeleteHome()
    {
        // Stop every watcher a test started, so none keeps polling a deleted folder.
        foreach (var fixture in _fixtures)
        {
            if (fixture.Mode.Trigger is { } trigger)
                await trigger.DisposeAsync();
        }
        _fixtures.Clear();

        try
        {
            Directory.Delete(_root, true);
        }
        catch (IOException)
        {
            // A collectible context may still be releasing a candidate; the temp folder is the OS's.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private async Task<HostedFixture> Boot(IReadOnlyDictionary<string, string?>? variables, int? databaseSchemaVersion = 0)
    {
        HostedFixture? fixture = null;
        await OnEngine(() =>
        {
            fixture = new HostedFixture(_build, variables: variables, hostDirectory: _hostDirectory, reloadPollInterval: Poll,
                databaseSchemaVersion: databaseSchemaVersion);
            fixture.StartAndLoad();
        });
        _fixtures.Add(fixture!);
        return fixture!;
    }

    private Task OnEngine(Action action) =>
        _engine.InvokeAsync(() =>
        {
            action();
            return ValueTask.CompletedTask;
        }, CancellationToken.None).AsTask();

    /// <summary>One engine frame; once the host returns from it, the frame records that it returned.</summary>
    private Task Frame(HostedFixture fixture) =>
        OnEngine(() =>
        {
            var frame = ++_frame;
            fixture.Mode.RunFrame(frame);
            fixture.Record.Enqueue($"frame-returned {frame}");
        });

    /// <summary>Runs engine frames, as the server's main loop would, until <paramref name="work"/> has finished.</summary>
    private async Task<T> FramesUntil<T>(HostedFixture fixture, Task<T> work)
    {
        var deadline = DateTime.UtcNow + Wait;
        while (!work.IsCompleted)
        {
            Require(DateTime.UtcNow < deadline, "The reload did not finish while the engine ran frames.");
            await Frame(fixture);
        }

        return await work;
    }

    private string PublishCandidate(Guid id, bool failValidation = false, bool failActivation = false, int? requiredSchemaVersion = null) =>
        FixtureGenerationBuilder.BuildInto(_layout.CandidateDirectory(id), ReloadLayout.GameAssemblyFileName, failActivation, failValidation,
            requiredSchemaVersion);

    [Test]
    public async Task A_candidate_that_needs_a_server_migration_is_rejected_with_a_receipt_saying_a_restart_is_required()
    {
        var fixture = await Boot(ReloadOn, databaseSchemaVersion: 42);
        await OnEngine(() => fixture.Mode.OnSignal("ON_MODULE_LOAD_FINISH"));
        fixture.ClearRecord();
        var id = Guid.NewGuid();
        PublishCandidate(id, requiredSchemaVersion: 43);
        var temporary = _layout.SignalPath + ".tmp";
        await DevelopmentReloadFiles.WriteRequestAsync(temporary,
            new DevelopmentReloadRequest(DevelopmentReloadFiles.CurrentFormatVersion, id), CancellationToken.None);
        File.Move(temporary, _layout.SignalPath);

        var receipt = await FramesUntil(fixture, FinalReceipt(id));
        await OnEngine(() => fixture.Mode.OnRunScriptResult("act", 3).Should().Be(NativeHost.ScriptHandled));

        receipt.Status.Should().Be(ReloadReceiptStatus.Rejected, receipt.Detail);
        receipt.Detail.Should().Contain("needs server migration 43, but the database is at 42").And.Contain("a restart is required");
        receipt.PauseMilliseconds.Should().BeNull("a rejected candidate never holds the engine thread");
        fixture.Mode.ActiveGeneration.Should().Be(1);
        fixture.Record.Should().NotContain(line => line.StartsWith("quiesce", StringComparison.Ordinal));
        fixture.Record.Should().Contain("script act self=3", "the running build keeps serving");
        fixture.Log.Should().Contain(line => line.StartsWith("reload-schema The candidate needs server migration 43", StringComparison.Ordinal));
    }

    [TestCase(42)]
    [TestCase(41)]
    public async Task A_candidate_at_or_below_the_databases_migration_passes_the_schema_gate(int required)
    {
        var fixture = await Boot(ReloadOn, databaseSchemaVersion: 42);
        await OnEngine(() => fixture.Mode.OnSignal("ON_MODULE_LOAD_FINISH"));

        var candidate = PublishCandidate(Guid.NewGuid(), requiredSchemaVersion: required);
        var attempt = await FramesUntil(fixture, Task.Run(() => fixture.Mode.ReloadAsync(candidate, CancellationToken.None)));

        attempt.Report.Outcome.Should().Be(ReloadOutcome.Applied, attempt.Report.Detail);
        attempt.Pause.Should().NotBeNull();
        fixture.Log.Should().Contain($"reload-schema The candidate's newest server migration ({required}) is at or below the database's (42).");
    }

    [Test]
    public async Task No_engine_frame_runs_between_the_old_builds_quiescence_and_the_new_builds_activation()
    {
        var fixture = await Boot(ReloadOn);
        await OnEngine(() => fixture.Mode.OnSignal("ON_MODULE_LOAD_FINISH"));
        await Frame(fixture);
        fixture.ClearRecord();

        var candidate = PublishCandidate(Guid.NewGuid());
        var attempt = await FramesUntil(fixture, Task.Run(() => fixture.Mode.ReloadAsync(candidate, CancellationToken.None)));
        await Frame(fixture);
        await OnEngine(() => fixture.Mode.OnRunScriptResult("cond_t", 5).Should().Be(NativeHost.ScriptConditionTrue));

        attempt.Report.Outcome.Should().Be(ReloadOutcome.Applied, attempt.Report.Detail);
        attempt.Report.ActiveGeneration.Should().Be(2);
        fixture.Mode.ActiveGeneration.Should().Be(2);
        var record = fixture.Record.ToList();
        var quiesce = record.FindIndex(line => line == $"quiesce generation=1 thread={_engine.ThreadId}");
        var dispose = record.IndexOf("dispose generation=1");
        var activate = record.FindIndex(line => line.StartsWith("activate generation=2 ", StringComparison.Ordinal));
        quiesce.Should().BeGreaterThanOrEqualTo(0, "the old build is quiesced on the engine thread: " + string.Join(" | ", record));
        dispose.Should().BeGreaterThan(quiesce);
        activate.Should().BeGreaterThan(dispose);
        record.GetRange(quiesce, activate - quiesce).Should()
            .NotContain(line => line.StartsWith("frame-returned", StringComparison.Ordinal) || line.StartsWith("tick", StringComparison.Ordinal),
                "the engine thread stays in one frame from quiescence to activation");
        record[activate].Should().Contain($"thread={_engine.ThreadId} ");
        record.Should().Contain("lifecycle generation=2 moduleLoaded=True", "the new build knows the module has loaded");

        var held = attempt.PausedFrame!.Value;
        attempt.Pause.Should().NotBeNull().And.BeGreaterThan(TimeSpan.Zero);
        record.GetRange(activate, record.Count - activate).Should()
            .ContainInOrder($"tick frame={held}", $"frame-returned {held}", "script cond_t self=5");
        record.Take(quiesce).Should().NotContain($"frame-returned {held}");
        fixture.Log.Should().Contain(line => line.StartsWith($"reload-pause frame={held} heldMs=", StringComparison.Ordinal));
        fixture.Log.Should().Contain(line => line.StartsWith($"reload-start validating {candidate}", StringComparison.Ordinal));
    }

    [Test]
    public async Task The_pause_counts_the_old_builds_quiescence_even_when_the_frame_runs_it_before_the_hold()
    {
        // The booted build takes 400 ms to quiesce; whichever pump of the holding frame runs it, the engine was blocked.
        _build = FixtureGenerationBuilder.BuildInto(Path.Combine(_hostDirectory, "game"), "HostFixture.dll", slowQuiescence: true);
        var fixture = await Boot(ReloadOn);
        await OnEngine(() => fixture.Mode.OnSignal("ON_MODULE_LOAD_FINISH"));
        await Frame(fixture);

        var candidate = PublishCandidate(Guid.NewGuid());
        var attempt = await FramesUntil(fixture, Task.Run(() => fixture.Mode.ReloadAsync(candidate, CancellationToken.None)));

        attempt.Report.Outcome.Should().Be(ReloadOutcome.Applied, attempt.Report.Detail);
        attempt.Pause.Should().NotBeNull().And.BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(400),
            "the old build's quiescence ran while the engine thread was held");
    }

    [Test]
    public async Task A_candidate_rejected_in_validation_leaves_the_old_build_running_and_the_frames_flowing()
    {
        var fixture = await Boot(ReloadOn);
        await OnEngine(() => fixture.Mode.OnSignal("ON_MODULE_LOAD_FINISH"));
        fixture.ClearRecord();

        var candidate = PublishCandidate(Guid.NewGuid(), failValidation: true);
        var attempt = await FramesUntil(fixture, Task.Run(() => fixture.Mode.ReloadAsync(candidate, CancellationToken.None)));
        await OnEngine(() => fixture.Mode.OnRunScriptResult("act", 9).Should().Be(NativeHost.ScriptHandled));

        attempt.Report.Outcome.Should().Be(ReloadOutcome.RejectedBeforeTeardown);
        attempt.Report.Detail.Should().Contain("validation failed");
        attempt.Pause.Should().BeNull("a rejected candidate never holds the engine thread");
        attempt.PausedFrame.Should().BeNull();
        fixture.Mode.ActiveGeneration.Should().Be(1);
        fixture.Record.Should().NotContain(line => line.StartsWith("quiesce", StringComparison.Ordinal) || line.StartsWith("dispose", StringComparison.Ordinal));
        fixture.Record.Should().Contain("script act self=9");
        fixture.Log.Should().NotContain(line => line.StartsWith("reload-pause", StringComparison.Ordinal));
    }

    [Test]
    public async Task A_failure_after_quiescence_leaves_the_host_needing_a_restart()
    {
        var fixture = await Boot(ReloadOn);
        await OnEngine(() => fixture.Mode.OnSignal("ON_MODULE_LOAD_FINISH"));
        fixture.ClearRecord();

        var candidate = PublishCandidate(Guid.NewGuid(), failActivation: true);
        var attempt = await FramesUntil(fixture, Task.Run(() => fixture.Mode.ReloadAsync(candidate, CancellationToken.None)));
        fixture.ClearRecord();
        await Frame(fixture);

        attempt.Report.Outcome.Should().Be(ReloadOutcome.RestartRequired);
        attempt.Pause.Should().NotBeNull("the engine thread was held until the reload failed");
        fixture.Mode.ActiveGeneration.Should().BeNull();
        fixture.Log.Should().Contain(line => line.StartsWith("restart-required the reload failed after generation 1 was quiesced", StringComparison.Ordinal)
                                             && line.Contains("needs a restart"));
        fixture.Record.Should().Equal($"frame-returned {_frame}");
        await OnEngine(() => fixture.Mode.OnRunScriptResult("act", 1).Should().Be(NativeHost.ScriptNotHandled));

        var again = PublishCandidate(Guid.NewGuid());
        var refused = await FramesUntil(fixture, Task.Run(() => fixture.Mode.ReloadAsync(again, CancellationToken.None)));
        refused.Report.Outcome.Should().Be(ReloadOutcome.RejectedBeforeTeardown);
        refused.Report.Detail.Should().Contain("requires supervisor restart");
    }

    [Test]
    public async Task A_signal_written_while_the_server_runs_is_accepted_applied_and_answered_with_the_pause()
    {
        var fixture = await Boot(ReloadOn);
        fixture.Mode.Trigger.Should().NotBeNull();
        fixture.Mode.Trigger!.Watching.Should().BeFalse("the host watches only once the module has loaded");
        fixture.Record.Should().Contain("lifecycle generation=1 moduleLoaded=False");

        await OnEngine(() => fixture.Mode.OnSignal("ON_MODULE_LOAD_FINISH"));
        fixture.Mode.Trigger.Watching.Should().BeTrue();
        var id = Guid.NewGuid();
        PublishCandidate(id);
        // As SWLOR.CLI --reload writes it: a whole request moved over the signal file.
        var temporary = _layout.SignalPath + ".tmp";
        await DevelopmentReloadFiles.WriteRequestAsync(temporary,
            new DevelopmentReloadRequest(DevelopmentReloadFiles.CurrentFormatVersion, id), CancellationToken.None);
        File.Move(temporary, _layout.SignalPath);

        var receipt = await FramesUntil(fixture, FinalReceipt(id));

        receipt.Status.Should().Be(ReloadReceiptStatus.Applied, receipt.Detail);
        receipt.ActiveGeneration.Should().Be(2);
        receipt.PauseMilliseconds.Should().BeGreaterThan(0);
        receipt.PausedFrame.Should().NotBeNull();
        fixture.Log.Should().Contain(line => line.StartsWith("reload-receipt ", StringComparison.Ordinal) && line.Contains("status=Accepted"));
        fixture.Log.Should().Contain(line => line.StartsWith("reload-receipt ", StringComparison.Ordinal) && line.Contains("status=Applied"));
        fixture.Log.Should().Contain(line => line.StartsWith("reload-on ", StringComparison.Ordinal));
        fixture.Record.Should().Contain("lifecycle generation=2 moduleLoaded=True");

        await OnEngine(() => fixture.Mode.OnDestroyServer());
        fixture.Mode.Trigger.Should().BeNull("the trigger stops with the server");
    }

    private async Task<ReloadReceipt> FinalReceipt(Guid id)
    {
        var deadline = DateTime.UtcNow + Wait;
        while (DateTime.UtcNow < deadline)
        {
            if (ReloadReceiptFiles.Read(_layout.ReceiptPath(id)) is { IsFinal: true } receipt)
                return receipt;
            await Task.Delay(Poll);
        }

        throw new TimeoutException($"No final receipt for {id}.");
    }

    [Test]
    public async Task With_reload_off_there_is_no_trigger_and_the_module_load_only_marks_the_lifecycle()
    {
        var fixture = await Boot(new Dictionary<string, string?> { [HostSettings.EnvironmentVariable] = "dev" });

        await OnEngine(() => fixture.Mode.OnSignal("ON_MODULE_LOAD_FINISH"));
        fixture.ClearRecord();
        await Frame(fixture);

        fixture.Mode.Trigger.Should().BeNull("with the switch off there is no watcher");
        fixture.Mode.Lifecycle.ModuleLoaded.Should().BeTrue();
        fixture.Mode.Lifecycle.ReloadEnabled.Should().BeFalse();
        fixture.Mode.Services.Resolve(typeof(SWLOR.Game.Contracts.IHostLifecycle)).Should().BeSameAs(fixture.Mode.Lifecycle);
        fixture.Log.Should().NotContain(line => line.StartsWith("reload", StringComparison.Ordinal));
        fixture.Record.Should().Equal($"tick frame={_frame}", $"frame-returned {_frame}");
        Directory.Exists(_layout.ReloadDirectory).Should().BeFalse();
    }

    [Test]
    public async Task A_switch_set_on_a_production_server_is_refused_logged_and_left_off()
    {
        var fixture = await Boot(new Dictionary<string, string?>
        {
            [HostSettings.EnvironmentVariable] = "production",
            [HostSettings.HotReloadVariable] = "true"
        });

        await OnEngine(() => fixture.Mode.OnSignal("ON_MODULE_LOAD_FINISH"));

        fixture.Mode.Trigger.Should().BeNull();
        fixture.Mode.Lifecycle.ReloadEnabled.Should().BeFalse();
        fixture.Log.Should().ContainSingle(line => line.StartsWith("reload", StringComparison.Ordinal))
            .Which.Should().Be("reload-refused SWLOR_HOT_RELOAD is set but SWLOR_ENVIRONMENT is production: production never reloads. Reload stays off.");
        fixture.Mode.ActiveGeneration.Should().Be(1, "the server keeps running with reload off");
        Directory.Exists(_layout.ReloadDirectory).Should().BeFalse();
    }

    [Test]
    public async Task A_reload_needs_the_host_to_have_started()
    {
        var fixture = new HostedFixture(_build, variables: ReloadOn, hostDirectory: _hostDirectory);

        var reload = async () => await fixture.Mode.ReloadAsync(_build, CancellationToken.None);

        await reload.Should().ThrowAsync<InvalidOperationException>().WithMessage("The SWLOR host was not started.");
    }
}
