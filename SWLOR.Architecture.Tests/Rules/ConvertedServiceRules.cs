using SWLOR.Architecture.Tests.Domains;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Rules;

/// <summary>
/// The rules every converted service (<c>ConvertedServices.json</c>) must keep. Each rule reads the compiled assembly
/// and returns one message per violation, so a failing test names exactly what to fix.
/// </summary>
internal static class ConvertedServiceRules
{
    /// <summary>The list must be well formed, and each service must be in the domain the domain map puts it in.</summary>
    public static IReadOnlyList<string> ListProblems(ConvertedServiceList list, DomainMap map)
    {
        var problems = new List<string>(list.Problems);
        foreach (var service in list.Services)
        {
            var mapped = map.DomainOf(service.Unit);
            if (mapped is null)
                problems.Add($"{service.Name} is not a service in DomainMap.json.");
            else if (mapped != service.Domain)
                problems.Add($"{service.Name} is listed in the {service.Domain} domain but DomainMap.json puts it in {mapped}.");
        }

        return problems;
    }

    /// <summary>
    /// Each converted service has its three pieces: a static facade class, an interface, and a class that implements
    /// the interface.
    /// </summary>
    public static IReadOnlyList<string> MissingPieces(ConvertedServiceList list, AssemblyFacts facts)
    {
        // Generic types of one name (Name and Name<T>) share a key; the first one is the one the rule looks at.
        var byKey = new Dictionary<TypeKey, TypeFact>();
        foreach (var type in facts.Types)
            byKey.TryAdd(type.Key, type);

        var problems = new List<string>();
        foreach (var service in list.Services)
        {
            if (!byKey.TryGetValue(service.Facade, out var facade))
                problems.Add($"{service.Name}: the static facade {service.Facade} does not exist.");
            else if (!facade.IsStatic)
                problems.Add($"{service.Name}: {service.Facade} must be a static class.");

            if (!byKey.TryGetValue(service.Interface, out var contract))
                problems.Add($"{service.Name}: the interface {service.Interface} does not exist.");
            else if (!contract.IsInterface)
                problems.Add($"{service.Name}: {service.Interface} must be an interface.");

            if (!byKey.TryGetValue(service.Instance, out var instance))
                problems.Add($"{service.Name}: the instance class {service.Instance} does not exist.");
            else if (instance.IsInterface || instance.IsStatic)
                problems.Add($"{service.Name}: {service.Instance} must be an instance class, not static or an interface.");
            else if (!instance.Interfaces.Contains(service.Interface.ToString()))
                problems.Add($"{service.Name}: {service.Instance} must implement {service.Interface}.");
        }

        return problems;
    }

    /// <summary>
    /// A converted service keeps its state on the instance, so none of its types may hold a static field. Only
    /// <c>const</c> fields are allowed. A <c>static readonly</c> field is state too when it holds a collection, so it is
    /// not allowed either.
    /// </summary>
    /// <param name="facts">The compiled assembly.</param>
    /// <param name="isServiceType">True for a top-level type that belongs to a converted service.</param>
    public static IReadOnlyList<string> StaticMutableFields(AssemblyFacts facts, Func<TypeKey, bool> isServiceType)
    {
        return facts.StaticFields
            .Where(field => !field.IsLiteral && isServiceType(field.Owner))
            .Select(field => $"{field} is static state of a converted service: make it an instance field" +
                             (field.IsInitOnly ? " (static readonly still shares the collection it holds)." : "."))
            .OrderBy(message => message, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// <c>[NWNEventHandler]</c> methods stay on the facade, so the handler order manifest keeps its names and the
    /// script registry can bind them as static methods. A handler on any other type of a converted service fails.
    /// </summary>
    /// <param name="facts">The compiled assembly.</param>
    /// <param name="facadeOf">The facade type of the converted service the type belongs to, or null for other types.</param>
    public static IReadOnlyList<string> HandlersOffTheFacade(AssemblyFacts facts, Func<TypeKey, TypeKey?> facadeOf)
    {
        var problems = new List<string>();
        foreach (var handler in facts.Handlers)
        {
            var facade = facadeOf(handler.Owner);
            if (facade is { } expected && expected != handler.Owner)
                problems.Add($"{handler.Owner}.{handler.Method} is an event handler on a converted service but not on its facade {expected}: leave the handler on the facade and forward to the instance.");
        }

        return problems.OrderBy(message => message, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// The instance class and its interface never use the facade of their own service: the facade forwards to the
    /// instance, never the other way, or the call would loop back through the container.
    /// </summary>
    public static IReadOnlyList<string> InstanceUsesItsFacade(TypeReferenceGraph graph, ConvertedServiceList list)
    {
        var problems = new List<string>();
        foreach (var service in list.Services)
        {
            foreach (var instanceType in new[] { service.Instance, service.Interface })
            {
                if (graph.References.TryGetValue((instanceType, service.Facade), out var members))
                    problems.Add($"{instanceType} uses its own facade {service.Facade} ({string.Join(", ", members.OrderBy(m => m, StringComparer.Ordinal))}): call the instance members directly.");
            }
        }

        return problems;
    }
}
