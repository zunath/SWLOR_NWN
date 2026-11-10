namespace SWLOR.Architecture.Tests.Domains;

/// <summary>What a <see cref="DependencyUnit"/> is in the source tree.</summary>
internal enum UnitKind
{
    /// <summary>
    /// A domain's own code: every type in <c>Domain.&lt;Domain&gt;</c> and its topic namespaces (<c>Domain.Crafting.Minigame</c>),
    /// which holds the domain's services and their support types. The unit is named after the domain.
    /// </summary>
    Domain,

    /// <summary>
    /// A type that sits directly in <c>Service</c> (a static helper not yet given a domain, such as <c>ColorToken</c>), or a
    /// <c>Service.&lt;Name&gt;Service</c> folder of a tree the P2-Z2 move has not reached. The domain map's <c>services</c> list
    /// places it.
    /// </summary>
    Service,

    /// <summary>A folder of <c>Feature</c> (definitions or content), for example <c>PerkDefinition</c>.</summary>
    FeatureFolder,

    /// <summary>A type that sits directly in <c>Feature</c> (a top-level <c>Feature/*.cs</c> file).</summary>
    FeatureType,

    /// <summary>Code outside <c>Domain</c>, <c>Service</c> and <c>Feature</c>: <c>Core</c>, <c>Native</c>, <c>Entity</c>, <c>Enumeration</c>, <c>Extension</c>.</summary>
    Platform
}
