using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SWLOR.Architecture.Tests.Domains;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>
/// Game logic that names content (target 4): code outside <c>Feature</c> (a domain's code, <c>Native</c>,
/// <c>NativeSinks</c>, <c>Core</c>, ...) that names a type declared in <c>Feature</c>, its own domain's content included. Read
/// from source with the compiler's binding, so the result does not depend on the build configuration and a constant (a
/// <c>Feature</c> enum member) counts. The composition root (<c>contentRuleExemptAreas</c> of the domain map) registers content
/// and is left out; the native sinks are wiring for the order but stay under this rule (owner decision 12).
/// </summary>
/// <remarks>
/// Only names of <c>Feature</c> types are bound (in code, attributes and <c>using static</c> directives): a member of a
/// content type reached without naming the type (through a variable or an extension method) is not seen.
/// </remarks>
internal static class LogicToContentScanner
{
    private const string FeatureNamespace = UnitClassifier.RootNamespace + ".Feature";

    public static IReadOnlyList<ContentReference> Scan(SourceSnapshot snapshot, DomainMap map)
    {
        var server = snapshot.Server;
        var compilation = snapshot.CompilationOf(server);
        var contentNames = server.Trees
            .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>())
            .Select(member => member switch
            {
                BaseTypeDeclarationSyntax type when IsContentNamespace(Namespace(type)) => type.Identifier.ValueText,
                DelegateDeclarationSyntax @delegate when IsContentNamespace(Namespace(@delegate)) => @delegate.Identifier.ValueText,
                _ => null
            })
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        var found = new List<ContentReference>();
        foreach (var tree in server.Trees)
        {
            var root = tree.GetRoot();
            var names = root.DescendantNodes().OfType<SimpleNameSyntax>().Where(name => contentNames.Contains(name.Identifier.ValueText)).ToList();
            if (names.Count == 0)
                continue;

            var model = SourceSnapshot.Bind(compilation, tree);
            var referenced = new Dictionary<(DependencyUnit From, DependencyUnit To), SortedSet<string>>();
            foreach (var name in names)
            {
                if (UnitOf(name) is not { } from || IsContent(from) || map.IsContentRuleExempt(from))
                    continue;

                var symbol = model.GetSymbolInfo(name).Symbol;
                var type = (symbol as INamedTypeSymbol ?? symbol?.ContainingType)?.OriginalDefinition;
                if (type is null)
                    continue;

                var outermost = type;
                while (outermost.ContainingType is { } containing)
                    outermost = containing;

                var key = new TypeKey(outermost.ContainingNamespace.ToDisplayString(), outermost.Name);
                if (UnitClassifier.Classify(key) is not { } to || !IsContent(to))
                    continue;

                if (!referenced.TryGetValue((from, to), out var types))
                {
                    types = new SortedSet<string>(StringComparer.Ordinal);
                    referenced[(from, to)] = types;
                }

                types.Add(type.ToDisplayString());
            }

            found.AddRange(referenced.Select(pair => new ContentReference(
                tree.FilePath,
                pair.Key.From,
                pair.Key.To,
                map.DomainOf(pair.Key.From) ?? "(unmapped)",
                map.DomainOf(pair.Key.To) ?? "(unmapped)",
                pair.Value.ToList())));
        }

        return found.OrderBy(reference => reference.Key, StringComparer.Ordinal).ToList();
    }

    public static bool IsContent(DependencyUnit unit) => unit.Kind is UnitKind.FeatureFolder or UnitKind.FeatureType;

    /// <summary>The unit of the code a name is in: the unit of its top-level type's namespace, or of the file's namespace for a <c>using</c> directive.</summary>
    private static DependencyUnit? UnitOf(SyntaxNode node)
    {
        if (SyntaxNames.TopLevelType(node) is { } type)
            return UnitClassifier.Classify(new TypeKey(Namespace(type), type.Identifier.ValueText));

        var root = (CompilationUnitSyntax)node.SyntaxTree.GetRoot();
        var declared = root.DescendantNodes(child => child is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax).OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
        return declared is null ? null : UnitClassifier.Classify(new TypeKey(Namespace(declared), declared.Identifier.ValueText));
    }

    private static string Namespace(SyntaxNode node) =>
        string.Join(".", node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Select(space => space.Name.ToString()).Reverse());

    private static bool IsContentNamespace(string @namespace) =>
        @namespace == FeatureNamespace || @namespace.StartsWith(FeatureNamespace + ".", StringComparison.Ordinal);
}
