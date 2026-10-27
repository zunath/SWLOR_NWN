namespace SWLOR.EngineSeamGenerator;

/// <summary>A hand-written wrapper source file: its text, using directives, members and constants.</summary>
internal sealed class WrapperSourceFile
{
    /// <summary>The path relative to the repository root, with '/' separators.</summary>
    public required string Path { get; init; }

    /// <summary>The file text as read, with its own line endings.</summary>
    public required string Text { get; init; }

    /// <summary>The line ending the file mostly uses; new lines written into the file use it.</summary>
    public required string Eol { get; init; }

    /// <summary>The compilation-unit using directives exactly as written, in order.</summary>
    public required IReadOnlyList<string> Usings { get; init; }

    public required IReadOnlyList<WrapperMember> Members { get; init; }

    /// <summary>Public constants. They cannot sit behind an interface, so they stay static.</summary>
    public required IReadOnlyList<string> Constants { get; init; }
}
