namespace SWLOR.TypeMoves;

/// <summary>What the analysis found in one file (the union over every project that compiles it).</summary>
internal sealed class FileFindings
{
    /// <summary>
    /// The segments of a qualifier that spelled a source namespace, replaced by the target (<c>Service</c> becomes
    /// <c>Domain.Foundation</c>, <c>Crafting</c> becomes <c>Foundation</c>), by the position of the replaced text.
    /// </summary>
    public Dictionary<int, TextEdit> QualifierEdits { get; } = new();

    /// <summary>
    /// For each moved type's name whose qualifier was rewritten (by the name's position), the whole qualifier: the check
    /// writes the full namespace there when the shorter text would bind to something else.
    /// </summary>
    public Dictionary<int, QualifierSpan> Qualifiers { get; } = new();

    /// <summary>The namespaces the file must import because it names a moved type (or calls its extension method) by its simple name.</summary>
    public SortedSet<string> Imports { get; } = new(StringComparer.Ordinal);

    /// <summary>The source namespaces a type that stays there binds through by a using written in this file: those usings stay.</summary>
    public SortedSet<string> KeptImports { get; } = new(StringComparer.Ordinal);

    /// <summary>The source namespaces a moved file names a staying type of (it saw them from inside): it must import them.</summary>
    public SortedSet<string> NeededSources { get; } = new(StringComparer.Ordinal);

    /// <summary>Simple-name references to moved types (for the summary).</summary>
    public int MovedReferences { get; set; }

    /// <summary>Adds what another project's compilation of the same file found.</summary>
    public void Merge(FileFindings other)
    {
        foreach (var (start, edit) in other.QualifierEdits)
            QualifierEdits[start] = edit;
        foreach (var (position, span) in other.Qualifiers)
            Qualifiers[position] = span;
        Imports.UnionWith(other.Imports);
        KeptImports.UnionWith(other.KeptImports);
        NeededSources.UnionWith(other.NeededSources);
        MovedReferences += other.MovedReferences;
    }
}

