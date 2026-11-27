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
    /// The list must be well formed, and each service must be in the domain the domain map puts it in. Each extra (a class
    /// or a part) must count as the service it is listed on.
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

            foreach (var extra in service.Extras)
                problems.AddRange(ExtraProblems(service, extra, ConvertedPieces.ForExtra(service, extra), map));
        }

        return problems;
    }

    /// <summary>
    /// Each converted service has its pieces: an interface and a class that implements it, and a static property of the
    /// interface's type on <see cref="ConvertedService.Accessor"/> named like the service (or like the further class), which
    /// static code reaches the service through. A part has the interface and the class and no property.
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
                if (pieces.AccessorProperty is { } propertyName &&
                    !facts.StaticProperties.Any(property => property.Owner == accessor && property.Name == propertyName && property.Type == pieces.Interface.ToString()))
                    problems.Add($"{pieces.Label}: {accessor} has no static property {propertyName} of type {pieces.Interface}, which static code uses to reach the service.");

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
    /// The <c>[NWNEventHandler]</c> methods of a converted service are on its instance classes (the main one, those of its
    /// further classes and its parts): the script registry binds them to the container's singleton. A handler on a support
    /// type or a static class of the service fails.
    /// </summary>
    /// <param name="facts">The compiled assembly.</param>
    /// <param name="instancesOf">
    /// For a type of a converted service, the instance classes of the service; null for other types.
    /// </param>
    public static IReadOnlyList<string> HandlersOffTheInstance(AssemblyFacts facts, Func<TypeKey, IReadOnlyList<TypeKey>?> instancesOf)
    {
        var problems = new List<string>();
        foreach (var handler in facts.Handlers)
        {
            if (instancesOf(handler.Owner) is not { Count: > 0 } instances || instances.Contains(handler.Owner))
                continue;

            var expected = instances.Count == 1 ? $"its instance class {instances[0]}" : $"one of its instance classes ({string.Join(", ", instances)})";
            problems.Add($"{handler.Owner}.{handler.Method} is an event handler of a converted service, but not on {expected}: " +
                         "put it on an instance method of the service (a moved handler gets a line in tools/Refactor/HandlerRenames.txt).");
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
    /// An extra must belong to the service it is listed on: the interface and instance class of any extra are named after the
    /// service (<see cref="UnitClassifier.ServiceOf"/>), so a reader finds the service from the class name, and the extra is
    /// not the service's main class.
    /// </summary>
    private static IEnumerable<string> ExtraProblems(ConvertedService service, ExtraPiece extra, ConvertedPieces pieces, DomainMap map)
    {
        if (pieces.Instance == service.Instance)
        {
            yield return $"{pieces.Label} is the main class of {service.Name} ({service.Instance}): give the {extra.Kind} a name of its own.";
            yield break;
        }

        foreach (var type in new[] { pieces.Interface, pieces.Instance })
        {
            var namedService = UnitClassifier.ServiceOf(type, map);
            if (namedService != service.Name)
            {
                yield return $"{pieces.Label}: {type} would count as {namedService ?? "no service"}, not as {service.Name}. " +
                             $"Name the {extra.Kind} so that its service class starts with {service.Name} (for example {service.Name}Encounter).";
            }
        }
    }

    /// <summary>
    /// A registered service class is a piece of a converted service: an instance class <c>&lt;Name&gt;Service</c> behind the
    /// interface <c>I&lt;Name&gt;Service</c>, both directly in the service's domain namespace (<see cref="MissingPieces"/> checks
    /// the pieces exist; <see cref="ListProblems"/> that the name is a service of that domain). So a class or an interface in
    /// <c>Domain.&lt;Domain&gt;</c> or a topic below it that is named like a service class (<c>FooService</c>,
    /// <c>IFooService</c>) must be one of the listed pieces. Support types share the namespace and are not service classes: any
    /// other name passes.
    /// </summary>
    /// <param name="facts">The compiled assembly.</param>
    /// <param name="list">The converted services; their pieces are the registered service classes.</param>
    public static IReadOnlyList<string> UnlistedServiceClasses(AssemblyFacts facts, ConvertedServiceList list)
    {
        var pieces = list.Services
            .SelectMany(service => service.Pieces)
            .SelectMany(piece => new[] { piece.Interface, piece.Instance })
            .ToHashSet();
        var domainPrefix = ConvertedService.ServerNamespace + ".Domain.";

        return facts.Types
            .Where(type => type.Key.Namespace.StartsWith(domainPrefix, StringComparison.Ordinal))
            .Where(type => IsNamedLikeAServiceClass(type.Key.Name))
            .Where(type => !pieces.Contains(type.Key))
            .Select(type => $"{type.Key} is named like a service class but is not a piece of a converted service in ConvertedServices.json: " +
                            "a registered service is <Name>Service behind I<Name>Service in Domain.<Domain>, listed there; " +
                            "a support type takes a name without the Service suffix.")
            .Distinct()
            .OrderBy(message => message, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>A type of any kind whose name ends in <c>Service</c> after at least one more character (<c>FooService</c>, <c>IFooService</c>).</summary>
    private static bool IsNamedLikeAServiceClass(string name) =>
        name.Length > ServiceSuffix.Length && name.EndsWith(ServiceSuffix, StringComparison.Ordinal);

    private const string ServiceSuffix = "Service";
}
