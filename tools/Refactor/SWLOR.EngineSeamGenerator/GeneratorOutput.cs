namespace SWLOR.EngineSeamGenerator;

/// <summary>Everything one generator run produced in memory, plus what it found on the way.</summary>
internal sealed class GeneratorOutput
{
    /// <summary>Every file the generator owns after the run: path to content. Wrapper files appear with their rewritten text.</summary>
    public Dictionary<string, string> Files { get; } = new(StringComparer.Ordinal);

    /// <summary>Folders that hold only generated parts; a file there that is not in <see cref="Files"/> is stale.</summary>
    public List<string> GeneratedRoots { get; } = new();

    /// <summary>Wrapper files whose text the run changed (or would change).</summary>
    public List<string> RewrittenWrappers { get; } = new();

    /// <summary>Problems that stop the run before anything is written.</summary>
    public List<string> Errors { get; } = new();

    /// <summary>Counts and notes for the person running the tool.</summary>
    public List<string> Report { get; } = new();
}
