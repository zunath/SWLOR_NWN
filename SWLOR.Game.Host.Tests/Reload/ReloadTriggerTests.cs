using System.Collections.Concurrent;
using FluentAssertions;
using Nwn.Hosting.Contracts.Generations;
using Nwn.Hosting.Reload;
using NUnit.Framework;
using SWLOR.Game.Host.Reload;

namespace SWLOR.Game.Host.Tests.Reload;

/// <summary>
/// The trigger: the package's signal watcher with SWLOR's handler. Only a signal written while it watches reloads; what
/// the signal file held before it started is never acted on.
/// </summary>
[TestFixture]
public class ReloadTriggerTests
{
    private static readonly TimeSpan Poll = TimeSpan.FromMilliseconds(20);
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(20);

    private string _root = null!;
    private ReloadLayout _layout = null!;
    private ConcurrentQueue<string> _log = null!;
    private ConcurrentQueue<string> _reloaded = null!;

    [SetUp]
    public void CreateHome()
    {
        _root = Path.Combine(Path.GetTempPath(), "swlor-reload-trigger", Guid.NewGuid().ToString("N"));
        _layout = ReloadLayout.ForServerHome(_root);
        _log = new ConcurrentQueue<string>();
        _reloaded = new ConcurrentQueue<string>();
    }

    [TearDown]
    public void DeleteHome()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private ReloadTrigger Trigger() => new(_layout, (candidate, _) =>
    {
        _reloaded.Enqueue(candidate);
        var report = new ReloadReport(ReloadOutcome.Applied, 1, 2, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, true, "activated");
        return Task.FromResult(new ReloadAttempt(report, TimeSpan.FromMilliseconds(3), 10));
    }, (category, message) => _log.Enqueue($"{category} {message}"), Poll);

    private Guid Publish()
    {
        var id = Guid.NewGuid();
        Directory.CreateDirectory(_layout.CandidateDirectory(id));
        File.WriteAllText(_layout.CandidateAssembly(id), "candidate");
        return id;
    }

    private async Task Signal(Guid id)
    {
        Directory.CreateDirectory(_layout.ReloadDirectory);
        var temporary = _layout.SignalPath + ".tmp";
        await DevelopmentReloadFiles.WriteRequestAsync(temporary,
            new DevelopmentReloadRequest(DevelopmentReloadFiles.CurrentFormatVersion, id), CancellationToken.None);
        // A distinct write time, so the watcher sees a change even on a file system with coarse timestamps.
        File.SetLastWriteTimeUtc(temporary, DateTime.UtcNow.AddSeconds(_signals++ + 1));
        File.Move(temporary, _layout.SignalPath, overwrite: true);
    }

    private int _signals;

    private async Task<ReloadReceipt> FinalReceipt(Guid id)
    {
        var deadline = DateTime.UtcNow + Wait;
        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(_layout.ReceiptPath(id)) && ReloadReceiptFiles.Read(_layout.ReceiptPath(id)) is { IsFinal: true } receipt)
                return receipt;
            await Task.Delay(Poll);
        }

        throw new TimeoutException($"No final receipt for {id}. Log: {string.Join(" | ", _log)}");
    }

    [Test]
    public async Task A_signal_written_while_the_trigger_watches_reloads_the_candidate_and_is_answered()
    {
        await using var trigger = Trigger();
        trigger.Start();
        var id = Publish();

        await Signal(id);
        var receipt = await FinalReceipt(id);

        receipt.Status.Should().Be(ReloadReceiptStatus.Applied);
        receipt.PauseMilliseconds.Should().Be(3);
        _reloaded.Should().Equal(_layout.CandidateAssembly(id));
        trigger.Watching.Should().BeTrue();
        _log.Should().Contain($"reload-watching signal={_layout.SignalPath} candidates={_layout.GenerationsDirectory} receipts={_layout.ReceiptsDirectory}");
    }

    [Test]
    public async Task Nothing_watches_before_Start_and_a_signal_present_at_Start_is_never_acted_on()
    {
        var stale = Publish();
        await Signal(stale);
        await using var trigger = Trigger();
        trigger.Watching.Should().BeFalse();

        trigger.Start();
        trigger.Start();
        var fresh = Publish();
        await Signal(fresh);
        await FinalReceipt(fresh);

        _reloaded.Should().Equal(_layout.CandidateAssembly(fresh));
        File.Exists(_layout.ReceiptPath(stale)).Should().BeFalse("a request written before the server watched is not a reload of this server");
        _log.Count(line => line.StartsWith("reload-watching", StringComparison.Ordinal)).Should().Be(1, "a second Start does nothing");
    }

    [Test]
    public async Task A_reload_folder_the_server_cannot_create_keeps_reload_off()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(_layout.ReloadDirectory, "a file where the reload folder should be");
        await using var trigger = Trigger();

        trigger.Start();

        trigger.Watching.Should().BeFalse();
        _log.Should().ContainSingle().Which.Should().StartWith($"reload-off cannot create {_layout.ReceiptsDirectory}");
    }

    [Test]
    public async Task Disposing_stops_the_watching()
    {
        var trigger = Trigger();
        trigger.Start();

        await trigger.DisposeAsync();
        await trigger.DisposeAsync();
        var id = Publish();
        await Signal(id);
        await Task.Delay(Poll * 10);

        trigger.Watching.Should().BeFalse();
        _reloaded.Should().BeEmpty();
    }

    [Test]
    public void The_trigger_needs_a_layout_a_log_and_a_poll_interval()
    {
        Func<string, CancellationToken, Task<ReloadAttempt>> reload = (_, _) => Task.FromResult<ReloadAttempt>(null!);
        Action<string, string> log = (_, _) => { };

        ((Action)(() => new ReloadTrigger(null!, reload, log, Poll))).Should().Throw<ArgumentNullException>();
        ((Action)(() => new ReloadTrigger(_layout, reload, null!, Poll))).Should().Throw<ArgumentNullException>();
        ((Action)(() => new ReloadTrigger(_layout, reload, log, TimeSpan.Zero))).Should().Throw<ArgumentOutOfRangeException>();
        ReloadTrigger.DefaultPollInterval.Should().Be(TimeSpan.FromSeconds(1));
    }
}
