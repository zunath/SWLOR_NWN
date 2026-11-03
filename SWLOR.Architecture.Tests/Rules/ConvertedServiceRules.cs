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
    /// (a further static facade or a part) must belong to the service it is listed on.
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

            var instances = new HashSet<TypeKey>();
            foreach (var extra in service.Extras)
            {
                var pieces = ConvertedPieces.ForExtra(service, extra);
                problems.AddRange(ExtraProblems(service, extra, pieces, map));
                if (!instances.Add(pieces.Instance))
                    problems.Add($"{pieces.Label}: {pieces.Instance} is already the instance class of another class of {service.Name}: give each extra a type name of its own.");
            }
        }

        return problems;
    }

    /// <summary>
    /// Each converted service has its three pieces: a static facade class, an interface, and a class that implements
    /// the interface. The same holds for each extra facade the service lists, with the interface and the instance class
    /// named after the facade type. A part has the interface and the class that implements it, and no facade.
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
                if (pieces.Facade is { } facadeKey)
                {
                    if (!byKey.TryGetValue(facadeKey, out var facade))
                        problems.Add($"{pieces.Label}: the static facade {facadeKey} does not exist.");
                    else if (!facade.IsStatic)
                        problems.Add($"{pieces.Label}: {facadeKey} must be a static class.");
                }

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
    /// injected without a construction cycle). A part has no facade of its own, so there is nothing to check for it.
    /// </summary>
    public static IReadOnlyList<string> InstanceUsesItsFacade(TypeReferenceGraph graph, ConvertedServiceList list)
    {
        var problems = new List<string>();
        foreach (var service in list.Services)
        {
            foreach (var pieces in service.Pieces.Where(pieces => pieces.Facade is not null))
            {
                var facade = pieces.Facade!.Value;
                foreach (var instanceType in new[] { pieces.Instance, pieces.Interface })
                {
                    if (graph.References.TryGetValue((instanceType, facade), out var members))
                        problems.Add($"{instanceType} uses its own facade {facade} ({string.Join(", ", members.OrderBy(m => m, StringComparer.Ordinal))}): call the instance members directly.");
                }
            }
        }

        return problems;
    }

    /// <summary>
    /// An extra must belong to the service it is listed on. A further facade lives in the service's own namespace
    /// (<c>Service.&lt;Name&gt;Service</c>); the interface and instance class of any extra are named after the service
    /// so the cross-domain guard counts them as the same unit (a part is only these two classes).
    /// </summary>
    private static IEnumerable<string> ExtraProblems(ConvertedService service, ExtraPiece extra, ConvertedPieces pieces, DomainMap map)
    {
        if (pieces.Facade is { } facade)
        {
            if (facade == service.Facade)
            {
                yield return $"{pieces.Label} is the main facade of {service.Name}: list only the further static classes of its namespace.";
                yield break;
            }

            var owner = UnitClassifier.Classify(facade, map);
            if (owner != service.Unit)
            {
                yield return $"{pieces.Label}: {facade} belongs to {Describe(owner)}, not to {service.Name}. " +
                             $"An extra facade lives in the namespace {ConvertedService.ServerNamespace}.Service.{service.Name}Service of the service it is listed on.";
            }
        }
        else if (pieces.Instance == service.Instance)
        {
            yield return $"{pieces.Label} is the main class of {service.Name} ({service.Instance}): give the part a name of its own.";
            yield break;
        }

        foreach (var type in new[] { pieces.Interface, pieces.Instance })
        {
            var typeOwner = UnitClassifier.Classify(type, map);
            if (typeOwner != service.Unit)
            {
                yield return $"{pieces.Label}: {type} would count as {Describe(typeOwner)}, not as {service.Name}. " +
                             $"Name the {extra.Kind} so that its service class starts with {service.Name} (for example {service.Name}Encounter).";
            }
        }
    }

    private static string Describe(DependencyUnit? unit) => unit is { } found ? found.ToString() : "no service";
}
