using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.TypeMoves;

/// <summary>Small questions about C# syntax the analysis, the edits and the check share.</summary>
internal static class SyntaxQueries
{
    /// <summary>The dotted text of a name (<c>SWLOR.Game.Server.Service</c>, <c>global::X</c> as <c>X</c>).</summary>
    public static string Dotted(NameSyntax name) => name switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        QualifiedNameSyntax qualified => Dotted(qualified.Left) + "." + qualified.Right.Identifier.ValueText,
        AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
        GenericNameSyntax generic => generic.Identifier.ValueText,
        _ => name.ToString()
    };

    /// <summary>Every simple name of the file, those in documentation comments (<c>cref</c>) included.</summary>
    public static IEnumerable<SimpleNameSyntax> SimpleNames(SyntaxNode root) =>
        root.DescendantNodes(descendIntoTrivia: true).OfType<SimpleNameSyntax>();

    /// <summary>Every using directive of the file: the compilation unit's and those of each namespace declaration.</summary>
    public static IEnumerable<UsingDirectiveSyntax> AllUsings(SyntaxNode root) =>
        root.DescendantNodes(node => node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax).OfType<UsingDirectiveSyntax>();

    /// <summary>A plain namespace import: no <c>static</c>, no alias.</summary>
    public static bool IsPlain(UsingDirectiveSyntax directive) =>
        !directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) && directive.Alias == null && directive.Name != null;

    public static bool IsGlobal(UsingDirectiveSyntax directive) => directive.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword);

    /// <summary>The full name of a namespace declaration, the enclosing declarations included.</summary>
    public static string FullName(BaseNamespaceDeclarationSyntax declaration) =>
        string.Join(".", declaration.AncestorsAndSelf().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(ns => Dotted(ns.Name)));

    /// <summary>The namespaces the file declares, in order.</summary>
    public static IReadOnlyList<string> DeclaredNamespaces(SyntaxNode root) =>
        root.DescendantNodes(node => node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Select(FullName)
            .ToList();

    /// <summary>The innermost namespace declaration around a node, as a full name; empty in the global namespace.</summary>
    public static string NamespaceOf(SyntaxNode node) =>
        node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault() is { } declaration ? FullName(declaration) : string.Empty;

    /// <summary>True when <paramref name="inner"/> is <paramref name="outer"/> or a namespace below it.</summary>
    public static bool IsWithin(string inner, string outer) =>
        inner == outer || inner.StartsWith(outer + ".", StringComparison.Ordinal);

    /// <summary>True when the node is part of the name of a namespace declaration.</summary>
    public static bool InNamespaceName(SyntaxNode node) =>
        node.AncestorsAndSelf().Any(ancestor => ancestor.Parent is BaseNamespaceDeclarationSyntax declaration && declaration.Name == ancestor);

    /// <summary>
    /// The qualifier of a name that is the right side of a qualified name, a member access, an alias-qualified name or a
    /// qualified <c>cref</c> (<c>Service</c> in <c>Service.Log</c> and in <c>&lt;see cref="Service.Log"/&gt;</c>), or null for a
    /// name that stands first.
    /// </summary>
    public static SyntaxNode? Qualifier(SimpleNameSyntax name) => name.Parent switch
    {
        QualifiedNameSyntax qualified when qualified.Right == name => qualified.Left,
        MemberAccessExpressionSyntax access when access.Name == name => access.Expression,
        MemberBindingExpressionSyntax binding when binding.Name == name => binding,
        AliasQualifiedNameSyntax alias when alias.Name == name => alias.Alias,
        NameMemberCrefSyntax { Parent: QualifiedCrefSyntax qualifiedCref } member when member.Name == name && qualifiedCref.Member == member => qualifiedCref.Container,
        _ => null
    };

    /// <summary>
    /// The identifiers of a dotted qualifier, left to right (<c>SWLOR</c>, <c>Game</c>, <c>Server</c>, <c>Service</c> for
    /// <c>global::SWLOR.Game.Server.Service</c>), or null when it is not a plain dotted name (an expression, an extern alias).
    /// </summary>
    public static List<SyntaxToken>? Segments(SyntaxNode qualifier)
    {
        switch (qualifier)
        {
            case IdentifierNameSyntax identifier:
                return [identifier.Identifier];
            case AliasQualifiedNameSyntax alias when alias.Alias.Identifier.IsKind(SyntaxKind.GlobalKeyword) && alias.Name is IdentifierNameSyntax:
                return [alias.Name.Identifier];
            case QualifiedNameSyntax { Right: IdentifierNameSyntax right } qualified when Segments(qualified.Left) is { } left:
                left.Add(right.Identifier);
                return left;
            case MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression, Name: IdentifierNameSyntax name } access when Segments(access.Expression) is { } head:
                head.Add(name.Identifier);
                return head;
            default:
                return null;
        }
    }

    /// <summary>
    /// True when a using of <paramref name="namespace"/> written in the file is in scope at the node: at the top of the file
    /// or in a namespace declaration around it (a <c>global using</c> counts for the whole project, not here).
    /// </summary>
    public static bool HasFileImport(SyntaxNode node, string @namespace)
    {
        var root = (CompilationUnitSyntax)node.SyntaxTree.GetRoot();
        var scopes = node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Select(declaration => declaration.Usings)
            .Append(root.Usings);
        return scopes.Any(usings => usings.Any(directive => IsPlain(directive) && !IsGlobal(directive) && Dotted(directive.Name!) == @namespace));
    }
}
