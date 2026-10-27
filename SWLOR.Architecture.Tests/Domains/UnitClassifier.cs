using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// Decides which <see cref="DependencyUnit"/> a type belongs to from its namespace and name, never from the file it
/// lives in, so the guard keeps working when a later phase moves a domain's types into new folders and namespaces.
/// Types of the server assembly outside <c>Service</c> and <c>Feature</c> are Platform, except the instance types of
/// converted services in <c>Domain.&lt;Domain&gt;</c> (see <see cref="ClassifyDomainType"/>); anything else (compiler
/// generated types, other assemblies) belongs to no unit.
/// </summary>
internal static class UnitClassifier
{
    public const string RootNamespace = "SWLOR.Game.Server";

    private const string ServiceSegment = "Service";
    private const string FeatureSegment = "Feature";
    private const string DomainSegment = "Domain";
    private const string ServiceSuffix = "Service";

    /// <param name="type">The type to place.</param>
    /// <param name="map">
    /// The domain map. Needed only to place the types of converted services in <c>Domain.&lt;Domain&gt;</c>; without
    /// it they are Platform code, <c>Platform/Domain.&lt;Domain&gt;</c>, like a type no service of the domain names.
    /// </param>
    public static DependencyUnit? Classify(TypeKey type, DomainMap? map = null)
    {
        var segments = Segments(type.Namespace);
        if (segments is null)
            return null;

        if (segments.Length == 0)
            return new DependencyUnit(UnitKind.Platform, "Root");

        switch (segments[0])
        {
            case ServiceSegment:
                if (segments.Length == 1)
                    return new DependencyUnit(UnitKind.Service, type.Name);

                // Service.CombatService.* belongs to the service Combat.
                var folder = segments[1];
                var name = folder.Length > ServiceSuffix.Length && folder.EndsWith(ServiceSuffix, StringComparison.Ordinal)
                    ? folder[..^ServiceSuffix.Length]
                    : folder;
                return new DependencyUnit(UnitKind.Service, name);

            case FeatureSegment:
                return segments.Length == 1
                    ? new DependencyUnit(UnitKind.FeatureType, type.Name)
                    : new DependencyUnit(UnitKind.FeatureFolder, segments[1]);

            case DomainSegment when segments.Length >= 2:
                return ClassifyDomainType(segments[1], type.Name, map);

            default:
                return new DependencyUnit(UnitKind.Platform, segments[0]);
        }
    }

    /// <summary>
    /// A type in <c>Domain.&lt;Domain&gt;</c> belongs to the service of that domain whose name starts the type's name,
    /// ignoring one leading <c>I</c> (<c>IAchievementService</c> and <c>AchievementService</c> are the service
    /// <c>Achievement</c>), so a converted service is one unit before and after its conversion. The longest matching
    /// service name wins (<c>SpaceModuleRecastService</c> is <c>SpaceModuleRecast</c>, not <c>Space</c>). A type no
    /// service of the domain names is Platform, <c>Platform/Domain.&lt;Domain&gt;</c>, so it shows up as an edge to review.
    /// </summary>
    private static DependencyUnit ClassifyDomainType(string domain, string typeName, DomainMap? map)
    {
        var stripped = typeName.Length > 1 && typeName[0] == 'I' && char.IsUpper(typeName[1]) ? typeName[1..] : typeName;
        string? best = null;
        foreach (var service in map?.ServicesOf(domain) ?? Array.Empty<string>())
        {
            if (!stripped.StartsWith(service, StringComparison.Ordinal))
                continue;

            var boundary = stripped.Length == service.Length || !char.IsLower(stripped[service.Length]);
            if (boundary && (best is null || service.Length > best.Length))
                best = service;
        }

        return best is null
            ? new DependencyUnit(UnitKind.Platform, DomainSegment + "." + domain)
            : new DependencyUnit(UnitKind.Service, best);
    }

    /// <summary>The namespace segments below <see cref="RootNamespace"/>, or null when the type is not part of the server.</summary>
    private static string[]? Segments(string @namespace)
    {
        if (@namespace == RootNamespace)
            return [];

        return @namespace.StartsWith(RootNamespace + ".", StringComparison.Ordinal)
            ? @namespace[(RootNamespace.Length + 1)..].Split('.')
            : null;
    }
}
