using System.Globalization;
using System.Text.Json;
using Nwn.Hosting.Contracts.Generations;
using Nwn.Hosting.Reload;

namespace SWLOR.Game.Host.Reload;

/// <summary>
/// Answers the reload signal, the way the package's <c>GameDevelopmentReloadHandler</c> does (an immutable candidate per
/// id, one activation per id), with a receipt per attempt in the reload folder and in the server log: Accepted when the
/// candidate is found, then Rejected with the reason, Applied with the pause, RestartRequired or Failed. The package's
/// <c>GameReloadSignalWatcher</c> calls it each time the signal file changes, one call at a time, off the engine thread.
/// </summary>
public sealed class ReloadSignalHandler
{
    /// <summary>How many times a signal that does not parse is read before it is taken as not a request.</summary>
    private const int SignalReadAttempts = 4;

    /// <summary>The wait between those reads, for a writer that is still writing the file.</summary>
    private static readonly TimeSpan SignalReadRetryDelay = TimeSpan.FromMilliseconds(50);

    private readonly ReloadLayout _layout;
    private readonly Func<string, CancellationToken, Task<ReloadAttempt>> _reloadAsync;
    private readonly Action<string, string> _log;
    private readonly Func<DateTimeOffset> _now;
    private readonly HashSet<Guid> _attempted = [];

    /// <param name="layout">Where the signal, the candidates and the receipts are.</param>
    /// <param name="reloadAsync">Replaces the running build with the candidate assembly at the given path.</param>
    /// <param name="log">The server log: a category and a message.</param>
    public ReloadSignalHandler(ReloadLayout layout, Func<string, CancellationToken, Task<ReloadAttempt>> reloadAsync, Action<string, string> log)
        : this(layout, reloadAsync, log, () => DateTimeOffset.UtcNow)
    {
    }

    /// <summary>For tests: <paramref name="now"/> stamps the receipts.</summary>
    public ReloadSignalHandler(
        ReloadLayout layout,
        Func<string, CancellationToken, Task<ReloadAttempt>> reloadAsync,
        Action<string, string> log,
        Func<DateTimeOffset> now)
    {
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        _reloadAsync = reloadAsync ?? throw new ArgumentNullException(nameof(reloadAsync));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _now = now ?? throw new ArgumentNullException(nameof(now));
    }

    /// <summary>
    /// Reads the signal and runs the attempt it asks for. A signal that is not a request, or names a candidate that was
    /// already attempted (in this process or, by its receipt, an earlier one), is logged and ignored: one candidate is
    /// activated at most once. A missing candidate is rejected without touching the running build.
    /// </summary>
    public async Task HandleAsync(CancellationToken cancellationToken)
    {
        var (valid, request) = await ReadSignalAsync(cancellationToken).ConfigureAwait(false);
        if (!valid)
            return;

        if (request is null)
        {
            _log("reload-signal-ignored", $"{_layout.SignalPath} names no candidate; publish one with SWLOR.CLI --reload.");
            return;
        }

        var generationId = request.GenerationId;
        var receiptPath = _layout.ReceiptPath(generationId);
        if (!_attempted.Add(generationId) || File.Exists(receiptPath))
        {
            _log("reload-signal-ignored", $"generation={ReloadLayout.IdText(generationId)} was already attempted; its receipt is {receiptPath}.");
            return;
        }

        var candidate = _layout.CandidateAssembly(generationId);
        if (!File.Exists(candidate))
        {
            Answer(Receipt(generationId, ReloadReceiptStatus.Rejected,
                $"No candidate at {candidate}: publish the build into {_layout.CandidateDirectory(generationId)} before signalling it. " +
                "The running build was left untouched."));
            return;
        }

        Answer(Receipt(generationId, ReloadReceiptStatus.Accepted, $"Validating {candidate}."));

        ReloadReceipt final;
        try
        {
            var attempt = await _reloadAsync(candidate, cancellationToken).ConfigureAwait(false);
            final = ReceiptFor(generationId, attempt);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            final = Receipt(generationId, ReloadReceiptStatus.Failed, $"{exception.GetType().Name}: {exception.Message}");
        }

        Answer(final);
    }

    /// <summary>
    /// Reads the request, giving a writer that is still writing the file a moment to finish. A file that is still not a
    /// request is logged once and ignored: the watcher calls again only when the file changes, and a handler that threw
    /// would be called (and logged) every poll until then.
    /// </summary>
    private async Task<(bool Valid, DevelopmentReloadRequest? Request)> ReadSignalAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return (true, await DevelopmentReloadFiles.ReadRequestAsync(_layout.SignalPath, cancellationToken).ConfigureAwait(false));
            }
            catch (Exception exception) when (exception is JsonException or InvalidDataException)
            {
                if (attempt >= SignalReadAttempts)
                {
                    _log("reload-signal-invalid", $"{_layout.SignalPath}: {exception.Message}");
                    return (false, null);
                }
            }

            await Task.Delay(SignalReadRetryDelay, cancellationToken).ConfigureAwait(false);
        }
    }

    private ReloadReceipt ReceiptFor(Guid generationId, ReloadAttempt attempt)
    {
        var report = attempt.Report;
        var (status, detail) = report.Outcome switch
        {
            ReloadOutcome.Applied => (ReloadReceiptStatus.Applied, string.Create(CultureInfo.InvariantCulture,
                $"Generation {report.ActiveGeneration} is running the candidate. No engine frame ran from the old build's quiescence " +
                $"to the new build's activation (drain {report.Drain.TotalMilliseconds:F0} ms, dispose and activation " +
                $"{report.ApplyPause.TotalMilliseconds:F0} ms).")),
            ReloadOutcome.RejectedBeforeTeardown => (ReloadReceiptStatus.Rejected, report.Detail),
            ReloadOutcome.RestartRequired => (ReloadReceiptStatus.RestartRequired, "The server needs a restart: " + report.Detail),
            _ => (ReloadReceiptStatus.Failed, $"Unknown reload outcome {report.Outcome}: {report.Detail}")
        };

        return Receipt(generationId, status, detail) with
        {
            ActiveGeneration = report.ActiveGeneration,
            ValidationMilliseconds = report.CandidateValidation.TotalMilliseconds,
            PauseMilliseconds = attempt.Pause?.TotalMilliseconds,
            PausedFrame = attempt.PausedFrame
        };
    }

    private ReloadReceipt Receipt(Guid generationId, ReloadReceiptStatus status, string detail) =>
        new(ReloadReceipt.CurrentFormatVersion, generationId, status, detail, _now());

    /// <summary>
    /// Logs the receipt in full, then writes it with its detail shortened; a receipt that cannot be written is logged,
    /// since the log has it anyway.
    /// </summary>
    private void Answer(ReloadReceipt receipt)
    {
        _log("reload-receipt", ReloadReceiptFiles.Describe(receipt));
        try
        {
            ReloadReceiptFiles.Write(_layout.ReceiptPath(receipt.GenerationId), receipt with { Detail = ReloadReceiptFiles.LimitDetail(receipt.Detail) });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _log("reload-receipt-failure", $"{_layout.ReceiptPath(receipt.GenerationId)}: {exception.Message}");
        }
    }
}
