using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// Decides which <see cref="DependencyUnit"/> a type belongs to from its namespace and name, never from the file it
/// lives in, so the guard keeps working when a later phase moves a domain's types into new folders and namespaces.
/// Types of the server assembly outside <c>Service</c> and <c>Feature</c> are Platform; anything else (compiler
/// generated types, other assemblies) belongs to no unit.
/// </summary>
internal static class UnitClassifier
{
    public const string RootNamespace = "SWLOR.Game.Server";

    private const string ServiceSegment = "Service";
    private const string FeatureSegment = "Feature";
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

            default:
                return new DependencyUnit(UnitKind.Platform, segments[0]);
        }
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
