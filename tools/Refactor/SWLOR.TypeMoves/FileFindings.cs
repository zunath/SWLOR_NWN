namespace SWLOR.TypeMoves;

/// <summary>What the analysis found in one file (the union over every project that compiles it).</summary>
internal sealed class FileFindings
{
    /// <summary>The last segment of a qualifier that named the old namespace, replaced by the new namespace (<c>Service</c> becomes <c>Domain.Foundation</c>).</summary>
    public Dictionary<int, TextEdit> QualifierEdits { get; } = new();

    /// <summary>The namespaces the file must import because it names a moved type by its simple name.</summary>
    public SortedSet<string> Imports { get; } = new(StringComparer.Ordinal);

    /// <summary>A type that stays in the old namespace binds through a using of it in this file, so that using stays.</summary>
    public bool KeepsImport { get; set; }

    /// <summary>A moved file names a type that stays in the old namespace (it saw it from inside), so it must import that namespace.</summary>
    public bool NeedsOldNamespace { get; set; }

    /// <summary>Simple-name references to moved types (for the summary).</summary>
    public int MovedReferences { get; set; }

    /// <summary>Adds what another project's compilation of the same file found.</summary>
    public void Merge(FileFindings other)
    {
        foreach (var (start, edit) in other.QualifierEdits)
            QualifierEdits[start] = edit;
        Imports.UnionWith(other.Imports);
        KeepsImport |= other.KeepsImport;
        NeedsOldNamespace |= other.NeedsOldNamespace;
        MovedReferences += other.MovedReferences;
    }
}
