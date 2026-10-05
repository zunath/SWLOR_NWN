using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Threading;

namespace SWLOR.Toolset.PreviewRender.Performance;

internal sealed class ToolsetPerformanceObserver : IAsyncDisposable
{
    private static readonly TimeSpan SampleInterval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan DispatcherTimeout = TimeSpan.FromSeconds(2);
    private readonly string _runRoot;
    private readonly string _scope;
    private readonly DateTimeOffset _processStartedUtc;
    private readonly long _startedAtTimestamp = Stopwatch.GetTimestamp();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly CancellationTokenSource _stop = new();
    private readonly Process _process = Process.GetCurrentProcess();
    private readonly object _gate = new();
    private readonly List<PerformanceOperationSample> _operations = new();
    private readonly List<DispatcherDelaySample> _dispatcherSamples = new();
    private readonly List<PerformancePhaseSample> _phaseSamples = new();
    private readonly List<(Task<long> Completion, PerformancePhase Phase, int Ordinal, long PostedAt, long PostedElapsedMilliseconds)> _lateCompletions = new();
    private readonly Task _sampler;
    private int _phase = (int)PerformancePhase.Startup;
    private int _operationOrdinal;
    private int _phaseOrdinal;
    private long _phaseStartedAt = Stopwatch.GetTimestamp();
    private long _posted;
    private long _completed;
    private long _timedOut;
    private long _privateBytesPeak;
    private string? _modelOpenGlVendor;
    private string? _modelOpenGlRenderer;
    private string? _modelOpenGlVersion;
    private bool _stopped;

    public ToolsetPerformanceObserver(string runRoot, string scope)
    {
        _runRoot = Path.GetFullPath(runRoot);
        _scope = scope;
        _processStartedUtc = DateTimeOffset.TryParse(
            Environment.GetEnvironmentVariable("SWLOR_PERFORMANCE_PROCESS_STARTED_UTC"), out var started)
                ? started
                : DateTimeOffset.UtcNow;
        _sampler = SampleAsync(_stop.Token);
    }

    public void SetPhase(PerformancePhase phase)
    {
        lock (_gate)
        {
            var previous = (PerformancePhase)_phase;
            if (previous == phase)
                return;
            var endedAt = Stopwatch.GetTimestamp();
            _phaseSamples.Add(new PerformancePhaseSample(
                previous,
                Interlocked.Increment(ref _phaseOrdinal),
                ElapsedMilliseconds(_phaseStartedAt),
                ElapsedMilliseconds(endedAt),
                checked((long)Math.Ceiling(Stopwatch.GetElapsedTime(_phaseStartedAt, endedAt).TotalMilliseconds))));
            _phaseStartedAt = endedAt;
            Volatile.Write(ref _phase, (int)phase);
        }
    }

    public long Record(PerformancePhase phase, PerformanceOperation operation, Stopwatch stopwatch, string scope, bool completed = true)
    {
        var elapsed = stopwatch.ElapsedMilliseconds;
        var completedAt = Stopwatch.GetTimestamp();
        var startedAt = completedAt - stopwatch.ElapsedTicks;
        lock (_gate)
        {
            _operations.Add(new PerformanceOperationSample(
                phase,
                operation,
                Interlocked.Increment(ref _operationOrdinal),
                ElapsedMilliseconds(startedAt),
                ElapsedMilliseconds(completedAt),
                elapsed,
                scope,
                completed));
        }
        return elapsed;
    }

    public async Task SetPhaseAndDrainAsync(PerformancePhase phase)
    {
        SetPhase(phase);
        var drained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(() => drained.TrySetResult(), DispatcherPriority.Default);
        await drained.Task.WaitAsync(DispatcherTimeout);
    }

    public void RecordModelViewportIdentity(string vendor, string renderer, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vendor);
        ArgumentException.ThrowIfNullOrWhiteSpace(renderer);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        lock (_gate)
        {
            if (_modelOpenGlVendor is not null
                && (_modelOpenGlVendor != vendor || _modelOpenGlRenderer != renderer || _modelOpenGlVersion != version))
                throw new InvalidDataException("The real model viewport changed OpenGL identity within one performance run.");
            _modelOpenGlVendor = vendor;
            _modelOpenGlRenderer = renderer;
            _modelOpenGlVersion = version;
        }
    }

    public void WriteReady(string areaResRef, int width, int height, int tileCount, int instanceCount, int completedFrameCount)
    {
        var readyUtc = DateTimeOffset.UtcNow;
        var ready = new
        {
            Schema = "swlor.desktop-performance-ready.v1",
            Scope = _scope,
            AreaResRef = areaResRef,
            AreaWidth = width,
            AreaHeight = height,
            TileCount = tileCount,
            InstanceCount = instanceCount,
            CompletedCurrentSceneViewportFrames = completedFrameCount,
            ProcessStartedUtc = _processStartedUtc,
            WindowReadyUtc = readyUtc,
            ProcessStartToReadyMilliseconds = Math.Max(0, (long)(readyUtc - _processStartedUtc).TotalMilliseconds),
            Readiness = "Production MainWindow, populated Area Contents, completed current-scene viewport frames",
            CorrectnessOnly = true,
            PerformanceBudgetQualified = false
        };
        WriteNew("performance-ready.json", ready);
        SetPhase(PerformancePhase.Interactive);
    }

    public async Task StopAndWriteAsync()
    {
        if (_stopped)
            return;
        _stopped = true;
        lock (_gate)
        {
            var endedAt = Stopwatch.GetTimestamp();
            _phaseSamples.Add(new PerformancePhaseSample(
                (PerformancePhase)_phase,
                Interlocked.Increment(ref _phaseOrdinal),
                ElapsedMilliseconds(_phaseStartedAt),
                ElapsedMilliseconds(endedAt),
                checked((long)Math.Ceiling(Stopwatch.GetElapsedTime(_phaseStartedAt, endedAt).TotalMilliseconds))));
        }
        _stop.Cancel();
        try
        {
            await _sampler.WaitAsync(DispatcherTimeout + SampleInterval);
        }
        catch (TimeoutException)
        {
            Interlocked.Increment(ref _timedOut);
        }

        await DrainLateCompletionsAsync();
        _process.Refresh();
        UpdatePeak(_process.PrivateMemorySize64);
        PerformanceOperationSample[] operations;
        DispatcherDelaySample[] samples;
        PerformancePhaseSample[] phases;
        string? modelOpenGlVendor;
        string? modelOpenGlRenderer;
        string? modelOpenGlVersion;
        lock (_gate)
        {
            operations = _operations.ToArray();
            samples = _dispatcherSamples.ToArray();
            phases = _phaseSamples.ToArray();
            modelOpenGlVendor = _modelOpenGlVendor;
            modelOpenGlRenderer = _modelOpenGlRenderer;
            modelOpenGlVersion = _modelOpenGlVersion;
        }
        var completedSamples = samples.Where(item => item.Completed).Select(item => item.DelayMilliseconds).Order().ToArray();
        var p95Rank = Math.Max(1, (int)Math.Ceiling(completedSamples.Length * 0.95));
        var report = new
        {
            Schema = "swlor.desktop-performance-observations.v1",
            Scope = _scope,
            ProcessStartedUtc = _processStartedUtc,
            ProcessEndedUtc = DateTimeOffset.UtcNow,
            ProcessElapsedMilliseconds = _clock.ElapsedMilliseconds,
            PerformanceBudgetQualified = false,
            Phases = phases,
            DispatcherPhaseCoverage = Enum.GetValues<PerformancePhase>().ToDictionary(phase => phase, phase =>
                {
                    var phaseSamples = samples.Where(sample => sample.Phase == phase).ToArray();
                    return new
                    {
                        Posted = phaseSamples.Length,
                        Completed = phaseSamples.Count(sample => sample.Completed),
                        TimedOut = phaseSamples.Count(sample => sample.TimedOut)
                    };
                }),
            OperationCoverage = Enum.GetValues<PerformanceOperation>().ToDictionary(operation => operation, operation =>
            {
                var operationSamples = operations.Where(sample => sample.Operation == operation).ToArray();
                return new
                {
                    Count = operationSamples.Length,
                    Completed = operationSamples.Count(sample => sample.Completed),
                    Failed = operationSamples.Count(sample => !sample.Completed)
                };
            }),
            Dispatcher = new
            {
                PostedCount = Interlocked.Read(ref _posted),
                CompletedCount = Interlocked.Read(ref _completed),
                TimedOutCount = Interlocked.Read(ref _timedOut),
                Samples = samples,
                P95Milliseconds = completedSamples.Length == 0 ? (long?)null : completedSamples[p95Rank - 1],
                MaximumMilliseconds = completedSamples.Length == 0 ? (long?)null : completedSamples[^1],
                CoverageComplete = Interlocked.Read(ref _posted) == Interlocked.Read(ref _completed)
                    && Interlocked.Read(ref _timedOut) == 0
            },
            Memory = new
            {
                ObservedPrivateBytesPeak = Interlocked.Read(ref _privateBytesPeak),
                OperatingSystemPeakWorkingSetBytes = _process.PeakWorkingSet64,
                FinalPrivateBytes = _process.PrivateMemorySize64
            },
            ModelViewport = new
            {
                OpenGlVendor = modelOpenGlVendor,
                OpenGlRenderer = modelOpenGlRenderer,
                OpenGlVersion = modelOpenGlVersion,
                FirstSelection = OperationSummary(PerformanceOperation.FirstRenderedModelSelection),
                CachedSelections = OperationSummary(PerformanceOperation.CachedRenderedModelSelection)
            },
            Operations = operations
        };
        WriteNew("performance-observations.json", report);
        _process.Dispose();
        _stop.Dispose();
    }

    public async ValueTask DisposeAsync() => await StopAndWriteAsync();

    private async Task SampleAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(SampleInterval);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                    return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _process.Refresh();
            UpdatePeak(_process.PrivateMemorySize64);
            PerformancePhase phase;
            int ordinal;
            long postedAt;
            long postedElapsedMilliseconds;
            TaskCompletionSource<long> completion;
            lock (_gate)
            {
                phase = (PerformancePhase)_phase;
                if (phase is not (PerformancePhase.Interactive or PerformancePhase.Save or PerformancePhase.Pack or PerformancePhase.Reopen))
                    continue;

                ordinal = checked((int)Interlocked.Increment(ref _posted));
                postedAt = Stopwatch.GetTimestamp();
                postedElapsedMilliseconds = ElapsedMilliseconds(postedAt);
                completion = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
                Dispatcher.UIThread.Post(() => completion.TrySetResult(Stopwatch.GetTimestamp()), DispatcherPriority.Default);
            }
            try
            {
                var completedAt = await completion.Task.WaitAsync(DispatcherTimeout, cancellationToken).ConfigureAwait(false);
                RecordDelay(phase, ordinal, postedAt, postedElapsedMilliseconds, completedAt);
            }
            catch (TimeoutException)
            {
                Interlocked.Increment(ref _timedOut);
                lock (_gate)
                    _lateCompletions.Add((completion.Task, phase, ordinal, postedAt, postedElapsedMilliseconds));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await DrainPostedAsync(completion, phase, ordinal, postedAt, postedElapsedMilliseconds).ConfigureAwait(false);
                return;
            }
        }
    }

    private async Task DrainPostedAsync(
        TaskCompletionSource<long> completion,
        PerformancePhase phase,
        int ordinal,
        long postedAt,
        long postedElapsedMilliseconds)
    {
        try
        {
            var completedAt = await completion.Task.WaitAsync(DispatcherTimeout).ConfigureAwait(false);
            RecordDelay(phase, ordinal, postedAt, postedElapsedMilliseconds, completedAt);
        }
        catch (TimeoutException)
        {
            Interlocked.Increment(ref _timedOut);
            lock (_gate)
                _lateCompletions.Add((completion.Task, phase, ordinal, postedAt, postedElapsedMilliseconds));
        }
    }

    private async Task DrainLateCompletionsAsync()
    {
        (Task<long> Completion, PerformancePhase Phase, int Ordinal, long PostedAt, long PostedElapsedMilliseconds)[] pending;
        lock (_gate)
            pending = _lateCompletions.ToArray();
        var drains = pending.Select(async item =>
        {
            try
            {
                var completedAt = await item.Completion.WaitAsync(DispatcherTimeout).ConfigureAwait(false);
                RecordDelay(item.Phase, item.Ordinal, item.PostedAt, item.PostedElapsedMilliseconds, completedAt, timedOut: true);
            }
            catch (TimeoutException)
            {
                lock (_gate)
                    _dispatcherSamples.Add(new DispatcherDelaySample(
                        item.Phase,
                        item.Ordinal,
                        item.PostedElapsedMilliseconds,
                        null,
                        (long)DispatcherTimeout.TotalMilliseconds,
                        false,
                        true));
            }
        });
        await Task.WhenAll(drains).ConfigureAwait(false);
    }

    private void RecordDelay(
        PerformancePhase phase,
        int ordinal,
        long postedAt,
        long postedElapsedMilliseconds,
        long completedAt,
        bool timedOut = false)
    {
        var delay = (long)Math.Ceiling(Stopwatch.GetElapsedTime(postedAt, completedAt).TotalMilliseconds);
        Interlocked.Increment(ref _completed);
        lock (_gate)
            _dispatcherSamples.Add(new DispatcherDelaySample(
                phase,
                ordinal,
                postedElapsedMilliseconds,
                ElapsedMilliseconds(completedAt),
                delay,
                true,
                timedOut));
    }

    private long ElapsedMilliseconds(long timestamp) =>
        checked((long)Math.Floor(Stopwatch.GetElapsedTime(_startedAtTimestamp, timestamp).TotalMilliseconds));

    private void UpdatePeak(long bytes)
    {
        var current = Interlocked.Read(ref _privateBytesPeak);
        while (bytes > current)
        {
            var previous = Interlocked.CompareExchange(ref _privateBytesPeak, bytes, current);
            if (previous == current)
                return;
            current = previous;
        }
    }

    private void WriteNew(string fileName, object value)
    {
        var path = Path.Combine(_runRoot, fileName);
        if (File.Exists(path))
            throw new IOException($"Refusing to overwrite performance evidence: {path}");
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        var temporaryPath = path + ".tmp-" + Guid.NewGuid().ToString("N");
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(value, options));
        File.Move(temporaryPath, path);
    }

    private object OperationSummary(PerformanceOperation operation)
    {
        PerformanceOperationSample[] observations;
        lock (_gate)
            observations = _operations.Where(sample => sample.Operation == operation).ToArray();
        var ordered = observations.Where(sample => sample.Completed).Select(sample => sample.ElapsedMilliseconds).Order().ToArray();
        var p95Rank = Math.Max(1, (int)Math.Ceiling(ordered.Length * 0.95));
        return new
        {
            Count = observations.Length,
            Completed = observations.Count(sample => sample.Completed),
            P95Milliseconds = ordered.Length == 0 ? (long?)null : ordered[p95Rank - 1],
            MaximumMilliseconds = ordered.Length == 0 ? (long?)null : ordered[^1],
            Samples = observations,
            Metric = "real production model preview selection through a newer exact-scene completed OpenGL draw; no compositor-presentation claim"
        };
    }
}
