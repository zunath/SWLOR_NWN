using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SWLOR.TypeMoves;

/// <summary>
/// Rewrites the names of the moved types in text the compiler does not bind: guides, scripts, manifests, path lists and
/// JSON, and the comments and string literals of C# files (their code is the semantic rewrite's).
/// <list type="bullet">
/// <item>a full name, <c>SWLOR.Game.Server.Service.Log</c> (also after <c>global::</c>), becomes <c>SWLOR.Game.Server.Domain.Foundation.Log</c>;</item>
/// <item>a name relative to the server, <c>Service.Log</c> or <c>Domain.Crafting.RecipeType</c>, becomes <c>Domain.Foundation.Log</c> or
/// <c>Domain.Foundation.RecipeType</c> (not in string literals, which the code compares or prints);</item>
/// <item>a file path, <c>Service/Log.cs</c> (or with <c>\</c>), becomes <c>Domain/Foundation/Log.cs</c>;</item>
/// <item>a path built from literals, <c>"Service", "Log.cs"</c>, becomes <c>"Domain", "Foundation", "Log.cs"</c>.</item>
/// </list>
/// Each type has its own source namespace; a path or literals name a moved file only with that file's own source folder.
/// </summary>
internal sealed class TextRewriter
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview, DocumentationMode.None);
    private static readonly Regex Quoted = new("\"(?<text>[^\"]*)\"", RegexOptions.CultureInvariant);
    private static readonly Regex Separators = new(@"[/\\]+", RegexOptions.CultureInvariant);

    private readonly Dictionary<string, TypeMove> _byOldFullName;
    private readonly Dictionary<string, TypeMove> _byRelativeName;
    private readonly Dictionary<string, TypeMove> _byFile;
    private readonly Regex _full;
    private readonly Regex _relative;
    private readonly Regex _path;
    private readonly Regex _literals;

    public TextRewriter(TypeMoveData data)
    {
        _byOldFullName = data.Moves.ToDictionary(move => move.OldFullName, StringComparer.Ordinal);
        _byRelativeName = data.Moves.ToDictionary(move => TypeMove.BelowRoot(move.From) + "." + move.Name, StringComparer.Ordinal);
        _byFile = data.Moves.GroupBy(FileKey, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var folders = data.Sources.Select(source => TypeMove.BelowRoot(source).Split('.')).ToList();
        var files = "(?<file>" + Alternation(data.Moves.Select(move => Path.GetFileNameWithoutExtension(move.File))) + ")";
        var folderPattern = "(?<folder>" + string.Join("|", folders.OrderByDescending(segments => segments.Length)
            .Select(segments => string.Join(@"[/\\]{1,2}", segments.Select(Regex.Escape)))) + ")";
        var literalPattern = "(?<folder>" + string.Join("|", folders.OrderByDescending(segments => segments.Length)
            .Select(segments => string.Join(@"\s*,\s*", segments.Select(segment => "\"" + Regex.Escape(segment) + "\"")))) + ")";
        _full = new Regex(@"(?<![A-Za-z0-9_.])(?<global>global::)?(?<old>" + Alternation(_byOldFullName.Keys) + ")(?![A-Za-z0-9_])", RegexOptions.CultureInvariant);
        _relative = new Regex(@"(?<![A-Za-z0-9_.:])(?<old>" + Alternation(_byRelativeName.Keys) + ")(?![A-Za-z0-9_])", RegexOptions.CultureInvariant);
        _path = new Regex(@"(?<![A-Za-z0-9_.])" + folderPattern + @"(?<last>[/\\]{1,2})" + files + @"\.cs(?![A-Za-z0-9_])", RegexOptions.CultureInvariant);
        _literals = new Regex(literalPattern + "(?<gap>\\s*,\\s*)\"" + files + "\\.cs\"", RegexOptions.CultureInvariant);
    }

    /// <summary>Rewrites a file that is not C#.</summary>
    public string RewriteText(string text, ref int count)
    {
        var result = Replace(_full, text, Full, ref count);
        result = Replace(_relative, result, Relative, ref count);
        result = Replace(_path, result, PathPiece, ref count);
        return Replace(_literals, result, Literals, ref count);
    }

    /// <summary>Rewrites the comments and string literals of a C# file.</summary>
    public string RewriteCSharp(string text, ref int count)
    {
        if (!_full.IsMatch(text) && !_relative.IsMatch(text) && !_path.IsMatch(text) && !_literals.IsMatch(text))
            return text;

        var root = CSharpSyntaxTree.ParseText(text, ParseOptions).GetRoot();
        var edits = new List<TextEdit>();
        var local = 0;
        foreach (var token in root.DescendantTokens())
        {
            if (IsStringToken(token))
                Collect(text, token.Span, edits, literal: true, ref local);

            foreach (var trivia in token.LeadingTrivia.Concat(token.TrailingTrivia))
            {
                if (IsComment(trivia))
                    Collect(text, trivia.Span, edits, literal: false, ref local);
            }
        }

        // A path built from literals spans two tokens: both pieces must be string literals.
        foreach (Match match in _literals.Matches(text))
        {
            var first = root.FindToken(match.Index);
            var second = root.FindToken(match.Index + match.Length - 1);
            if (IsStringToken(first) && first.SpanStart == match.Index && IsStringToken(second) && second.Span.End == match.Index + match.Length &&
                !edits.Any(edit => edit.Start < match.Index + match.Length && match.Index < edit.End) && Literals(match) is { } replacement)
            {
                edits.Add(new TextEdit(match.Index, match.Length, replacement));
                local++;
            }
        }

        if (edits.Count == 0)
            return text;

        count += local;
        return TextEdit.Apply(text, edits);
    }

    private void Collect(string text, Microsoft.CodeAnalysis.Text.TextSpan span, List<TextEdit> edits, bool literal, ref int count)
    {
        var piece = text.Substring(span.Start, span.Length);
        var local = 0;
        var result = Replace(_full, piece, Full, ref local);
        if (!literal)
            result = Replace(_relative, result, Relative, ref local);
        result = Replace(_path, result, PathPiece, ref local);
        if (local == 0)
            return;

        edits.Add(new TextEdit(span.Start, span.Length, result));
        count += local;
    }

    /// <summary>Replaces each match the evaluator rewrites (null leaves a match as it is) and counts the rewrites.</summary>
    private static string Replace(Regex regex, string text, Func<Match, string?> evaluator, ref int count)
    {
        var local = 0;
        var result = regex.Replace(text, match =>
        {
            var replacement = evaluator(match);
            if (replacement == null)
                return match.Value;

            local++;
            return replacement;
        });
        count += local;
        return result;
    }

    private string Full(Match match) => match.Groups["global"].Value + _byOldFullName[match.Groups["old"].Value].NewFullName;

    private string Relative(Match match)
    {
        var move = _byRelativeName[match.Groups["old"].Value];
        return TypeMove.BelowRoot(move.To) + "." + move.Name;
    }

    /// <summary>The path in the target folder, with the separators of the match; null when the folder is not the file's own source.</summary>
    private string? PathPiece(Match match)
    {
        var folder = string.Join("/", Separators.Split(match.Groups["folder"].Value));
        if (!_byFile.TryGetValue(folder + "/" + match.Groups["file"].Value, out var move))
            return null;

        var separator = match.Groups["last"].Value;
        return TypeMove.BelowRoot(move.To).Replace(".", separator) + separator + move.File;
    }

    /// <summary>The literals of the target folder, with the gap of the match; null when the folder is not the file's own source.</summary>
    private string? Literals(Match match)
    {
        var folder = string.Join("/", Quoted.Matches(match.Groups["folder"].Value).Select(segment => segment.Groups["text"].Value));
        if (!_byFile.TryGetValue(folder + "/" + match.Groups["file"].Value, out var move))
            return null;

        var gap = match.Groups["gap"].Value;
        var segments = TypeMove.BelowRoot(move.To).Split('.');
        return string.Join(gap, segments.Select(segment => "\"" + segment + "\"")) + gap + "\"" + move.File + "\"";
    }

    /// <summary>A moved file by its source folder below the server and its name without the extension (<c>Domain/Crafting/RecipeType</c>).</summary>
    private static string FileKey(TypeMove move) =>
        TypeMove.BelowRoot(move.From).Replace('.', '/') + "/" + Path.GetFileNameWithoutExtension(move.File);

    private static string Alternation(IEnumerable<string> words) =>
        string.Join("|", words.Distinct(StringComparer.Ordinal).OrderByDescending(word => word.Length).ThenBy(word => word, StringComparer.Ordinal).Select(Regex.Escape));

    private static bool IsStringToken(SyntaxToken token) => token.Kind() is
        SyntaxKind.StringLiteralToken or SyntaxKind.InterpolatedStringTextToken or SyntaxKind.SingleLineRawStringLiteralToken or
        SyntaxKind.MultiLineRawStringLiteralToken or SyntaxKind.Utf8StringLiteralToken or SyntaxKind.Utf8SingleLineRawStringLiteralToken or
        SyntaxKind.Utf8MultiLineRawStringLiteralToken;

    private static bool IsComment(SyntaxTrivia trivia) => trivia.Kind() is
        SyntaxKind.SingleLineCommentTrivia or SyntaxKind.MultiLineCommentTrivia or SyntaxKind.SingleLineDocumentationCommentTrivia or
        SyntaxKind.MultiLineDocumentationCommentTrivia;
}
