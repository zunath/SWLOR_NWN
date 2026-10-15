namespace SWLOR.Toolset.PreviewRender.Performance;

internal sealed record DispatcherDelaySample(
    PerformancePhase Phase,
    int Ordinal,
    long PostedElapsedMilliseconds,
    long? CompletedElapsedMilliseconds,
    long DelayMilliseconds,
    bool Completed,
    bool TimedOut);