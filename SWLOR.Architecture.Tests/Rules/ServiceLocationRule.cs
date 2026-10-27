using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Rules;

/// <summary>
/// No service location: game code receives its dependencies through a constructor and never asks the container for
/// them. Only the <c>Composition</c> namespace may touch the container, and the static facades of converted services
/// may use the one internal accessor (<c>FacadeAccessor</c>) to reach their instance.
/// </summary>
internal static class ServiceLocationRule
{
    private const string DependencyInjectionNamespace = "Microsoft.Extensions.DependencyInjection";
    private const string ServiceProvider = "System.IServiceProvider";

    /// <param name="graph">The references between the types of the assembly.</param>
    /// <param name="facts">The uses of types from other assemblies.</param>
    /// <param name="compositionNamespace">The namespace allowed to use the container and the accessor.</param>
    /// <param name="accessor">The facade accessor type.</param>
    /// <param name="isFacade">True for the static facade of a converted service.</param>
    public static IReadOnlyList<string> Find(
        TypeReferenceGraph graph,
        AssemblyFacts facts,
        string compositionNamespace,
        TypeKey accessor,
        Func<TypeKey, bool> isFacade)
    {
        bool InComposition(TypeKey type) =>
            type.Namespace == compositionNamespace || type.Namespace.StartsWith(compositionNamespace + ".", StringComparison.Ordinal);

        var problems = new List<string>();

        foreach (var reference in facts.ExternalReferences)
        {
            if (InComposition(reference.Source) || !IsContainerType(reference.ExternalType))
                continue;

            problems.Add($"{reference.Source} uses {reference.ExternalType} ({Describe(reference.Members)}): only {compositionNamespace} may use the container. Take the dependency through the constructor.");
        }

        foreach (var ((source, target), members) in graph.References)
        {
            if (target != accessor || InComposition(source) || isFacade(source))
                continue;

            problems.Add($"{source} uses {accessor} ({Describe(members)}): only {compositionNamespace} and the static facades of converted services may resolve from the container. Take the dependency through the constructor.");
        }

        return problems.OrderBy(message => message, StringComparer.Ordinal).ToList();
    }

    private static bool IsContainerType(string fullName) =>
        fullName == ServiceProvider
        || fullName == DependencyInjectionNamespace
        || fullName.StartsWith(DependencyInjectionNamespace + ".", StringComparison.Ordinal);

    private static string Describe(IEnumerable<string> members) => string.Join(", ", members.OrderBy(member => member, StringComparer.Ordinal));
}
