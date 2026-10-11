namespace SWLOR.Toolset.PreviewRender.Performance;

internal sealed record PerformancePhaseSample(
    PerformancePhase Phase,
    int Ordinal,
    long StartedElapsedMilliseconds,
    long EndedElapsedMilliseconds,
    long ElapsedMilliseconds);
