using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.FacadeRemoval;

/// <summary>
/// Rewrites one C# file's calls into removed facades, and back. Works on the syntax tree, so comments, strings and
/// disabled code are never touched, and changes only the text it must: a call keeps its text when the imported
/// <c>GameServices</c> property serves it.
/// <list type="bullet">
/// <item><c>Taxi.X</c> stays as it is; the file gains <c>using static SWLOR.Game.Server.Composition.GameServices;</c>
/// (or its project's global usings do, when they import <c>SWLOR.Game.Server.Service</c>).</item>
/// <item><c>Service.Taxi.X</c> and <c>SWLOR.Game.Server.Service.Taxi.X</c> become <c>Taxi.X</c>.</item>
/// <item>An explicit name (<c>Combat</c>) becomes <c>GameServices.Combat.X</c>, with <c>using SWLOR.Game.Server.Composition;</c>.</item>
/// <item>A moved constant <c>Combat.MaximumHitRate</c> becomes <c>CombatRules.MaximumHitRate</c>, with a using of the owner's namespace.</item>
/// </list>
/// <see cref="Reverse"/> undoes this for a master-shaped file: it drops those using lines (the ones naming a namespace
/// master does not have), turns <c>GameServices.Taxi.X</c> back into <c>Taxi.X</c> and moved constants back onto their
/// facade, so the result compiles against master. A qualified name cannot be restored, so reversal is exact for files
/// that called the facades by their simple names.
/// </summary>
internal static class CallerRewriter
{
    public const string AccessorNamespace = "SWLOR.Game.Server.Composition";
    public const string AccessorClass = "GameServices";
    public const string AccessorType = AccessorNamespace + "." + AccessorClass;
    public const string UsingStatic = "using static " + AccessorType + ";";
    public const string GlobalUsingStatic = "global " + UsingStatic;

    private const string BranchOnlyNamespace = "SWLOR.Game.Server.Domain";

    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview, DocumentationMode.None);

    public static SyntaxNode Parse(string text) => CSharpSyntaxTree.ParseText(text, ParseOptions).GetRoot();

    /// <param name="text">The file.</param>
    /// <param name="facades">The facades removed in the tree the file belongs to.</param>
    /// <param name="importedByProject">True when the file's project imports the <c>GameServices</c> properties globally.</param>
    public static RewriteResult Apply(string text, IReadOnlyList<RemovedFacade> facades, bool importedByProject)
    {
        if (!facades.Any(facade => text.Contains(facade.Name, StringComparison.Ordinal)))
            return new RewriteResult(text, 0, 0, false);

        var root = Parse(text);
        var edits = new List<TextEdit>();
        var namespaces = new SortedSet<string>(StringComparer.Ordinal);
        var needsComposition = false;
        var needsAccessorImport = false;

        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (!access.IsKind(SyntaxKind.SimpleMemberAccessExpression) || DottedName(access.Expression) is not { } prefix)
                continue;

            var facade = facades.FirstOrDefault(candidate => Names(candidate.FullName, prefix));
            if (facade == null)
                continue;

            var member = access.Name.Identifier.ValueText;
            var bare = prefix == facade.Name;
            if (facade.Constant(member) is { } constant)
            {
                edits.Add(new TextEdit(access.Expression.SpanStart, access.Expression.Span.Length, constant.OwnerName));
                if (constant.OwnerNamespace.Length > 0)
                    namespaces.Add(constant.OwnerNamespace);
            }
            else if (facade.Explicit)
            {
                edits.Add(new TextEdit(access.Expression.SpanStart, access.Expression.Span.Length, AccessorClass + "." + facade.Name));
                needsComposition = true;
            }
            else
            {
                if (!bare)
                    edits.Add(new TextEdit(access.Expression.SpanStart, access.Expression.Span.Length, facade.Name));
                needsAccessorImport = true;
            }
        }

        if (edits.Count == 0 && !needsAccessorImport)
            return new RewriteResult(text, 0, 0, false);

        var usings = AllUsings(root).ToList();
        var declared = DeclaredNamespaces(root);
        var lines = new List<string>();
        if (needsComposition && !HasPlainUsing(usings, AccessorNamespace) && !IsInside(declared, AccessorNamespace))
            lines.Add("using " + AccessorNamespace + ";");
        foreach (var @namespace in namespaces)
        {
            if (!HasPlainUsing(usings, @namespace) && !IsInside(declared, @namespace))
                lines.Add("using " + @namespace + ";");
        }

        if (needsAccessorImport && !importedByProject && !usings.Any(IsAccessorImport))
            lines.Add(UsingStatic);

        if (lines.Count > 0)
            edits.Add(InsertUsings(text, root, usings, lines));

        var rewrites = edits.Count - (lines.Count > 0 ? 1 : 0);
        return new RewriteResult(TextEdit.Apply(text, edits), lines.Count, rewrites, needsAccessorImport);
    }

    /// <summary>Undoes <see cref="Apply"/> on a file written for the branch, so it compiles against master.</summary>
    public static RewriteResult Reverse(string text, IReadOnlyList<RemovedFacade> facades)
    {
        var owners = facades.SelectMany(facade => facade.Constants.Select(constant => (Facade: facade, Constant: constant))).ToList();
        if (!text.Contains(AccessorClass, StringComparison.Ordinal) &&
            !text.Contains(AccessorNamespace, StringComparison.Ordinal) &&
            !owners.Any(owner => text.Contains(owner.Constant.OwnerName, StringComparison.Ordinal)))
        {
            return new RewriteResult(text, 0, 0, false);
        }

        var root = Parse(text);
        var edits = new List<TextEdit>();
        var removed = 0;
        var branchNamespaces = owners
            .Select(owner => owner.Constant.OwnerNamespace)
            .Where(IsBranchOnly)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var directive in AllUsings(root))
        {
            if (directive.Alias != null || directive.Name == null)
                continue;

            var name = Dotted(directive.Name);
            var drop = directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword)
                ? name == AccessorType
                : name == AccessorNamespace || branchNamespaces.Contains(name);
            if (!drop)
                continue;

            edits.Add(RemoveLine(text, directive));
            removed++;
        }

        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (!access.IsKind(SyntaxKind.SimpleMemberAccessExpression) || DottedName(access.Expression) is not { } prefix)
                continue;

            var member = access.Name.Identifier.ValueText;
            var viaAccessor = facades.FirstOrDefault(facade => Names(AccessorType + "." + facade.Name, prefix) && prefix.Contains(AccessorClass + ".", StringComparison.Ordinal));
            if (viaAccessor != null)
            {
                edits.Add(new TextEdit(access.Expression.SpanStart, access.Expression.Span.Length, viaAccessor.Name));
                continue;
            }

            var moved = owners.FirstOrDefault(owner => owner.Constant.Name == member && Names(owner.Constant.Owner, prefix));
            if (moved.Facade != null)
                edits.Add(new TextEdit(access.Expression.SpanStart, access.Expression.Span.Length, moved.Facade.Name));
        }

        if (edits.Count == 0)
            return new RewriteResult(text, 0, 0, false);

        return new RewriteResult(TextEdit.Apply(text, edits), removed, edits.Count - removed, false);
    }

    /// <summary>
    /// True when <paramref name="prefix"/> (the dotted text before a member) names the type <paramref name="fullName"/>: the
    /// full name, or a trailing part of it (<c>Taxi</c>, <c>Service.Taxi</c>), without <c>global::</c>.
    /// </summary>
    public static bool Names(string fullName, string prefix) =>
        fullName == prefix || fullName.EndsWith("." + prefix, StringComparison.Ordinal);

    /// <summary>The dotted text of a name made only of identifiers (<c>Taxi</c>, <c>SWLOR.Game.Server.Service.Taxi</c>, <c>global::X.Y</c>), or null.</summary>
    public static string? DottedName(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression, Name: IdentifierNameSyntax name } access
            when DottedName(access.Expression) is { } left => left + "." + name.Identifier.ValueText,
        AliasQualifiedNameSyntax { Alias.Identifier.ValueText: "global", Name: IdentifierNameSyntax name } => name.Identifier.ValueText,
        _ => null
    };

    private static string Dotted(NameSyntax name) => name switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        QualifiedNameSyntax qualified => Dotted(qualified.Left) + "." + qualified.Right.Identifier.ValueText,
        AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
        _ => name.ToString()
    };

    private static bool IsBranchOnly(string @namespace) =>
        @namespace == AccessorNamespace || @namespace == BranchOnlyNamespace || @namespace.StartsWith(BranchOnlyNamespace + ".", StringComparison.Ordinal);

    private static IEnumerable<UsingDirectiveSyntax> AllUsings(SyntaxNode root) =>
        root.DescendantNodes(node => node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax).OfType<UsingDirectiveSyntax>();

    private static bool IsAccessorImport(UsingDirectiveSyntax directive) =>
        directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) && directive.Name != null && Dotted(directive.Name) == AccessorType;

    private static bool HasPlainUsing(IEnumerable<UsingDirectiveSyntax> usings, string @namespace) =>
        usings.Any(directive => !directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) && directive.Alias == null &&
                                directive.Name != null && Dotted(directive.Name) == @namespace);

    private static IReadOnlyList<string> DeclaredNamespaces(SyntaxNode root)
    {
        var result = new List<string>();
        foreach (var declaration in root.DescendantNodes(node => node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax).OfType<BaseNamespaceDeclarationSyntax>())
        {
            var parts = declaration.AncestorsAndSelf().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(ns => Dotted(ns.Name));
            result.Add(string.Join(".", parts));
        }

        return result;
    }

    // A namespace's members are in scope inside it and inside every namespace nested in it.
    private static bool IsInside(IEnumerable<string> declared, string @namespace) =>
        declared.Any(name => name == @namespace || name.StartsWith(@namespace + ".", StringComparison.Ordinal));

    /// <summary>
    /// One edit that inserts the new using lines after the file's last top-level using, else after the last using of
    /// its first namespace that has any, else on the line of its first member.
    /// </summary>
    private static TextEdit InsertUsings(string text, SyntaxNode root, IReadOnlyList<UsingDirectiveSyntax> usings, IReadOnlyList<string> lines)
    {
        var eol = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var unit = (CompilationUnitSyntax)root;
        var anchor = unit.Usings.LastOrDefault();
        if (anchor == null && usings.FirstOrDefault(directive => directive.Parent is BaseNamespaceDeclarationSyntax)?.Parent is BaseNamespaceDeclarationSyntax owner)
            anchor = owner.Usings.Last();

        if (anchor != null)
        {
            var indent = Indentation(text, anchor.SpanStart);
            var end = LineEnd(text, anchor.Span.End);

            // Code after the last using on its own line (`using System; class C`): the new lines go right after the directive.
            var next = anchor.GetLastToken().GetNextToken();
            if (next.RawKind != 0 && next.SpanStart < end)
                return new TextEdit(anchor.Span.End, 0, string.Concat(lines.Select(line => eol + indent + line)));

            var prefix = end == text.Length && !text.EndsWith('\n') ? eol : string.Empty;
            return new TextEdit(end, 0, prefix + string.Concat(lines.Select(line => indent + line + eol)));
        }

        var first = (SyntaxNode?)unit.AttributeLists.FirstOrDefault() ?? unit.Members.FirstOrDefault();
        var at = first == null ? text.Length : LineStart(text, first.SpanStart);
        return new TextEdit(at, 0, string.Concat(lines.Select(line => line + eol)));
    }

    /// <summary>Removes a using directive with its line when it stands alone on it, else just the directive.</summary>
    private static TextEdit RemoveLine(string text, UsingDirectiveSyntax directive)
    {
        var start = LineStart(text, directive.SpanStart);
        var end = LineEnd(text, directive.Span.End);
        var before = text[start..directive.SpanStart];
        var after = text[directive.Span.End..end];
        if (string.IsNullOrWhiteSpace(before) && string.IsNullOrWhiteSpace(after))
            return new TextEdit(start, end - start, string.Empty);

        return new TextEdit(directive.SpanStart, directive.Span.Length, string.Empty);
    }

    private static int LineStart(string text, int position)
    {
        var newline = position == 0 ? -1 : text.LastIndexOf('\n', position - 1);
        return newline + 1;
    }

    /// <summary>The position after the end of line that follows <paramref name="position"/>, or the end of the text.</summary>
    private static int LineEnd(string text, int position)
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
}
