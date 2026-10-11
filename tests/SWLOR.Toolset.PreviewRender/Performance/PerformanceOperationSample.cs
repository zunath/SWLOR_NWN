namespace SWLOR.Toolset.PreviewRender.Performance;

internal sealed record PerformanceOperationSample(
    PerformancePhase Phase,
    PerformanceOperation Operation,
    int Ordinal,
    long StartedElapsedMilliseconds,
    long CompletedElapsedMilliseconds,
    long ElapsedMilliseconds,
    string Scope,
    bool Completed);