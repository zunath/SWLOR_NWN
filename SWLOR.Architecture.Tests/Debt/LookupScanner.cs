using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>
/// Finds every use of the static service lookup (<c>GameServices</c> and <c>FacadeAccessor</c>) in code, by binding names
/// the way the compiler does: a call written <c>Stat.GetLevel(...)</c> under <c>using static ...GameServices</c> (or a
/// project's <c>global using static</c>) counts; comments, XML documentation and strings do not.
/// </summary>
/// <remarks>
/// Each use of a member is one site; the type name is a site of its own only where no member follows it
/// (<c>typeof(GameServices)</c>, <c>nameof(GameServices)</c>); a <c>using static</c> or alias directive that names the type is
/// one site, so a file stays listed until it no longer names the lookup (deleting it would not compile otherwise). The
/// lookup's own types (<c>GameServices</c>, <c>FacadeAccessor</c>, <c>FacadeCache</c>) are the lookup itself, not uses of it.
/// </remarks>
internal static class LookupScanner
{
    public const string GameServices = "SWLOR.Game.Server.Composition.GameServices";
    public const string FacadeAccessor = "SWLOR.Game.Server.Composition.FacadeAccessor";

    private const string FacadeCache = "SWLOR.Game.Server.Composition.FacadeCache";

    private static readonly string[] LookupTypes = [GameServices, FacadeAccessor];

    /// <summary>The lookup's own types: names inside them are not uses.</summary>
    private static readonly string[] OwnTypes = [GameServices, FacadeAccessor, FacadeCache];

    public static IReadOnlyList<LookupSite> Scan(SourceSnapshot snapshot) =>
        snapshot.Projects
            .AsParallel()
            .AsOrdered()
            .SelectMany(project => Scan(snapshot, project))
            .ToList();

    public static IReadOnlyList<LookupSite> Scan(SourceSnapshot snapshot, ParsedProject project)
    {
        var importsEverywhere = project.Trees.Any(ImportsLookupGlobally);
        var candidates = project.Trees
            .Where(tree => importsEverywhere || MentionsLookup(tree))
            .ToList();
        if (candidates.Count == 0)
            return [];

        var compilation = snapshot.CompilationOf(project);
        var lookupTypes = LookupTypes
            .Select(name => compilation.GetTypeByMetadataName(name))
            .OfType<INamedTypeSymbol>()
            .ToList();
        if (lookupTypes.Count == 0)
            return [];

        var names = lookupTypes
            .SelectMany(type => type.GetMembers().Select(member => member.Name).Append(type.Name))
            .ToHashSet(StringComparer.Ordinal);

        var sites = new List<LookupSite>();
        foreach (var tree in candidates)
        {
            var model = SourceSnapshot.Bind(compilation, tree);
            foreach (var directive in tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>())
            {
                // using static X.GameServices; and using Alias = X.GameServices; name the type. A namespace import does not.
                if (directive.Name is null || directive.StaticKeyword.IsKind(SyntaxKind.None) && directive.Alias is null)
                    continue;

                var info = model.GetSymbolInfo(directive.Name);
                if (info.Symbol is INamedTypeSymbol imported && IsLookupType(imported))
                    sites.Add(new LookupSite(project.Name, tree.FilePath, null, "using " + imported.Name, null, IsStaticContext: false, IsImport: true));
            }

            foreach (var name in tree.GetRoot().DescendantNodes().OfType<SimpleNameSyntax>())
            {
                if (!names.Contains(name.Identifier.ValueText) || name.Ancestors().Any(node => node is UsingDirectiveSyntax))
                    continue;

                var topLevel = SyntaxNames.TopLevelType(name);
                var typeName = topLevel is null ? null : SyntaxNames.FullName(topLevel);
                if (typeName != null && OwnTypes.Any(own => IsNamed(typeName, own)))
                    continue;

                var info = model.GetSymbolInfo(name);
                var symbol = info.Symbol;
                var site = Site(symbol, name);
                if (site is { } found)
                    sites.Add(new LookupSite(project.Name, tree.FilePath, typeName, found.Member, found.Service, IsStaticContext(name)));
            }
        }

        return sites;
    }

    /// <summary>True when code at <paramref name="node"/> runs without an instance: a static member, local function or lambda.</summary>
    public static bool IsStaticContext(SyntaxNode node)
    {
        for (var current = node.Parent; current != null; current = current.Parent)
        {
            switch (current)
            {
                case LocalFunctionStatementSyntax local when local.Modifiers.Any(SyntaxKind.StaticKeyword):
                    return true;
                case AnonymousFunctionExpressionSyntax lambda when lambda.Modifiers.Any(SyntaxKind.StaticKeyword):
                    return true;
                case BaseTypeDeclarationSyntax type:
                    // A field initializer or attribute argument of a type without a member around it: static only in a static class.
                    return type.Modifiers.Any(SyntaxKind.StaticKeyword);
                case MemberDeclarationSyntax member and not BaseTypeDeclarationSyntax:
                    return member.Modifiers.Any(SyntaxKind.StaticKeyword) || member.Modifiers.Any(SyntaxKind.ConstKeyword);
            }
        }

        // Top-level statements.
        return true;
    }

    private static (string Member, string? Service)? Site(ISymbol? symbol, SimpleNameSyntax name)
    {
        switch (symbol)
        {
            case null:
                return null;

            case INamedTypeSymbol type when IsLookupType(type):
                // The member after it is the site; the type name alone is one only without a member (typeof, nameof). A
                // namespace-qualified name (SWLOR.Game.Server.Composition.GameServices.Stat) is the whole qualified expression.
                SyntaxNode written = name.Parent switch
                {
                    MemberAccessExpressionSyntax outer when outer.Name == name => outer,
                    QualifiedNameSyntax outer when outer.Right == name => outer,
                    _ => name
                };
                var qualifies = written.Parent is MemberAccessExpressionSyntax access && access.Expression == written
                                || written.Parent is QualifiedNameSyntax qualified && qualified.Left == written;
                return qualifies ? null : (type.Name, null);

            case { ContainingType: { } owner } when IsLookupType(owner.OriginalDefinition):
                var service = symbol switch
                {
                    IPropertySymbol property => property.Type.ToDisplayString(),
                    IMethodSymbol { TypeArguments.Length: 1 } method => method.TypeArguments[0].ToDisplayString(),
                    _ => null
                };
                return ($"{owner.Name}.{symbol.Name}", service);

            default:
                return null;
        }
    }

    private static bool IsLookupType(INamedTypeSymbol type)
    {
        var name = type.ToDisplayString();
        return LookupTypes.Contains(name, StringComparer.Ordinal);
    }

    private static bool IsNamed(string typeName, string fullName) =>
        typeName == fullName || typeName.StartsWith(fullName + "<", StringComparison.Ordinal);

    private static bool MentionsLookup(SyntaxTree tree)
    {
        var text = tree.GetText().ToString();
        return text.Contains("GameServices", StringComparison.Ordinal) || text.Contains("FacadeAccessor", StringComparison.Ordinal);
    }

    private static bool ImportsLookupGlobally(SyntaxTree tree) =>
        tree.GetRoot() is CompilationUnitSyntax unit
        && unit.Usings.Any(directive => directive.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword)
                                        && directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword)
                                        && directive.Name?.ToString().EndsWith("GameServices", StringComparison.Ordinal) == true);
}
