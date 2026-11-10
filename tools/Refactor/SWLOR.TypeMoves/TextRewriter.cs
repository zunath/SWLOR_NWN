using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SWLOR.TypeMoves;

/// <summary>
/// Rewrites the names of the moved types in text the compiler does not bind: guides, scripts, manifests, path lists and
/// JSON, and the comments and string literals of C# files (their code is the semantic rewrite's).
/// <list type="bullet">
/// <item>a full name, <c>SWLOR.Game.Server.Service.Log</c> (also after <c>global::</c>), becomes <c>SWLOR.Game.Server.Domain.Foundation.Log</c>;</item>
/// <item>a name relative to the server, <c>Service.Log</c>, becomes <c>Domain.Foundation.Log</c> (not in string literals, which
/// the code compares or prints);</item>
/// <item>a file path, <c>Service/Log.cs</c> (or with <c>\</c>), becomes <c>Domain/Foundation/Log.cs</c>;</item>
/// <item>a path built from literals, <c>"Service", "Log.cs"</c>, becomes <c>"Domain", "Foundation", "Log.cs"</c>.</item>
/// </list>
/// </summary>
internal sealed class TextRewriter
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview, DocumentationMode.None);

    private readonly TypeMoveData _data;
    private readonly Regex _full;
    private readonly Regex _relative;
    private readonly Regex _path;
    private readonly Regex _literals;

    public TextRewriter(TypeMoveData data)
    {
        _data = data;
        var names = "(?<name>" + string.Join("|", data.Moves.Select(move => move.Name).OrderByDescending(name => name.Length).Select(Regex.Escape)) + ")";
        _byFile = data.Moves.GroupBy(move => Path.GetFileNameWithoutExtension(move.File), StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var files = "(?<file>" + string.Join("|", _byFile.Keys.OrderByDescending(name => name.Length).Select(Regex.Escape)) + ")";
        var leaf = Regex.Escape(TypeMove.Leaf(data.From));
        var folder = TypeMove.FolderOf(data.From)[(TypeMove.ServerRoot.Length + 1)..];
        var folderPattern = string.Join(@"[/\\]{1,2}", folder.Split('/').Select(Regex.Escape));
        _full = new Regex(@"(?<![A-Za-z0-9_.])(?<global>global::)?" + Regex.Escape(data.From) + @"\." + names + "(?![A-Za-z0-9_])", RegexOptions.CultureInvariant);
        _relative = new Regex(@"(?<![A-Za-z0-9_.:])" + leaf + @"\." + names + "(?![A-Za-z0-9_])", RegexOptions.CultureInvariant);
        _path = new Regex(@"(?<![A-Za-z0-9_.])" + folderPattern + @"(?<last>[/\\]{1,2})" + files + @"\.cs(?![A-Za-z0-9_])", RegexOptions.CultureInvariant);
        _literals = new Regex("\"" + leaf + "\"(?<gap>\\s*,\\s*)\"" + files + "\\.cs\"", RegexOptions.CultureInvariant);
    }

    private readonly Dictionary<string, TypeMove> _byFile;

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
                !edits.Any(edit => edit.Start < match.Index + match.Length && match.Index < edit.End))
            {
                edits.Add(new TextEdit(match.Index, match.Length, Literals(match)));
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

    private static string Replace(Regex regex, string text, Func<Match, string> evaluator, ref int count)
    {
        var local = 0;
        var result = regex.Replace(text, match =>
        {
            local++;
            return evaluator(match);
        });
        count += local;
        return result;
    }

    private string Full(Match match) => match.Groups["global"].Value + Move(match).NewFullName;

    private string Relative(Match match) => Move(match).RelativeTarget + "." + match.Groups["name"].Value;

    private string PathPiece(Match match)
    {
        var move = _byFile[match.Groups["file"].Value];
        var separator = match.Groups["last"].Value;
        return TypeMove.FolderOf(move.To)[(TypeMove.ServerRoot.Length + 1)..].Replace("/", separator) + separator + move.File;
    }

    private string Literals(Match match)
    {
        var move = _byFile[match.Groups["file"].Value];
        var gap = match.Groups["gap"].Value;
        var segments = move.RelativeTarget.Split('.');
        return string.Join(gap, segments.Select(segment => "\"" + segment + "\"")) + gap + "\"" + move.File + "\"";
    }

    private TypeMove Move(Match match) => _data.ByName[match.Groups["name"].Value];

    private static bool IsStringToken(SyntaxToken token) => token.Kind() is
        SyntaxKind.StringLiteralToken or SyntaxKind.InterpolatedStringTextToken or SyntaxKind.SingleLineRawStringLiteralToken or
        SyntaxKind.MultiLineRawStringLiteralToken or SyntaxKind.Utf8StringLiteralToken or SyntaxKind.Utf8SingleLineRawStringLiteralToken or
        SyntaxKind.Utf8MultiLineRawStringLiteralToken;

    private static bool IsComment(SyntaxTrivia trivia) => trivia.Kind() is
        SyntaxKind.SingleLineCommentTrivia or SyntaxKind.MultiLineCommentTrivia or SyntaxKind.SingleLineDocumentationCommentTrivia or
        SyntaxKind.MultiLineDocumentationCommentTrivia;
}
