using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SWLOR.Architecture.Tests.Domains;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>
/// Every source member of the server that names a type of another unit, bound the way the compiler binds it (P5-1b). It is
/// the reference set of the domain order measures (pairs by band, upward references), the same one the P5-1 measuring
/// tool records (<c>tools/Refactor/DomainOrder</c>), so the guard and the tool count alike. Read from source, it does not
/// depend on the build configuration, and it sees what the compiler inlines (constants, enum members, <c>nameof</c>).
/// </summary>
/// <remarks>
/// <para>A reference is recorded where a name binds to a server type or to a member of one: the type itself (a type name that
/// only qualifies a member is covered by the member), the containing type of a method (an extension method's static class),
/// field, property or event, the type arguments of a generic method or type it uses, and the types the compiler infers
/// without a name: the type of a <c>var</c>, a target-typed <c>new()</c>, an indexer, a lambda's parameters and return type
/// and a <c>foreach</c> element. Names in <c>using</c> directives are not references. A member is the innermost method,
/// constructor, destructor, property, indexer, event, field, operator, conversion or enum member around the name, with its
/// lambdas and local functions.</para>
/// <para>Only references between two different units are kept: inside one unit nothing is measured.</para>
/// </remarks>
internal static class SourceReferenceScanner
{
    public static IReadOnlyList<SourceReference> Scan(SourceSnapshot snapshot)
    {
        var compilation = snapshot.CompilationOf(snapshot.Server);
        var found = new ConcurrentBag<IReadOnlyCollection<SourceReference>>();
        Parallel.ForEach(snapshot.Server.Trees, tree => found.Add(ScanTree(compilation, tree)));

        return found
            .SelectMany(references => references)
            .OrderBy(reference => reference.File, StringComparer.Ordinal)
            .ThenBy(reference => reference.Member, StringComparer.Ordinal)
            .ThenBy(reference => reference.To.ToString(), StringComparer.Ordinal)
            .ThenBy(reference => reference.Target, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyCollection<SourceReference> ScanTree(CSharpCompilation compilation, SyntaxTree tree)
    {
        var collector = new Collector(compilation.Assembly, SourceSnapshot.Bind(compilation, tree), tree.FilePath);
        foreach (var node in tree.GetRoot().DescendantNodes())
        {
            switch (node)
            {
                case SimpleNameSyntax name:
                    collector.Name(name);
                    break;
                case ImplicitObjectCreationExpressionSyntax creation:
                    if (collector.Model.GetSymbolInfo(creation).Symbol is IMethodSymbol constructor)
                        collector.Add(node, constructor.ContainingType);
                    break;
                case ElementAccessExpressionSyntax element:
                    if (collector.Model.GetSymbolInfo(element).Symbol is IPropertySymbol indexer)
                        collector.Add(node, indexer.ContainingType);
                    break;
                case AnonymousFunctionExpressionSyntax lambda:
                    if (collector.Model.GetSymbolInfo(lambda).Symbol is IMethodSymbol function)
                    {
                        foreach (var parameter in function.Parameters)
                            collector.AddDeep(node, parameter.Type);
                        collector.AddDeep(node, function.ReturnType);
                    }

                    break;
                case ForEachStatementSyntax forEach:
                    if (collector.Model.GetForEachStatementInfo(forEach).ElementType is { } elementType)
                        collector.AddDeep(node, elementType);
                    break;
            }
        }

        return collector.References;
    }

    private sealed class Collector(IAssemblySymbol assembly, SemanticModel model, string file)
    {
        private readonly HashSet<SourceReference> _references = new();

        public SemanticModel Model { get; } = model;

        public IReadOnlyCollection<SourceReference> References => _references;

        public void Name(SimpleNameSyntax name)
        {
            var info = Model.GetSymbolInfo(name);
            var symbol = info.Symbol ?? (info.CandidateSymbols.Length == 1 ? info.CandidateSymbols[0] : null);
            switch (symbol)
            {
                case INamedTypeSymbol type:
                    // A type that only qualifies a member is covered by the member; a using directive is not a reference.
                    if (IsQualifierOfMember(name) || name.Ancestors().Any(node => node is UsingDirectiveSyntax))
                        return;
                    // A var has no syntax for the type arguments it infers (var kinds = Kinds() for a List<StatKind>), so they
                    // are taken from the inferred type; written type arguments are names of their own.
                    if (name.IsVar)
                        AddDeep(name, type);
                    else
                        Add(name, type);
                    break;
                case IMethodSymbol method:
                    var declared = method.ReducedFrom ?? method;
                    Add(name, declared.ContainingType);
                    foreach (var argument in method.TypeArguments)
                        AddDeep(name, argument);
                    AddTypeArguments(name, declared.ContainingType);
                    break;
                case IFieldSymbol field:
                    Add(name, field.ContainingType);
                    AddTypeArguments(name, field.ContainingType);
                    break;
                case IPropertySymbol property:
                    Add(name, property.ContainingType);
                    AddTypeArguments(name, property.ContainingType);
                    break;
                case IEventSymbol @event:
                    Add(name, @event.ContainingType);
                    break;
            }
        }

        public void AddDeep(SyntaxNode node, ITypeSymbol type)
        {
            switch (type)
            {
                case IArrayTypeSymbol array:
                    AddDeep(node, array.ElementType);
                    break;
                case IPointerTypeSymbol pointer:
                    AddDeep(node, pointer.PointedAtType);
                    break;
                case INamedTypeSymbol named:
                    Add(node, named);
                    foreach (var argument in named.TypeArguments)
                        AddDeep(node, argument);
                    break;
            }
        }

        public void Add(SyntaxNode node, INamedTypeSymbol? target)
        {
            if (target is null)
                return;

            target = target.OriginalDefinition;
            if (!SymbolEqualityComparer.Default.Equals(target.ContainingAssembly, assembly))
                return;

            var top = target;
            while (top.ContainingType is { } containing)
                top = containing;

            if (Source(node) is not { } source)
                return;

            var toKey = new TypeKey(top.ContainingNamespace.ToDisplayString(), top.Name);
            if (toKey == source.Type)
                return;

            if (UnitClassifier.Classify(source.Type) is not { } from || UnitClassifier.Classify(toKey) is not { } to || from == to)
                return;

            _references.Add(new SourceReference(file, from, source.Member, to, TargetName(target)));
        }

        private void AddTypeArguments(SyntaxNode node, INamedTypeSymbol? type)
        {
            if (type is not { IsGenericType: true })
                return;

            foreach (var argument in type.TypeArguments)
                AddDeep(node, argument);
        }

        private bool IsQualifierOfMember(SimpleNameSyntax name)
        {
            var right = name.Parent switch
            {
                MemberAccessExpressionSyntax access when access.Expression == name => access.Name,
                QualifiedNameSyntax qualified when qualified.Left == name => qualified.Right,
                _ => null
            };
            return right != null && Model.GetSymbolInfo(right).Symbol is { } member && member is not INamespaceSymbol;
        }

        /// <summary>The top-level type a node is in and the member around it (see <see cref="SourceReference.Member"/>).</summary>
        private static (TypeKey Type, string Member)? Source(SyntaxNode node)
        {
            var types = node.AncestorsAndSelf().Where(ancestor => ancestor is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax).ToList();
            if (types.Count == 0)
                return null;

            var path = string.Join("+", types.AsEnumerable().Reverse().Select(type => type switch
            {
                BaseTypeDeclarationSyntax declaration => declaration.Identifier.ValueText,
                DelegateDeclarationSyntax @delegate => @delegate.Identifier.ValueText,
                _ => "?"
            }));
            var outermost = types[^1];
            var name = outermost is BaseTypeDeclarationSyntax outer ? outer.Identifier.ValueText : ((DelegateDeclarationSyntax)outermost).Identifier.ValueText;
            var @namespace = string.Join(".", outermost.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Select(space => space.Name.ToString()).Reverse());
            var qualifiedPath = @namespace.Length == 0 ? path : @namespace + "." + path;
            return (new TypeKey(@namespace, name), qualifiedPath + "::" + MemberOf(node));
        }

        private static string MemberOf(SyntaxNode node)
        {
            foreach (var ancestor in node.AncestorsAndSelf())
            {
                switch (ancestor)
                {
                    case BaseTypeDeclarationSyntax or DelegateDeclarationSyntax:
                        return "(type)";
                    case MethodDeclarationSyntax method:
                        return method.Identifier.ValueText;
                    case ConstructorDeclarationSyntax constructor:
                        return constructor.Modifiers.Any(SyntaxKind.StaticKeyword) ? ".cctor" : ".ctor";
                    case DestructorDeclarationSyntax:
                        return "Finalize";
                    case PropertyDeclarationSyntax property:
                        return property.Identifier.ValueText;
                    case IndexerDeclarationSyntax:
                        return "Item";
                    case EventDeclarationSyntax @event:
                        return @event.Identifier.ValueText;
                    case EventFieldDeclarationSyntax eventField:
                        return eventField.Declaration.Variables.First().Identifier.ValueText;
                    case FieldDeclarationSyntax field:
                        return field.Declaration.Variables.First().Identifier.ValueText;
                    case OperatorDeclarationSyntax op:
                        return "op_" + op.OperatorToken.ValueText;
                    case ConversionOperatorDeclarationSyntax:
                        return "op_Conversion";
                    case EnumMemberDeclarationSyntax enumMember:
                        return enumMember.Identifier.ValueText;
                }
            }

            return "(type)";
        }

        private static string TargetName(INamedTypeSymbol type)
        {
            var name = type.Name;
            for (var containing = type.ContainingType; containing != null; containing = containing.ContainingType)
                name = containing.Name + "." + name;
            return name;
        }
    }
}
