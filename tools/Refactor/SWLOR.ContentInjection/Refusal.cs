namespace SWLOR.ContentInjection;

/// <summary>Something the tool will not convert and a person must do: where, and why.</summary>
/// <param name="Path">The file, relative to the root, with forward slashes.</param>
/// <param name="Line">The 1-based line, or 0 for the whole file.</param>
/// <param name="Reason">What a person must do.</param>
internal sealed record Refusal(string Path, int Line, string Reason)
{
    public override string ToString() => Line > 0 ? $"{Path}:{Line}: {Reason}" : $"{Path}: {Reason}";
}
