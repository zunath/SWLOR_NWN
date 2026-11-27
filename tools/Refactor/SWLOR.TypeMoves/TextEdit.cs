namespace SWLOR.TypeMoves;

/// <summary>Replaces <paramref name="Length"/> characters at <paramref name="Start"/> with <paramref name="Replacement"/>.</summary>
internal sealed record TextEdit(int Start, int Length, string Replacement)
{
    public int End => Start + Length;

    /// <summary>Applies edits that do not overlap, last first so earlier positions stay valid. Insertions at one position keep their order and follow a replacement there.</summary>
    public static string Apply(string text, IReadOnlyList<TextEdit> edits)
    {
        if (edits.Count == 0)
            return text;

        var ordered = Ordered(edits);
        var builder = new System.Text.StringBuilder(text);
        foreach (var edit in ordered)
        {
            builder.Remove(edit.Start, edit.Length);
            builder.Insert(edit.Start, edit.Replacement);
        }

        return builder.ToString();
    }

    /// <summary>
    /// The position in the edited text of a position of the original text, or -1 when an edit replaced the character
    /// there. A position where an insertion was made maps after the inserted text.
    /// </summary>
    public static int MapPosition(int position, IReadOnlyList<TextEdit> edits)
    {
        var shift = 0;
        foreach (var edit in edits)
        {
            if (edit.Length == 0)
            {
                if (edit.Start <= position)
                    shift += edit.Replacement.Length;
            }
            else if (edit.End <= position)
            {
                shift += edit.Replacement.Length - edit.Length;
            }
            else if (edit.Start <= position)
            {
                return -1;
            }
        }

        return position + shift;
    }

    /// <summary>
    /// The edits from the end of the text to its start (at one position a replacement before the insertions, so text inserted
    /// where a line is removed survives); throws when two of them overlap.
    /// </summary>
    private static List<TextEdit> Ordered(IReadOnlyList<TextEdit> edits)
    {
        var ordered = edits
            .Select((edit, index) => (edit, index))
            .OrderByDescending(pair => pair.edit.Start)
            .ThenBy(pair => pair.edit.Length > 0 ? 0 : 1)
            .ThenByDescending(pair => pair.index)
            .Select(pair => pair.edit)
            .ToList();

        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].End > ordered[i - 1].Start)
                throw new TypeMoveException($"Two edits overlap at position {ordered[i - 1].Start}.");
        }

        return ordered;
    }
}
