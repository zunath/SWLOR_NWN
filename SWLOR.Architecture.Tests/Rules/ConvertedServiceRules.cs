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
        problems.AddRange(InProgressProblems(list, map));
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
    /// The <c>inProgress</c> list may hold only <see cref="ConvertedServiceList.OnlyInProgressService"/>, in the domain
    /// the domain map puts it in. It is temporary: P2-CB10 removes the list when <c>Combat</c> is fully converted and
    /// listed under <c>services</c>.
    /// </summary>
    public static IReadOnlyList<string> InProgressProblems(ConvertedServiceList list, DomainMap map)
    {
        var problems = new List<string>();
        foreach (var service in list.InProgress)
        {
            if (service.Name != ConvertedServiceList.OnlyInProgressService)
            {
                problems.Add($"{service.Name} is listed in inProgress, but only {ConvertedServiceList.OnlyInProgressService} may be: " +
                             "convert a service in one pull request and list it under services.");
                continue;
            }

            var mapped = map.DomainOf(service.Unit);
            if (mapped is null)
                problems.Add($"{service.Name} (inProgress) is not a service in DomainMap.json.");
            else if (mapped != service.Domain)
                problems.Add($"{service.Name} is listed in inProgress in the {service.Domain} domain but DomainMap.json puts it in {mapped}.");
        }

        return problems;
    }

    /// <summary>
    /// The static facades that may resolve their instance from the container: those of the converted services and
    /// the facade of the service in progress.
    /// </summary>
    public static IReadOnlySet<TypeKey> FacadesMayLocate(ConvertedServiceList list)
    {
        // A removed facade no longer exists; only Composition (GameServices) resolves for it.
        return list.Services.Where(service => !service.FacadeRemoved).SelectMany(service => service.Facades)
            .Concat(list.InProgress.Select(service => service.Facade))
            .ToHashSet();
    }

    /// <summary>
    /// True for a part of a service in progress: a class or interface in its domain namespace named
    /// <c>&lt;Service&gt;*Service</c> or <c>I&lt;Service&gt;*Service</c> (<c>CombatQueriesService</c>), that is not the
    /// interface or instance class of a service already listed (<c>CombatPointService</c>). The rules for converted
    /// services apply to such a part: it keeps no static state.
    /// </summary>
    public static bool IsInProgressPart(TypeKey type, ConvertedServiceList list)
    {
        foreach (var service in list.InProgress)
        {
            if (type.Namespace != service.DomainNamespace)
                continue;

            var name = type.Name.Length > 1 && type.Name[0] == 'I' && char.IsUpper(type.Name[1]) ? type.Name[1..] : type.Name;
            if (!name.StartsWith(service.Name, StringComparison.Ordinal) || !name.EndsWith("Service", StringComparison.Ordinal))
                continue;

            var listed = list.Services.SelectMany(known => known.Pieces)
                .Any(pieces => pieces.Interface == type || pieces.Instance == type);
            if (!listed)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Each converted service has its three pieces: a static facade class, an interface, and a class that implements
    /// the interface. The same holds for each extra facade the service lists, with the interface and the instance class
    /// named after the facade type. A part has the interface and the class that implements it, and no facade. For a
    /// service whose facades were removed (<see cref="ConvertedService.FacadeRemoved"/>) each facade type must be gone,
    /// and <see cref="ConvertedService.Accessor"/> must have a static property of the facade's name typed as its interface.
    /// </summary>
    public static IReadOnlyList<string> MissingPieces(ConvertedServiceList list, AssemblyFacts facts)
    {
        // Generic types of one name (Name and Name<T>) share a key; the first one is the one the rule looks at.
        var byKey = new Dictionary<TypeKey, TypeFact>();
        foreach (var type in facts.Types)
            byKey.TryAdd(type.Key, type);

        var accessor = ConvertedService.Accessor;
        var problems = new List<string>();
        foreach (var service in list.Services)
        {
            foreach (var pieces in service.Pieces)
            {
                if (pieces.Facade is { } removedKey && service.FacadeRemoved)
                {
                    if (byKey.ContainsKey(removedKey))
                        problems.Add($"{pieces.Label}: the static facade {removedKey} still exists, but the service is marked \"facadeRemoved\": delete it (static code uses {accessor}.{removedKey.Name}).");
                    if (!facts.StaticProperties.Any(property => property.Owner == accessor && property.Name == removedKey.Name && property.Type == pieces.Interface.ToString()))
                        problems.Add($"{pieces.Label}: {accessor} has no static property {removedKey.Name} of type {pieces.Interface}, which static code uses instead of the removed facade.");
                }
                else if (pieces.Facade is { } facadeKey)
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
    /// <c>const</c> fields and <c>static readonly</c> fields of a deeply immutable type (primitives, <c>string</c>, enums,
    /// <c>Regex</c>, frozen and immutable collections of such elements, records whose members are all init-only and
    /// immutable) are allowed. A <c>static readonly</c> field that holds an array, a mutable collection or any type the
    /// check cannot prove immutable is state too, so it is reported.
    /// </summary>
    /// <param name="facts">The compiled assembly.</param>
    /// <param name="isServiceType">True for a top-level type that belongs to a converted service.</param>
    public static IReadOnlyList<string> StaticMutableFields(AssemblyFacts facts, Func<TypeKey, bool> isServiceType)
    {
        return facts.StaticFields
            .Where(field => !field.IsLiteral && !(field.IsInitOnly && field.IsDeeplyImmutable) && isServiceType(field.Owner))
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
    /// Once a service's facades are removed, its <c>[NWNEventHandler]</c> methods are on the instance classes the facades
    /// forwarded to (the script registry binds them to the container's singleton), never on a part, a support type or a
    /// leftover static class.
    /// </summary>
    /// <param name="facts">The compiled assembly.</param>
    /// <param name="instancesOf">
    /// For a type of a service whose facades were removed, the instance classes that had a facade (the main one and those of
    /// its further facades); null for other types.
    /// </param>
    public static IReadOnlyList<string> HandlersOffTheInstance(AssemblyFacts facts, Func<TypeKey, IReadOnlyList<TypeKey>?> instancesOf)
    {
        var problems = new List<string>();
        foreach (var handler in facts.Handlers)
        {
            if (instancesOf(handler.Owner) is not { Count: > 0 } instances || instances.Contains(handler.Owner))
                continue;

            var expected = instances.Count == 1 ? $"its instance class {instances[0]}" : $"one of its instance classes ({string.Join(", ", instances)})";
            problems.Add($"{handler.Owner}.{handler.Method} is an event handler of a service whose facade was removed, but not on {expected}: " +
                         "put it on the instance method the facade forwarded to and list the rename in tools/Refactor/HandlerRenames.txt.");
        }

        return problems.OrderBy(message => message, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// A class the container builds (the interface or the instance class of any piece of a converted service) never uses
    /// <see cref="ConvertedService.Accessor"/>: that would be service location from inside the container. It takes the
    /// interface through its constructor, or <c>Lazy&lt;IXService&gt;</c> where a direct parameter would make a cycle.
    /// </summary>
    public static IReadOnlyList<string> RegisteredClassesUseTheAccessor(TypeReferenceGraph graph, IEnumerable<TypeKey> registered, TypeKey accessor)
    {
        var problems = new List<string>();
        foreach (var type in registered.Distinct())
        {
            if (graph.References.TryGetValue((type, accessor), out var members))
                problems.Add($"{type} uses {accessor} ({string.Join(", ", members.OrderBy(m => m, StringComparer.Ordinal))}): a class the container builds takes the interface through its constructor (Lazy<IXService> for a constructor cycle).");
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
