using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SWLOR.Architecture.Tests.Debt;

namespace SWLOR.Architecture.Tests.Rules;

/// <summary>
/// The guards of content injection (MaintainabilityRefactorContentInjection.md, "Guards"), read from the server's source with the
/// semantic model: who sets a service-typed member, who builds a service or a type that takes services, how content names its
/// services, whether content singletons need each other, whether a static initializer reads a service, who reads a static
/// <c>ContainerHost</c>, and whether <c>ContentRegistrations</c> lists exactly the content singletons. Each method returns one line
/// per finding (<c>Type: what (file:line)</c>); the tests compare them with the reviewed exceptions.
/// </summary>
internal sealed class ContentInjectionRules
{
    public const string CompositionNamespace = "SWLOR.Game.Server.Composition";
    public const string ContentNamespace = "SWLOR.Game.Server.Feature";
    public const string ServiceRegistrationsType = CompositionNamespace + ".ServiceRegistrations";
    public const string ContentRegistrationsType = CompositionNamespace + ".ContentRegistrations";
    public const string AccessorType = CompositionNamespace + ".GameServices";
    public const string ContainerHostType = CompositionNamespace + ".ContainerHost";
    public const string ContentFactoryInterface = "SWLOR.Game.Server.Core.IContentFactory";
    public const string HandlerAttribute = "SWLOR.Game.Server.Core.NWNEventHandler";

    private static readonly string[] RegistrationMethods = ["AddSingleton", "AddTransient", "AddScoped", "TryAddSingleton"];

    private readonly CSharpCompilation _compilation;
    private readonly IReadOnlyList<SyntaxTree> _trees;
    private readonly Dictionary<SyntaxTree, SemanticModel> _models = new();

    public ContentInjectionRules(SourceSnapshot snapshot)
    {
        _compilation = snapshot.CompilationOf(snapshot.Server);
        _trees = snapshot.Server.Trees;
        Registered = RegisteredBy(ServiceRegistrationsType).Concat(RegisteredBy(ContentRegistrationsType)).ToHashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        ContentSingletons = RegisteredBy(ContentRegistrationsType).OfType<INamedTypeSymbol>().ToHashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
    }

    /// <summary>Every type the container registers: both sides of each registration, services and content singletons.</summary>
    public IReadOnlySet<ITypeSymbol> Registered { get; }

    /// <summary>The content singletons <c>ContentRegistrations</c> registers.</summary>
    public IReadOnlySet<INamedTypeSymbol> ContentSingletons { get; }

    /// <summary>No method or property sets a service-typed member: services come in through constructors only.</summary>
    public IReadOnlyList<string> ServiceSetters()
    {
        var problems = new List<string>();
        foreach (var (tree, model) in Models())
        {
            foreach (var assignment in tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>())
            {
                var target = model.GetSymbolInfo(assignment.Left).Symbol;
                var type = target switch { IFieldSymbol field => field.Type, IPropertySymbol property => property.Type, _ => null };
                if (type == null || !IsRegistered(type))
                    continue;

                var member = assignment.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault(node => node is not BaseTypeDeclarationSyntax);
                var inOwnConstructor = member is ConstructorDeclarationSyntax constructor && !constructor.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                                       SymbolEqualityComparer.Default.Equals(model.GetDeclaredSymbol(constructor)?.ContainingType, target!.ContainingType) &&
                                       assignment.Parent is not InitializerExpressionSyntax;
                if (!inOwnConstructor)
                    problems.Add(Finding(assignment, $"sets {target!.ContainingType?.Name}.{target.Name} ({type.Name}) outside its constructor"));
            }
        }

        return Sorted(problems);
    }

    /// <summary>A service or content singleton is never built with <c>new</c> or <c>Activator</c> outside <c>Composition/</c>.</summary>
    public IReadOnlyList<string> RegisteredCreations()
    {
        var problems = new List<string>();
        foreach (var (tree, model) in Models())
        {
            foreach (var node in tree.GetRoot().DescendantNodes())
            {
                var created = Created(node, model);
                if (created == null || !IsRegistered(created) || InNamespace(node, model, CompositionNamespace))
                    continue;

                problems.Add(Finding(node, $"builds {created.Name}, which the container registers: take it through the constructor"));
            }
        }

        return Sorted(problems);
    }

    /// <summary>
    /// A type whose constructor takes a registered type is built only by the factory, by a copy of itself, or as a private collaborator
    /// of the class that creates it (every service it is handed is the creator's own: <c>this</c>, a member, or a constructor parameter).
    /// </summary>
    public IReadOnlyList<string> MadeContentCreations()
    {
        var problems = new List<string>();
        foreach (var (tree, model) in Models())
        {
            foreach (var creation in tree.GetRoot().DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>())
            {
                if (model.GetSymbolInfo(creation).Symbol is not IMethodSymbol constructor || IsRegistered(constructor.ContainingType) ||
                    !constructor.Parameters.Any(parameter => IsRegistered(parameter.Type)) || InNamespace(creation, model, CompositionNamespace))
                {
                    continue;
                }

                var creator = model.GetEnclosingSymbol(creation.SpanStart)?.ContainingType;
                if (IsSameOrNested(creator, constructor.ContainingType))
                    continue;

                if (creation.ArgumentList == null || !PassesOwnServices(creation, constructor, model, creator))
                    problems.Add(Finding(creation, $"builds {constructor.ContainingType.Name}, which takes services, with services that are not its own: use the content factory"));
            }
        }

        return Sorted(problems);
    }

    /// <summary>Service-typed members of content are private get-only properties named like the old property or class, or <c>&lt;Name&gt;Service</c>.</summary>
    public IReadOnlyList<string> ContentMemberShapes()
    {
        var problems = new List<string>();
        foreach (var (tree, model) in Models())
        {
            foreach (var declaration in tree.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>())
            {
                var symbols = declaration switch
                {
                    FieldDeclarationSyntax fieldDeclaration => fieldDeclaration.Declaration.Variables.Select(variable => model.GetDeclaredSymbol(variable)),
                    PropertyDeclarationSyntax propertyDeclaration => new[] { model.GetDeclaredSymbol(propertyDeclaration) },
                    _ => Enumerable.Empty<ISymbol?>()
                };

                foreach (var symbol in symbols.OfType<ISymbol>())
                {
                    var type = symbol switch { IFieldSymbol field => field.Type, IPropertySymbol property => property.Type, _ => null };
                    if (type == null || !IsRegistered(type) || !IsContent(symbol.ContainingType) || symbol.IsImplicitlyDeclared)
                        continue;

                    var expected = ExpectedName(type);
                    if (symbol is not IPropertySymbol getter || getter.DeclaredAccessibility != Accessibility.Private || getter.SetMethod != null || getter.IsStatic)
                        problems.Add(Finding(declaration, $"{symbol.ContainingType.Name}.{symbol.Name} holds {type.Name}: content takes a service as a private get-only property named {expected}"));
                    else if (symbol.Name != expected && !(symbol.Name == expected + "Service" && Declares(symbol.ContainingType, expected)))
                        problems.Add(Finding(declaration, $"{symbol.ContainingType.Name}.{symbol.Name} holds {type.Name}: name it {expected} (or {expected}Service where the class has a member {expected})"));
                }
            }
        }

        return Sorted(problems);
    }

    /// <summary>No two content singletons depend on each other, directly or through others.</summary>
    public IReadOnlyList<string> SingletonCycles()
    {
        var singletons = ContentSingletons.ToArray();
        var edges = singletons.ToDictionary<INamedTypeSymbol, INamedTypeSymbol, INamedTypeSymbol[]>(type => type,
            type => type.Constructors.SelectMany(constructor => constructor.Parameters).Select(parameter => parameter.Type)
                .OfType<INamedTypeSymbol>().Where(ContentSingletons.Contains).ToArray(), SymbolEqualityComparer.Default);

        var problems = new List<string>();
        foreach (var start in singletons)
        {
            var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var pending = new Stack<INamedTypeSymbol>(edges[start]);
            while (pending.Count > 0)
            {
                var next = pending.Pop();
                if (SymbolEqualityComparer.Default.Equals(next, start))
                {
                    problems.Add($"{start.ToDisplayString()}: needs itself through other content singletons; the container cannot build it");
                    break;
                }

                if (seen.Add(next) && edges.TryGetValue(next, out var further))
                {
                    foreach (var type in further)
                        pending.Push(type);
                }
            }
        }

        return Sorted(problems);
    }

    /// <summary>No static field or property initializer reads a service: it would run at first use of the type, not with the services.</summary>
    public IReadOnlyList<string> StaticInitializers()
    {
        var problems = new List<string>();
        foreach (var (tree, model) in Models())
        {
            foreach (var declaration in tree.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>())
            {
                var initializers = declaration switch
                {
                    FieldDeclarationSyntax field when field.Modifiers.Any(SyntaxKind.StaticKeyword) => field.Declaration.Variables.Select(variable => variable.Initializer?.Value),
                    PropertyDeclarationSyntax property when property.Modifiers.Any(SyntaxKind.StaticKeyword) => new[] { property.Initializer?.Value },
                    _ => Enumerable.Empty<ExpressionSyntax?>()
                };

                foreach (var initializer in initializers.OfType<ExpressionSyntax>())
                {
                    var reads = initializer.DescendantNodesAndSelf().OfType<ExpressionSyntax>().Any(expression =>
                        model.GetSymbolInfo(expression).Symbol is IPropertySymbol { IsStatic: true } property && property.ContainingType.ToDisplayString() == AccessorType ||
                        expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } && model.GetTypeInfo(access.Expression).Type is { } receiver && IsRegistered(receiver));
                    if (reads)
                        problems.Add(Finding(declaration, $"a static initializer of {SyntaxNames.FullName(SyntaxNames.TopLevelType(declaration)!)} reads a service"));
                }
            }
        }

        return Sorted(problems);
    }

    /// <summary>Who reads a static <c>ContainerHost</c> (<c>CompositionRoot.ServerHost</c>): the entry points only, until it is removed.</summary>
    public IReadOnlyList<string> StaticContainerHosts()
    {
        var problems = new List<string>();
        foreach (var (tree, model) in Models())
        {
            foreach (var name in tree.GetRoot().DescendantNodes().OfType<SimpleNameSyntax>())
            {
                if (model.GetSymbolInfo(name).Symbol is not { IsStatic: true } symbol || symbol is not (IPropertySymbol or IFieldSymbol))
                    continue;

                var type = symbol switch { IPropertySymbol property => property.Type, IFieldSymbol field => field.Type, _ => null };
                if (type?.ToDisplayString() != ContainerHostType)
                    continue;

                var reader = model.GetEnclosingSymbol(name.SpanStart)?.ContainingType;
                problems.Add($"{Outermost(reader)?.ToDisplayString()} reads {symbol.ContainingType.Name}.{symbol.Name} ({Location(name)})");
            }
        }

        return Sorted(problems);
    }

    /// <summary>
    /// <c>ContentRegistrations</c> registers exactly the content singletons: each registered type is a content class whose public
    /// constructor takes only registered types; each content class with an instance handler, or that a class the container builds
    /// takes through its constructor, is registered; and no made content is registered (the factory never builds a registered type).
    /// </summary>
    public IReadOnlyList<string> RegistrationProblems()
    {
        var problems = new List<string>();
        foreach (var singleton in ContentSingletons)
        {
            if (!IsContent(singleton) || singleton.IsStatic || singleton.IsAbstract)
                problems.Add($"{singleton.ToDisplayString()} is registered in ContentRegistrations but is not a content class the container can build");
            else if (!singleton.Constructors.Any(constructor => constructor.DeclaredAccessibility == Accessibility.Public && constructor.Parameters.Length > 0 &&
                                                                constructor.Parameters.All(parameter => IsRegistered(parameter.Type))))
                problems.Add($"{singleton.ToDisplayString()} is registered in ContentRegistrations but no public constructor takes only registered types");
        }

        var injected = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var (tree, model) in Models())
        {
            foreach (var constructor in tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(constructor)?.ContainingType is not { } owner || !IsRegistered(owner))
                    continue;

                foreach (var parameter in constructor.ParameterList.Parameters)
                {
                    if (parameter.Type != null && model.GetTypeInfo(parameter.Type).Type is INamedTypeSymbol type && IsContent(type) && type.TypeKind == TypeKind.Class)
                        injected.Add(type);
                }
            }

            foreach (var invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { Name: "Create", TypeArguments.Length: > 0 } create &&
                    create.ContainingType.ToDisplayString() == ContentFactoryInterface && IsRegistered(create.TypeArguments[0]))
                {
                    problems.Add(Finding(invocation, $"builds {create.TypeArguments[0].Name} with the content factory, but the container registers it"));
                }
            }

            foreach (var declaration in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(declaration) is not { } type || !IsContent(type) || type.IsStatic || ContentSingletons.Contains(type))
                    continue;

                var handler = type.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(method => !method.IsStatic &&
                    method.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == HandlerAttribute));
                if (handler != null)
                    problems.Add(Finding(declaration, $"{type.Name} has the instance handler {handler.Name} but ContentRegistrations does not register it"));
            }
        }

        foreach (var type in injected.Where(type => !ContentSingletons.Contains(type)))
            problems.Add($"{type.ToDisplayString()} is taken through the constructor of a class the container builds, but ContentRegistrations does not register it");

        return Sorted(problems.Distinct());
    }

    /// <summary>The namespace and name of each content singleton, for the rules that read the compiled assembly.</summary>
    public IEnumerable<(string Namespace, string Name)> ContentSingletonKeys() =>
        ContentSingletons.Select(type => (type.ContainingNamespace.ToDisplayString(), type.Name));

    private IEnumerable<ITypeSymbol> RegisteredBy(string registrationsType)
    {
        var registrations = _compilation.GetTypeByMetadataName(registrationsType);
        if (registrations == null)
            yield break;

        foreach (var reference in registrations.DeclaringSyntaxReferences)
        {
            var node = reference.GetSyntax();
            var model = Model(node.SyntaxTree);
            foreach (var invocation in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax generic } && RegistrationMethods.Contains(generic.Identifier.ValueText) &&
                    model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method)
                {
                    foreach (var type in method.TypeArguments)
                        yield return type;
                }
            }
        }
    }

    private bool IsRegistered(ITypeSymbol type) => Registered.Contains(type.OriginalDefinition) || Registered.Contains(type);

    private static bool IsContent(INamedTypeSymbol? type)
    {
        var space = type?.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        return space == ContentNamespace || space.StartsWith(ContentNamespace + ".", StringComparison.Ordinal);
    }

    private static string ExpectedName(ITypeSymbol type)
    {
        if (type.ToDisplayString() == ContentFactoryInterface)
            return "Content";
        var name = type.Name;
        return type.TypeKind == TypeKind.Interface && name.Length > 8 && name[0] == 'I' && name.EndsWith("Service", StringComparison.Ordinal)
            ? name[1..^"Service".Length]
            : name;
    }

    private static bool Declares(INamedTypeSymbol type, string name)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (current.GetMembers(name).Any())
                return true;
        }

        return false;
    }

    private static ITypeSymbol? Created(SyntaxNode node, SemanticModel model)
    {
        switch (node)
        {
            case BaseObjectCreationExpressionSyntax creation:
                return model.GetTypeInfo(creation).Type;
            case InvocationExpressionSyntax invocation when model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { Name: "CreateInstance" } method &&
                                                           method.ContainingType.ToDisplayString() == "System.Activator":
                if (method.TypeArguments.Length == 1)
                    return method.TypeArguments[0];
                return invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression is TypeOfExpressionSyntax typeOf ? model.GetTypeInfo(typeOf.Type).Type : null;
            default:
                return null;
        }
    }

    private bool PassesOwnServices(BaseObjectCreationExpressionSyntax creation, IMethodSymbol constructor, SemanticModel model, INamedTypeSymbol? creator)
    {
        var enclosing = model.GetEnclosingSymbol(creation.SpanStart);
        foreach (var argument in creation.ArgumentList!.Arguments)
        {
            var parameter = argument.NameColon != null
                ? constructor.Parameters.FirstOrDefault(candidate => candidate.Name == argument.NameColon.Name.Identifier.ValueText)
                : constructor.Parameters.ElementAtOrDefault(creation.ArgumentList.Arguments.IndexOf(argument));
            if (parameter == null || !IsRegistered(parameter.Type))
                continue;

            if (argument.Expression is ThisExpressionSyntax)
                continue;

            var symbol = model.GetSymbolInfo(argument.Expression).Symbol;
            var own = symbol switch
            {
                IFieldSymbol field => IsSameOrBase(creator, field.ContainingType),
                IPropertySymbol property => IsSameOrBase(creator, property.ContainingType) && property.ContainingType.ToDisplayString() != AccessorType,
                IParameterSymbol parameterSymbol => parameterSymbol.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.Constructor } ||
                                                   SymbolEqualityComparer.Default.Equals(parameterSymbol.ContainingSymbol, enclosing),
                _ => false
            };
            if (!own)
                return false;
        }

        return true;
    }

    private static bool IsSameOrNested(INamedTypeSymbol? creator, INamedTypeSymbol created)
    {
        for (var type = creator; type != null; type = type.ContainingType)
        {
            if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, created.OriginalDefinition))
                return true;
        }

        return false;
    }

    private static bool IsSameOrBase(INamedTypeSymbol? creator, INamedTypeSymbol owner)
    {
        for (var type = creator; type != null; type = type.BaseType ?? type.ContainingType)
        {
            if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, owner.OriginalDefinition))
                return true;
        }

        return false;
    }

    private static INamedTypeSymbol? Outermost(INamedTypeSymbol? type)
    {
        while (type?.ContainingType != null)
            type = type.ContainingType;
        return type;
    }

    private bool InNamespace(SyntaxNode node, SemanticModel model, string space)
    {
        var name = Outermost(model.GetEnclosingSymbol(node.SpanStart)?.ContainingType)?.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        return name == space || name.StartsWith(space + ".", StringComparison.Ordinal);
    }

    private IEnumerable<(SyntaxTree Tree, SemanticModel Model)> Models() => _trees.Select(tree => (tree, Model(tree)));

    private SemanticModel Model(SyntaxTree tree)
    {
        if (!_models.TryGetValue(tree, out var model))
            _models[tree] = model = SourceSnapshot.Bind(_compilation, tree);
        return model;
    }

    private static string Finding(SyntaxNode node, string what)
    {
        var type = SyntaxNames.TopLevelType(node);
        return $"{(type == null ? "(file)" : SyntaxNames.FullName(type))}: {what} ({Location(node)})";
    }

    private static string Location(SyntaxNode node)
    {
        var span = node.SyntaxTree.GetLineSpan(node.Span);
        return $"{span.Path.Replace('\\', '/')}:{span.StartLinePosition.Line + 1}";
    }

    private static IReadOnlyList<string> Sorted(IEnumerable<string> problems) => problems.OrderBy(problem => problem, StringComparer.Ordinal).ToList();
}
