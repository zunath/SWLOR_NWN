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
/// <item>A bare name the file declares itself (<c>Snippet</c> as a property, <c>DB</c> as a test stub class) is not the facade and stays
/// alone; a qualified call in such a file is written <c>GameServices.Snippet.X</c>.</item>
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
        HashSet<string>? declaredNames = null;

        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (!access.IsKind(SyntaxKind.SimpleMemberAccessExpression) || DottedName(access.Expression) is not { } prefix)
                continue;

            var facade = facades.FirstOrDefault(candidate => Names(candidate.FullName, prefix));
            if (facade == null)
                continue;

            var member = access.Name.Identifier.ValueText;
            var bare = prefix == facade.Name;

            // A bare name the file declares itself (a type, a member, a local, a parameter or an alias of that name) is
            // that declaration, not the removed facade: a toolset view model's Snippet property, a test's DB stub. A
            // qualified call in such a file is written explicitly, because the bare name would mean the declaration.
            declaredNames ??= DeclaredNames(root);
            var shadowed = declaredNames.Contains(facade.Name);
            if (bare && shadowed)
                continue;

            if (facade.Constant(member) is { } constant)
            {
                edits.Add(new TextEdit(access.Expression.SpanStart, access.Expression.Span.Length, constant.OwnerName));
                if (constant.OwnerNamespace.Length > 0)
                    namespaces.Add(constant.OwnerNamespace);
            }
            else if (facade.IsSplit)
            {
                // The member names its owner. An unknown name is not the facade's (a namespace segment that reads the same,
                // or a member master added, which needs a line in the data), and a name with two owners is an overload on
                // each: syntax cannot tell them apart, so the call stays, does not compile, and a person picks the owner.
                var owners = facade.OwnersOf(member);
                if (owners.Count != 1)
                    continue;

                if (owners[0].IsAccessorProperty)
                {
                    edits.Add(new TextEdit(access.Expression.SpanStart, access.Expression.Span.Length, AccessorClass + "." + owners[0].Owner));
                    needsComposition = true;
                }
                else
                {
                    edits.Add(new TextEdit(access.Expression.SpanStart, access.Expression.Span.Length, owners[0].OwnerName));
                    namespaces.Add(owners[0].OwnerNamespace);
                }
            }
            else if (facade.Explicit || shadowed)
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
    /// <param name="text">The file.</param>
    /// <param name="facades">The removed facades.</param>
    /// <param name="declaredNamespaces">
    /// The namespaces the tree the file is reversed against declares (its server sources, enclosing namespaces included).
    /// The import of a branch-only namespace (the accessor's, a moved constant owner's) is dropped only when that tree does
    /// not declare it: once <c>Move-SupportNamespaces.ps1</c> ran on master's parity copy, master declares the
    /// <c>Domain.&lt;Domain&gt;</c> namespaces too, and the file still needs their support types. Null means none.
    /// </param>
    public static RewriteResult Reverse(string text, IReadOnlyList<RemovedFacade> facades, IReadOnlySet<string>? declaredNamespaces = null)
    {
        // A member a split facade handed to a class reads like a moved constant: Owner.Name goes back to Facade.Name.
        var owners = facades
            .SelectMany(facade => facade.Constants.Select(constant => (Facade: facade, Constant: constant))
                .Concat((facade.Members ?? []).Where(member => !member.IsAccessorProperty)
                    .Select(member => (Facade: facade, Constant: new RemovedConstant(member.Name, member.Owner)))))
            .ToList();
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
        bool MissingThere(string @namespace) => declaredNamespaces == null || !declaredNamespaces.Contains(@namespace);

        foreach (var directive in AllUsings(root))
        {
            if (directive.Alias != null || directive.Name == null)
                continue;

            var name = Dotted(directive.Name);
            var drop = directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword)
                ? name == AccessorType
                : (name == AccessorNamespace || branchNamespaces.Contains(name)) && MissingThere(name);
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
            var viaAccessor = facades.FirstOrDefault(facade => prefix.Contains(AccessorClass + ".", StringComparison.Ordinal) &&
                (Names(AccessorType + "." + facade.Name, prefix) || (facade.Members ?? []).Any(candidate => candidate.IsAccessorProperty && Names(AccessorType + "." + candidate.Owner, prefix))));
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

    /// <summary>
    /// Every name the file declares that a bare <c>X.Member</c> could mean instead of a type it imports: types and
    /// delegates, properties, fields, events, locals, parameters (lambdas and primary constructors too), pattern and
    /// query variables, type parameters and using aliases. Scope is ignored, so a file that declares a facade's name
    /// anywhere is left alone for that name.
    /// </summary>
    public static HashSet<string> DeclaredNames(SyntaxNode root)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in root.DescendantNodes())
        {
            var identifier = node switch
            {
                BaseTypeDeclarationSyntax type => type.Identifier,
                DelegateDeclarationSyntax @delegate => @delegate.Identifier,
                PropertyDeclarationSyntax property => property.Identifier,
                EventDeclarationSyntax @event => @event.Identifier,
                VariableDeclaratorSyntax variable => variable.Identifier,
                ParameterSyntax parameter => parameter.Identifier,
                TypeParameterSyntax typeParameter => typeParameter.Identifier,
                SingleVariableDesignationSyntax designation => designation.Identifier,
                ForEachStatementSyntax forEach => forEach.Identifier,
                CatchDeclarationSyntax @catch => @catch.Identifier,
                FromClauseSyntax from => from.Identifier,
                LetClauseSyntax let => let.Identifier,
                JoinClauseSyntax join => join.Identifier,
                JoinIntoClauseSyntax into => into.Identifier,
                QueryContinuationSyntax continuation => continuation.Identifier,
                UsingDirectiveSyntax { Alias: { } alias } => alias.Name.Identifier,
                _ => default
            };

            if (identifier.ValueText.Length > 0)
                names.Add(identifier.ValueText);
        }

        return names;
    }

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
