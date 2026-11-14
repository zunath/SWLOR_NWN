using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>
/// The services of the server and who takes them through a constructor. A service type is a type the container registers
/// in <c>Composition/ServiceRegistrations.Register</c> (both sides of <c>AddSingleton&lt;IService, Service&gt;()</c>): the game
/// services and the process services (<c>IHookControl</c>, <c>IRedisConnectionSource</c>, ...). Records, settings and other
/// values a constructor takes are not services.
/// </summary>
internal static class ServiceScanner
{
    public const string RegistrationsType = "SWLOR.Game.Server.Composition.ServiceRegistrations";
    public const string ServiceProvider = "System.IServiceProvider";

    private static readonly string[] RegistrationMethods = ["AddSingleton", "AddTransient", "AddScoped", "TryAddSingleton", "TryAddTransient", "TryAddScoped"];

    private static readonly Dictionary<string, string> DeferredWrappers = new(StringComparer.Ordinal)
    {
        ["System.Lazy<T>"] = "Lazy",
        ["System.Func<TResult>"] = "Func"
    };

    /// <summary>The full names of the types <c>ServiceRegistrations</c> registers.</summary>
    public static IReadOnlySet<string> ServiceTypes(SourceSnapshot snapshot)
    {
        var compilation = snapshot.CompilationOf(snapshot.Server);
        var registrations = compilation.GetTypeByMetadataName(RegistrationsType);
        var types = new HashSet<string>(StringComparer.Ordinal);
        if (registrations is null)
            return types;

        foreach (var reference in registrations.DeclaringSyntaxReferences)
        {
            var node = reference.GetSyntax();
            var model = compilation.GetSemanticModel(node.SyntaxTree, ignoreAccessibility: true);
            foreach (var invocation in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax { Name: GenericNameSyntax generic } || !RegistrationMethods.Contains(generic.Identifier.ValueText))
                    continue;

                if (model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method)
                    types.UnionWith(method.TypeArguments.Select(type => type.ToDisplayString()));
            }
        }

        return types;
    }

    /// <summary>Every constructor parameter of a server type that is a service, or a <c>Lazy&lt;&gt;</c> or <c>Func&lt;&gt;</c> of one.</summary>
    public static IReadOnlyList<ConstructorService> ConstructorServices(SourceSnapshot snapshot, IReadOnlySet<string> serviceTypes)
    {
        var compilation = snapshot.CompilationOf(snapshot.Server);
        var found = new List<ConstructorService>();
        foreach (var tree in snapshot.Server.Trees)
        {
            var parameterLists = tree.GetRoot().DescendantNodes()
                .Select(node => node switch
                {
                    ConstructorDeclarationSyntax constructor when !constructor.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StaticKeyword) => constructor.ParameterList,
                    TypeDeclarationSyntax type => type.ParameterList,
                    _ => null
                })
                .OfType<ParameterListSyntax>()
                .Where(list => list.Parameters.Count > 0)
                .ToList();
            if (parameterLists.Count == 0)
                continue;

            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
            foreach (var list in parameterLists)
            {
                var topLevel = SyntaxNames.TopLevelType(list)!;
                var typeName = SyntaxNames.FullName(topLevel);

                // A nested helper that takes the service it belongs to (AbilityService.TrackedAbilityImpact takes the
                // AbilityService) is not a dependency of the service.
                var self = (model.GetDeclaredSymbol(topLevel) as INamedTypeSymbol)?.ToDisplayString();
                foreach (var parameter in list.Parameters)
                {
                    if (model.GetDeclaredSymbol(parameter)?.Type is not INamedTypeSymbol parameterType)
                        continue;

                    var wrapper = parameterType.IsGenericType && DeferredWrappers.TryGetValue(parameterType.OriginalDefinition.ToDisplayString(), out var name) ? name : string.Empty;
                    var service = wrapper.Length > 0 ? parameterType.TypeArguments[0].ToDisplayString() : parameterType.ToDisplayString();
                    if (serviceTypes.Contains(service) && service != self)
                        found.Add(new ConstructorService(typeName, tree.FilePath, service, wrapper));
                }
            }
        }

        return found;
    }

    /// <summary>Every name in the server outside <paramref name="allowedFolder"/> that binds to <c>System.IServiceProvider</c>, as (file, top-level type).</summary>
    public static IReadOnlyList<(string File, string? Type)> ServiceProviderUses(SourceSnapshot snapshot, string allowedFolder)
    {
        var compilation = snapshot.CompilationOf(snapshot.Server);
        var uses = new List<(string File, string? Type)>();
        foreach (var tree in snapshot.Server.Trees)
        {
            if (tree.FilePath.StartsWith(allowedFolder, StringComparison.Ordinal) || !tree.GetText().ToString().Contains("IServiceProvider", StringComparison.Ordinal))
                continue;

            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
            foreach (var name in tree.GetRoot().DescendantNodes().OfType<SimpleNameSyntax>().Where(name => name.Identifier.ValueText == "IServiceProvider"))
            {
                if (name.Ancestors().Any(node => node is UsingDirectiveSyntax))
                    continue;

                var info = model.GetSymbolInfo(name);
                if (info.Symbol is INamedTypeSymbol type && type.ToDisplayString() == ServiceProvider)
                    uses.Add((tree.FilePath, SyntaxNames.TopLevelType(name) is { } top ? SyntaxNames.FullName(top) : null));
            }
        }

        return uses;
    }
}
