using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.TypeMoves;

/// <summary>
/// Adds and removes using directives the way the file already writes them. A new namespace goes into the block of plain
/// usings (at the top of the file, or in its first namespace declaration when only that one has usings): at its sorted place
/// when the block is sorted (ordinally, or with <c>System</c> first), else where the removed using of the old namespace was,
/// else after the last <c>SWLOR.Game.Server</c> using, else after the last using. A removed directive takes its line with it,
/// and the blank line after the block when the block becomes empty.
/// </summary>
internal static class UsingEditor
{
    private const string ServerPrefix = TypeMove.ServerRoot + ".";

    public static List<TextEdit> Plan(string text, CompilationUnitSyntax root, IReadOnlyCollection<string> additions, IReadOnlyCollection<UsingDirectiveSyntax> removals)
    {
        var edits = new List<TextEdit>();
        var eol = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var removed = removals.ToHashSet();

        var block = ChooseBlock(root);
        var plain = block.Where(directive => SyntaxQueries.IsPlain(directive) && !SyntaxQueries.IsGlobal(directive)).ToList();
        var kept = plain.Where(directive => !removed.Contains(directive)).ToList();
        var names = plain.Select(directive => SyntaxQueries.Dotted(directive.Name!)).ToList();
        IComparer<string>? order = IsSorted(names, StringComparer.Ordinal) ? StringComparer.Ordinal
            : IsSorted(names, SystemFirst.Instance) ? SystemFirst.Instance : null;

        // Where each new line goes: before a kept directive, after an anchor directive, or in place of a removed one.
        var before = new Dictionary<UsingDirectiveSyntax, List<string>>();
        var after = new Dictionary<UsingDirectiveSyntax, List<string>>();
        var replacing = new Dictionary<UsingDirectiveSyntax, List<string>>();
        var atTop = new List<string>();
        foreach (var @namespace in additions.OrderBy(name => name, order ?? StringComparer.Ordinal))
        {
            if (order != null && kept.FirstOrDefault(directive => order.Compare(SyntaxQueries.Dotted(directive.Name!), @namespace) > 0) is { } next)
                Add(before, next, @namespace);
            else if (order != null && kept.Count > 0)
                Add(after, kept[^1], @namespace);
            else if (plain.FirstOrDefault(directive => removed.Contains(directive)) is { } place && (order == null || kept.Count == 0))
                Add(replacing, place, @namespace);
            else if (kept.LastOrDefault(directive => SyntaxQueries.Dotted(directive.Name!).StartsWith(ServerPrefix, StringComparison.Ordinal)) is { } server)
                Add(after, server, @namespace);
            else if (kept.Count > 0)
                Add(after, kept[^1], @namespace);
            else if (block.Count > 0)
                Add(after, block[^1], @namespace);
            else
                atTop.Add(@namespace);
        }

        foreach (var (directive, lines) in before)
        {
            var start = LineStart(text, directive.SpanStart);
            var indent = text[start..directive.SpanStart];
            edits.Add(new TextEdit(start, 0, string.Concat(lines.Select(line => indent + "using " + line + ";" + eol))));
        }

        foreach (var (directive, lines) in after)
        {
            var indent = Indentation(text, directive.SpanStart);
            var end = LineEnd(text, directive.Span.End);
            var prefix = end == text.Length && !text.EndsWith('\n') ? eol : string.Empty;
            edits.Add(new TextEdit(end, 0, prefix + string.Concat(lines.Select(line => indent + "using " + line + ";" + eol))));
        }

        foreach (var directive in removed)
        {
            var indent = Indentation(text, directive.SpanStart);
            var lines = replacing.TryGetValue(directive, out var replacement)
                ? string.Concat(replacement.Select(line => indent + "using " + line + ";" + eol))
                : string.Empty;
            edits.Add(RemoveLine(text, directive, lines));
        }

        if (atTop.Count > 0)
        {
            var first = (SyntaxNode?)root.AttributeLists.FirstOrDefault() ?? root.Members.FirstOrDefault();
            var at = first == null ? text.Length : LineStart(text, first.SpanStart);
            edits.Add(new TextEdit(at, 0, string.Concat(atTop.Select(line => "using " + line + ";" + eol)) + (first == null ? string.Empty : eol)));
        }

        // A top-level block that loses every directive and gains none also loses the blank line after it.
        var all = root.Usings.ToList();
        if (all.Count > 0 && all.All(removed.Contains) && replacing.Count == 0 && after.Count == 0 && before.Count == 0)
        {
            var end = LineEnd(text, all[^1].Span.End);
            var next = LineEnd(text, end);
            if (end < text.Length && string.IsNullOrWhiteSpace(text[end..next]))
                edits.Add(new TextEdit(end, next - end, string.Empty));
        }

        return edits;
    }

    /// <summary>The using block new lines go into: the top of the file, else the first namespace declaration that has usings.</summary>
    private static IReadOnlyList<UsingDirectiveSyntax> ChooseBlock(CompilationUnitSyntax root)
    {
        if (root.Usings.Any(directive => !SyntaxQueries.IsGlobal(directive)))
            return root.Usings;

        var inner = root.DescendantNodes(node => node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
            .OfType<BaseNamespaceDeclarationSyntax>()
            .FirstOrDefault(declaration => declaration.Usings.Count > 0);
        return inner != null ? inner.Usings : root.Usings;
    }

    private static void Add(Dictionary<UsingDirectiveSyntax, List<string>> map, UsingDirectiveSyntax key, string value)
    {
        if (!map.TryGetValue(key, out var list))
            map[key] = list = new List<string>();
        list.Add(value);
    }

    private static bool IsSorted(IReadOnlyList<string> names, IComparer<string> comparer)
    {
        for (var i = 1; i < names.Count; i++)
        {
            if (comparer.Compare(names[i - 1], names[i]) > 0)
                return false;
        }

        return names.Count > 1;
    }

    /// <summary>Removes a directive with its line when it stands alone on it (putting <paramref name="replacement"/> there), else just the directive.</summary>
    private static TextEdit RemoveLine(string text, UsingDirectiveSyntax directive, string replacement)
    {
        var start = LineStart(text, directive.SpanStart);
        var end = LineEnd(text, directive.Span.End);
        if (string.IsNullOrWhiteSpace(text[start..directive.SpanStart]) && string.IsNullOrWhiteSpace(text[directive.Span.End..end]))
            return new TextEdit(start, end - start, replacement);

        return new TextEdit(directive.SpanStart, directive.Span.Length, replacement.TrimStart().TrimEnd('\r', '\n'));
    }

    public static int LineStart(string text, int position)
    {
        var newline = position == 0 ? -1 : text.LastIndexOf('\n', position - 1);
        return newline + 1;
    }

    /// <summary>The position after the end of line that follows <paramref name="position"/>, or the end of the text.</summary>
    public static int LineEnd(string text, int position)
    {
        var newline = text.IndexOf('\n', position);
        return newline < 0 ? text.Length : newline + 1;
    }

    private static string Indentation(string text, int position)
    {
        var start = LineStart(text, position);
        var length = 0;
        while (start + length < position && (text[start + length] == ' ' || text[start + length] == '\t'))
            length++;
        return text.Substring(start, length);
    }

    /// <summary>Ordinal order with <c>System</c> and <c>System.*</c> first (the editors' "place System directives first").</summary>
    private sealed class SystemFirst : IComparer<string>
    {
        public static readonly SystemFirst Instance = new();

        public int Compare(string? x, string? y)
        {
            var systemX = IsSystem(x!);
            var systemY = IsSystem(y!);
            if (systemX != systemY)
                return systemX ? -1 : 1;
            return StringComparer.Ordinal.Compare(x, y);
        }

        private static bool IsSystem(string name) => name == "System" || name.StartsWith("System.", StringComparison.Ordinal);
    }
}
