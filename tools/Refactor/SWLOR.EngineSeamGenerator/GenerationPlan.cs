namespace SWLOR.EngineSeamGenerator;

/// <summary>
/// What the generator will emit: the wrapper sources in a fixed order, and for every member the original body that
/// moves into the pass-through implementation.
/// </summary>
internal sealed class GenerationPlan
{
    public required IReadOnlyList<WrapperSourceFile> Sources { get; init; }

    /// <summary>The original body text per member key: from the source for members not yet moved, from the previous output otherwise.</summary>
    public required IReadOnlyDictionary<string, string> OriginalBodies { get; init; }

    /// <summary>Keys whose wrapper still held its original body in the source (moved by this run).</summary>
    public required IReadOnlySet<string> MovedThisRun { get; init; }

    /// <summary>Keys the previous pass-through output had but no wrapper has any more (dropped by this run).</summary>
    public required IReadOnlyList<string> Dropped { get; init; }
}
