using SWLOR.Architecture.Tests.Domains;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Rules;

/// <summary>
/// The rules every converted service (<c>ConvertedServices.json</c>) must keep. Each rule reads the compiled assembly
/// and returns one message per violation, so a failing test names exactly what to fix.
/// </summary>
internal static class ConvertedServiceRules
{
    /// <summary>
    /// The list must be well formed, and each service must be in the domain the domain map puts it in. Each extra
    /// facade must belong to the service it is listed on.
    /// </summary>
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

            foreach (var pieces in service.Pieces.Skip(1))
                problems.AddRange(ExtraFacadeProblems(service, pieces, map));
        }

        return problems;
    }

    /// <summary>
    /// Each converted service has its three pieces: a static facade class, an interface, and a class that implements
    /// the interface. The same holds for each extra facade the service lists, with the interface and the instance class
    /// named after the facade type.
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
            foreach (var pieces in service.Pieces)
            {
                if (!byKey.TryGetValue(pieces.Facade, out var facade))
                    problems.Add($"{pieces.Label}: the static facade {pieces.Facade} does not exist.");
                else if (!facade.IsStatic)
                    problems.Add($"{pieces.Label}: {pieces.Facade} must be a static class.");

                if (!byKey.TryGetValue(pieces.Interface, out var contract))
                    problems.Add($"{pieces.Label}: the interface {pieces.Interface} does not exist.");
                else if (!contract.IsInterface)
                    problems.Add($"{pieces.Label}: {pieces.Interface} must be an interface.");

                if (!byKey.TryGetValue(pieces.Instance, out var instance))
                    problems.Add($"{pieces.Label}: the instance class {pieces.Instance} does not exist.");
                else if (instance.IsInterface || instance.IsStatic)
                    problems.Add($"{pieces.Label}: {pieces.Instance} must be an instance class, not static or an interface.");
                else if (!instance.Interfaces.Contains(pieces.Interface.ToString()))
                    problems.Add($"{pieces.Label}: {pieces.Instance} must implement {pieces.Interface}.");
            }
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
            .Select(field => $"{field} is static state of a converted service: make it an instance {(field.IsAutoProperty ? "property" : "field")}" +
                             (field.IsInitOnly ? " (static readonly still shares the collection it holds)." : "."))
            .OrderBy(message => message, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// <c>[NWNEventHandler]</c> methods stay on a facade, so the handler order manifest keeps its names and the
    /// script registry can bind them as static methods. A handler on any other type of a converted service fails. A
    /// service with extra facades may keep its handlers on any of them.
    /// </summary>
    /// <param name="facts">The compiled assembly.</param>
    /// <param name="facadesOf">The static facades of the converted service the type belongs to, or null for other types.</param>
    public static IReadOnlyList<string> HandlersOffTheFacade(AssemblyFacts facts, Func<TypeKey, IReadOnlyList<TypeKey>?> facadesOf)
    {
        var problems = new List<string>();
        foreach (var handler in facts.Handlers)
        {
            if (facadesOf(handler.Owner) is not { Count: > 0 } facades || facades.Contains(handler.Owner))
                continue;

            var expected = facades.Count == 1 ? $"its facade {facades[0]}" : $"one of its facades ({string.Join(", ", facades)})";
            problems.Add($"{handler.Owner}.{handler.Method} is an event handler on a converted service but not on {expected}: leave the handler on the facade and forward to the instance.");
        }

        return problems.OrderBy(message => message, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// The instance class and its interface never use the facade they belong to: the facade forwards to the instance,
    /// never the other way, or the call would loop back through the container. Each facade has its own instance class,
    /// so an instance may call another facade of the same service (the way one piece reaches a piece that cannot be
    /// injected without a construction cycle).
    /// </summary>
    public static IReadOnlyList<string> InstanceUsesItsFacade(TypeReferenceGraph graph, ConvertedServiceList list)
    {
        var problems = new List<string>();
        foreach (var service in list.Services)
        {
            foreach (var pieces in service.Pieces)
            {
                foreach (var instanceType in new[] { pieces.Instance, pieces.Interface })
                {
                    if (graph.References.TryGetValue((instanceType, pieces.Facade), out var members))
                        problems.Add($"{instanceType} uses its own facade {pieces.Facade} ({string.Join(", ", members.OrderBy(m => m, StringComparer.Ordinal))}): call the instance members directly.");
                }
            }
        }

        return problems;
    }

    /// <summary>
    /// An extra facade must belong to the service it is listed on: the facade lives in the service's own namespace
    /// (<c>Service.&lt;Name&gt;Service</c>), and its interface and instance class are named after the service so the
    /// cross-domain guard counts them as the same unit.
    /// </summary>
    private static IEnumerable<string> ExtraFacadeProblems(ConvertedService service, ConvertedPieces pieces, DomainMap map)
    {
        if (pieces.Facade == service.Facade)
        {
            yield return $"{pieces.Label} is the main facade of {service.Name}: list only the further static classes of its namespace.";
            yield break;
        }

        var owner = UnitClassifier.Classify(pieces.Facade, map);
        if (owner != service.Unit)
        {
            yield return $"{pieces.Label}: {pieces.Facade} belongs to {Describe(owner)}, not to {service.Name}. " +
                         $"An extra facade lives in the namespace {ConvertedService.ServerNamespace}.Service.{service.Name}Service of the service it is listed on.";
        }

        foreach (var type in new[] { pieces.Interface, pieces.Instance })
        {
            var typeOwner = UnitClassifier.Classify(type, map);
            if (typeOwner != service.Unit)
            {
                yield return $"{pieces.Label}: {type} would count as {Describe(typeOwner)}, not as {service.Name}. " +
                             $"Name the facade type so that its service class starts with {service.Name} (for example {service.Name}Encounter).";
            }
        }
    }

    private static string Describe(DependencyUnit? unit) => unit is { } found ? found.ToString() : "no service";
}
