namespace SWLOR.FacadeRemoval;

/// <summary>Replaces <paramref name="Length"/> characters at <paramref name="Start"/> with <paramref name="Replacement"/>.</summary>
internal sealed record TextEdit(int Start, int Length, string Replacement)
{
    /// <summary>Applies edits that do not overlap, last first so earlier positions stay valid. Insertions at one position keep their order.</summary>
    public static string Apply(string text, IReadOnlyList<TextEdit> edits)
    {
        if (edits.Count == 0)
            return text;

        var ordered = edits
            .Select((edit, index) => (edit, index))
            .OrderByDescending(pair => pair.edit.Start)
            .ThenByDescending(pair => pair.index)
            .Select(pair => pair.edit);

        var builder = new System.Text.StringBuilder(text);
        foreach (var edit in ordered)
        {
            builder.Remove(edit.Start, edit.Length);
            builder.Insert(edit.Start, edit.Replacement);
        }

        return builder.ToString();
    }
}
