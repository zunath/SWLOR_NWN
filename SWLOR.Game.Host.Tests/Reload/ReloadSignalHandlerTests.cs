using System.Collections.Concurrent;
using FluentAssertions;
using Nwn.Hosting.Contracts.Generations;
using Nwn.Hosting.Reload;
using NUnit.Framework;
using SWLOR.Game.Host.Reload;

namespace SWLOR.Game.Host.Tests.Reload;

/// <summary>
/// The signal and receipt flow: a signal naming a published candidate is accepted, then answered with the outcome
/// (applied with the pause, rejected with the reason, restart required, failed), in the receipt file and in the server log.
/// A candidate is attempted at most once; anything that is not a request is logged and ignored.
/// </summary>
[TestFixture]
public class ReloadSignalHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);

    private string _root = null!;
    private ReloadLayout _layout = null!;
    private ConcurrentQueue<string> _log = null!;
    private List<string> _reloaded = null!;
    private List<(string Status, string Receipt)> _receiptsSeen = null!;

    [SetUp]
    public void CreateHome()
    {
        _root = Path.Combine(Path.GetTempPath(), "swlor-reload-signal", Guid.NewGuid().ToString("N"));
        _layout = ReloadLayout.ForServerHome(_root);
        _log = new ConcurrentQueue<string>();
        _reloaded = [];
        _receiptsSeen = [];
    }

    [TearDown]
    public void DeleteHome()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private static ReloadReport Report(ReloadOutcome outcome, string detail, long? active) =>
        new(outcome, 1, active, TimeSpan.FromMilliseconds(1500), TimeSpan.FromMilliseconds(3), TimeSpan.FromMilliseconds(700),
            TimeSpan.Zero, true, detail);

    private ReloadSignalHandler Handler(Func<string, ReloadAttempt> reload) =>
        new(_layout, (candidate, _) =>
        {
            _reloaded.Add(candidate);
            // What a developer's tool polling the receipt sees while the reload runs.
            var receipt = ReloadReceiptFiles.Read(_layout.ReceiptPath(IdOf(candidate)))!;
            _receiptsSeen.Add((receipt.Status.ToString(), receipt.Detail));
            return Task.FromResult(reload(candidate));
        }, (category, message) => _log.Enqueue($"{category} {message}"), () => Now);

    private static Guid IdOf(string candidate) => Guid.ParseExact(Path.GetFileName(Path.GetDirectoryName(candidate))!, "N");

    private Guid Publish()
    {
        var id = Guid.NewGuid();
        Directory.CreateDirectory(_layout.CandidateDirectory(id));
        File.WriteAllText(_layout.CandidateAssembly(id), "candidate");
        return id;
    }

    private async Task Signal(Guid id) =>
        await WriteSignal(_layout, new DevelopmentReloadRequest(DevelopmentReloadFiles.CurrentFormatVersion, id));

    private static async Task WriteSignal(ReloadLayout layout, DevelopmentReloadRequest request)
    {
        Directory.CreateDirectory(layout.ReloadDirectory);
        await DevelopmentReloadFiles.WriteRequestAsync(layout.SignalPath, request, CancellationToken.None);
    }

    [Test]
    public async Task An_applied_candidate_is_accepted_then_answered_with_the_pause()
    {
        var id = Publish();
        await Signal(id);
        var handler = Handler(_ => new ReloadAttempt(Report(ReloadOutcome.Applied, "activated", 2), TimeSpan.FromMilliseconds(712), 4242));

        await handler.HandleAsync(CancellationToken.None);

        _reloaded.Should().Equal(_layout.CandidateAssembly(id));
        _receiptsSeen.Should().ContainSingle().Which.Status.Should().Be("Accepted", "the receipt says Accepted while the reload runs");
        var receipt = ReloadReceiptFiles.Read(_layout.ReceiptPath(id))!;
        receipt.Status.Should().Be(ReloadReceiptStatus.Applied);
        receipt.GenerationId.Should().Be(id);
        receipt.ActiveGeneration.Should().Be(2);
        receipt.PauseMilliseconds.Should().Be(712);
        receipt.PausedFrame.Should().Be(4242);
        receipt.ValidationMilliseconds.Should().Be(1500);
        receipt.WrittenUtc.Should().Be(Now);
        receipt.Detail.Should().Contain("Generation 2 is running the candidate").And.Contain("drain 3 ms").And.Contain("activation 700 ms");
        _log.Should().ContainInOrder(
            $"reload-receipt generation={ReloadLayout.IdText(id)} status=Accepted: Validating {_layout.CandidateAssembly(id)}.",
            $"reload-receipt {ReloadReceiptFiles.Describe(receipt)}");
    }

    [Test]
    public async Task A_rejected_candidate_is_answered_with_the_reason()
    {
        var id = Publish();
        await Signal(id);
        var handler = Handler(_ => new ReloadAttempt(
            Report(ReloadOutcome.RejectedBeforeTeardown, "Candidate validation failed; the running generation was left active: boom", 1), null, null));

        await handler.HandleAsync(CancellationToken.None);

        var receipt = ReloadReceiptFiles.Read(_layout.ReceiptPath(id))!;
        receipt.Status.Should().Be(ReloadReceiptStatus.Rejected);
        receipt.Detail.Should().Be("Candidate validation failed; the running generation was left active: boom");
        receipt.ActiveGeneration.Should().Be(1, "the old build keeps running");
        receipt.PauseMilliseconds.Should().BeNull("a rejected candidate never holds the engine thread");
        _log.Should().Contain(line => line.Contains("status=Rejected") && line.EndsWith("boom", StringComparison.Ordinal));
    }

    [Test]
    public async Task A_failure_after_quiescence_says_the_server_needs_a_restart()
    {
        var id = Publish();
        await Signal(id);
        var handler = Handler(_ => new ReloadAttempt(Report(ReloadOutcome.RestartRequired, "Failure after teardown", null), TimeSpan.FromMilliseconds(5), 7));

        await handler.HandleAsync(CancellationToken.None);

        var receipt = ReloadReceiptFiles.Read(_layout.ReceiptPath(id))!;
        receipt.Status.Should().Be(ReloadReceiptStatus.RestartRequired);
        receipt.Detail.Should().Be("The server needs a restart: Failure after teardown");
        receipt.ActiveGeneration.Should().BeNull();
        receipt.PauseMilliseconds.Should().Be(5);
    }

    [Test]
    public async Task A_reload_that_throws_or_reports_an_unknown_outcome_is_answered_as_failed()
    {
        var thrown = Publish();
        await Signal(thrown);
        await Handler(_ => throw new InvalidOperationException("host stopped")).HandleAsync(CancellationToken.None);

        var unknown = Publish();
        await Signal(unknown);
        await Handler(_ => new ReloadAttempt(Report((ReloadOutcome)42, "odd", 1), null, null)).HandleAsync(CancellationToken.None);

        var failed = ReloadReceiptFiles.Read(_layout.ReceiptPath(thrown))!;
        failed.Status.Should().Be(ReloadReceiptStatus.Failed);
        failed.Detail.Should().Be("InvalidOperationException: host stopped");
        var odd = ReloadReceiptFiles.Read(_layout.ReceiptPath(unknown))!;
        odd.Status.Should().Be(ReloadReceiptStatus.Failed);
        odd.Detail.Should().Be("Unknown reload outcome 42: odd");
    }

    [Test]
    public async Task A_signal_for_a_candidate_that_was_never_published_is_rejected_without_a_reload()
    {
        var id = Guid.NewGuid();
        await Signal(id);
        var handler = Handler(_ => throw new AssertionException("no reload"));

        await handler.HandleAsync(CancellationToken.None);

        _reloaded.Should().BeEmpty();
        var receipt = ReloadReceiptFiles.Read(_layout.ReceiptPath(id))!;
        receipt.Status.Should().Be(ReloadReceiptStatus.Rejected);
        receipt.Detail.Should().Contain($"No candidate at {_layout.CandidateAssembly(id)}").And.Contain("left untouched");
    }

    [Test]
    public async Task A_candidate_is_attempted_at_most_once_in_this_process_or_after_a_restart()
    {
        var id = Publish();
        await Signal(id);
        var handler = Handler(_ => new ReloadAttempt(Report(ReloadOutcome.Applied, "activated", 2), TimeSpan.Zero, 1));
        await handler.HandleAsync(CancellationToken.None);

        // The same request again, to this handler and to the handler of a restarted server (a receipt is on disk).
        await handler.HandleAsync(CancellationToken.None);
        await Handler(_ => throw new AssertionException("no second activation")).HandleAsync(CancellationToken.None);

        _reloaded.Should().ContainSingle();
        _log.Where(line => line.StartsWith("reload-signal-ignored", StringComparison.Ordinal)).Should().HaveCount(2)
            .And.AllSatisfy(line => line.Should().Contain("was already attempted"));
    }

    [TestCase("initial")]
    [TestCase("2026-11-12T10:00:00.0000000+00:00")]
    public async Task A_signal_that_names_no_candidate_is_ignored(string marker)
    {
        Directory.CreateDirectory(_layout.ReloadDirectory);
        await File.WriteAllTextAsync(_layout.SignalPath, marker);

        await Handler(_ => throw new AssertionException("no reload")).HandleAsync(CancellationToken.None);

        _reloaded.Should().BeEmpty();
        _log.Should().ContainSingle().Which.Should().StartWith("reload-signal-ignored").And.Contain("names no candidate");
    }

    [TestCase("{not json")]
    [TestCase("{\"FormatVersion\":9,\"GenerationId\":\"0f8fad5b-d9cb-469f-a165-70867728950e\"}")]
    public async Task A_signal_that_is_not_a_request_is_logged_once_without_a_reload(string text)
    {
        Directory.CreateDirectory(_layout.ReloadDirectory);
        await File.WriteAllTextAsync(_layout.SignalPath, text);

        var handle = async () => await Handler(_ => throw new AssertionException("no reload")).HandleAsync(CancellationToken.None);

        await handle.Should().NotThrowAsync("the watcher would retry a throwing handler every poll until the file changes");
        _log.Should().ContainSingle().Which.Should().StartWith("reload-signal-invalid " + _layout.SignalPath);
    }

    [Test]
    public async Task A_signal_still_being_written_is_read_again_until_it_is_whole()
    {
        var id = Publish();
        Directory.CreateDirectory(_layout.ReloadDirectory);
        await File.WriteAllTextAsync(_layout.SignalPath, "{\"FormatVersion\":1,");
        var handler = Handler(_ => new ReloadAttempt(Report(ReloadOutcome.Applied, "activated", 2), TimeSpan.Zero, 1));

        var handling = handler.HandleAsync(CancellationToken.None);
        await Signal(id);
        await handling;

        _reloaded.Should().Equal(_layout.CandidateAssembly(id));
        _log.Should().NotContain(line => line.StartsWith("reload-signal-invalid", StringComparison.Ordinal));
    }

    [Test]
    public async Task A_receipt_that_cannot_be_written_is_logged_and_the_attempt_goes_on()
    {
        var id = Publish();
        await Signal(id);
        // A file where the receipts folder should be: every receipt write fails.
        File.WriteAllText(_layout.ReceiptsDirectory, "in the way");
        var handler = new ReloadSignalHandler(_layout,
            (candidate, _) =>
            {
                _reloaded.Add(candidate);
                return Task.FromResult(new ReloadAttempt(Report(ReloadOutcome.Applied, "activated", 2), TimeSpan.Zero, 1));
            },
            (category, message) => _log.Enqueue($"{category} {message}"));

        await handler.HandleAsync(CancellationToken.None);

        _reloaded.Should().ContainSingle();
        _log.Where(line => line.StartsWith("reload-receipt-failure", StringComparison.Ordinal)).Should().HaveCount(2);
        _log.Should().Contain(line => line.StartsWith("reload-receipt ", StringComparison.Ordinal) && line.Contains("status=Applied"));
    }

    [Test]
    public async Task A_long_detail_is_logged_whole_and_shortened_in_the_receipt()
    {
        var id = Publish();
        await Signal(id);
        var detail = new string('d', ReloadReceiptFiles.MaximumDetailLength * 2);

        await Handler(_ => new ReloadAttempt(Report(ReloadOutcome.RejectedBeforeTeardown, detail, 1), null, null)).HandleAsync(CancellationToken.None);

        ReloadReceiptFiles.Read(_layout.ReceiptPath(id))!.Detail.Should().HaveLength(ReloadReceiptFiles.MaximumDetailLength + 3);
        _log.Should().Contain(line => line.EndsWith(detail, StringComparison.Ordinal));
    }

    [Test]
    public void The_handler_needs_its_layout_reload_log_and_clock()
    {
        Func<string, CancellationToken, Task<ReloadAttempt>> reload = (_, _) => Task.FromResult<ReloadAttempt>(null!);
        Action<string, string> log = (_, _) => { };

        ((Action)(() => new ReloadSignalHandler(null!, reload, log))).Should().Throw<ArgumentNullException>();
        ((Action)(() => new ReloadSignalHandler(_layout, null!, log))).Should().Throw<ArgumentNullException>();
        ((Action)(() => new ReloadSignalHandler(_layout, reload, null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => new ReloadSignalHandler(_layout, reload, log, null!))).Should().Throw<ArgumentNullException>();
    }
}
