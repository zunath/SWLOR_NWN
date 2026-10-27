namespace SWLOR.Architecture.Tests.Domains;

/// <summary>What a <see cref="DependencyUnit"/> is in the source tree.</summary>
internal enum UnitKind
{
    /// <summary>
    /// A service: the static class <c>Service.Name</c> plus the types in <c>Service.NameService.*</c>, and, once the
    /// service is converted, its interface and instance class in <c>Domain.&lt;Domain&gt;</c>.
    /// </summary>
    Service,

    /// <summary>A folder of <c>Feature</c> (definitions or content), for example <c>PerkDefinition</c>.</summary>
    FeatureFolder,

    /// <summary>A type that sits directly in <c>Feature</c> (a top-level <c>Feature/*.cs</c> file).</summary>
    FeatureType,

    /// <summary>Code outside <c>Service</c> and <c>Feature</c>: <c>Core</c>, <c>Native</c>, <c>Entity</c>, <c>Enumeration</c>, <c>Extension</c>.</summary>
    Platform
}
