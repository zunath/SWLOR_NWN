using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// Decides which <see cref="DependencyUnit"/> a type belongs to from its namespace and name, never from the file it
/// lives in. A type in <c>Domain.&lt;Domain&gt;</c> or one of its topic namespaces (<c>Domain.Crafting.Minigame</c>) belongs
/// to the domain: since P2-Z2 a domain's services and their support types share that namespace, so the namespace names the
/// domain and nothing finer. A type in a <c>Service.&lt;Name&gt;Service</c> folder (a tree the P2-Z2 move has not reached) is
/// the unit of that service, a type in <c>Feature</c> its folder or itself, and the rest of the server assembly Platform;
/// anything else (compiler generated types, other assemblies) belongs to no unit. Nothing is left directly in <c>Service</c>
/// since P2-Z3: a type there belongs to no unit, and the namespace guard (<c>NamespaceRules</c>) fails it.
/// </summary>
internal static class UnitClassifier
{
    public const string RootNamespace = "SWLOR.Game.Server";

    private const string ServiceSegment = "Service";
    private const string FeatureSegment = "Feature";
    private const string DomainSegment = "Domain";
    private const string ServiceSuffix = "Service";

    public static DependencyUnit? Classify(TypeKey type)
    {
        var segments = Segments(type.Namespace);
        if (segments is null)
            return null;

        if (segments.Length == 0)
            return new DependencyUnit(UnitKind.Platform, "Root");

        switch (segments[0])
        {
            case ServiceSegment:
                return ServiceFolderName(segments) is { } folder ? new DependencyUnit(UnitKind.Service, folder) : null;

            case FeatureSegment:
                return segments.Length == 1
                    ? new DependencyUnit(UnitKind.FeatureType, type.Name)
                    : new DependencyUnit(UnitKind.FeatureFolder, segments[1]);

            case DomainSegment when segments.Length >= 2:
                // SWLOR.Game.Server.Domain.Crafting.Minigame.* belongs to the domain Crafting.
                return new DependencyUnit(UnitKind.Domain, segments[1]);

            default:
                return new DependencyUnit(UnitKind.Platform, segments[0]);
        }
    }

    /// <summary>
    /// The service of the domain map a type is named after, or null. A type in a <c>Service.&lt;Name&gt;Service</c> folder is that
    /// service. A type in <c>Domain.&lt;Domain&gt;</c> (or a topic below it) is the service of that domain whose name starts the
    /// type's name, ignoring one leading <c>I</c> (<c>IAchievementService</c>, <c>AchievementService</c> and
    /// <c>AchievementNotification</c> are all <c>Achievement</c>); the longest matching name wins (<c>SpaceModuleRecastService</c>
    /// is <c>SpaceModuleRecast</c>, not <c>Space</c>). The converted-service rules use it to tie a class to the service it is
    /// listed on; the cross-domain guard does not (it works by domain, <see cref="Classify"/>).
    /// </summary>
    public static string? ServiceOf(TypeKey type, DomainMap map)
    {
        var segments = Segments(type.Namespace);
        if (segments is not { Length: > 0 })
            return null;

        switch (segments[0])
        {
            case ServiceSegment:
                return ServiceFolderName(segments);

            case DomainSegment when segments.Length >= 2:
                return NamedService(segments[1], type.Name, map);

            default:
                return null;
        }
    }

    /// <summary>The name of the service a <c>Service.&lt;Name&gt;Service[.*]</c> namespace held, or null for the namespace <c>Service</c> itself.</summary>
    private static string? ServiceFolderName(string[] segments)
    {
        if (segments.Length == 1)
            return null;

        var folder = segments[1];
        return folder.Length > ServiceSuffix.Length && folder.EndsWith(ServiceSuffix, StringComparison.Ordinal)
            ? folder[..^ServiceSuffix.Length]
            : folder;
    }

    private static string? NamedService(string domain, string typeName, DomainMap map)
    {
        var stripped = typeName.Length > 1 && typeName[0] == 'I' && char.IsUpper(typeName[1]) ? typeName[1..] : typeName;
        string? best = null;
        foreach (var service in map.ServicesOf(domain))
        {
            if (!stripped.StartsWith(service, StringComparison.Ordinal))
                continue;

            var boundary = stripped.Length == service.Length || !char.IsLower(stripped[service.Length]);
            if (boundary && (best is null || service.Length > best.Length))
                best = service;
        }

        return best;
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
