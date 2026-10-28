using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// A service that has been converted to the dependency-injection pattern (Readmes/DependencyInjection.md): a static
/// facade <c>Service.&lt;Name&gt;</c> that forwards to an instance class <c>&lt;Name&gt;Service</c> behind the interface
/// <c>I&lt;Name&gt;Service</c>, both in <c>Domain.&lt;Domain&gt;</c>. A service that is large enough to be split by
/// responsibility (Readmes/DependencyInjection.md) lists the other classes it is made of as <c>Parts</c>: each part
/// <c>P</c> is a class <c>P</c> behind the interface <c>IP</c> in the same namespace, named after the service so the
/// cross-domain guard counts it as the service, and the container holds it as a singleton like the service itself.
/// </summary>
internal sealed record ConvertedService(string Name, string Domain, IReadOnlyList<string>? Parts = null)
{
    public const string ServerNamespace = "SWLOR.Game.Server";

    /// <summary>The static class content code and handlers keep calling.</summary>
    public TypeKey Facade => new(ServerNamespace + ".Service", Name);

    /// <summary>The namespace of the interface and the instance class.</summary>
    public string DomainNamespace => ServerNamespace + ".Domain." + Domain;

    public TypeKey Interface => new(DomainNamespace, "I" + Name + "Service");

    public TypeKey Instance => new(DomainNamespace, Name + "Service");

    public DependencyUnit Unit => new(UnitKind.Service, Name);

    /// <summary>The names of the parts the service is split into, empty when it is a single class.</summary>
    public IReadOnlyList<string> PartNames => Parts ?? [];

    public TypeKey PartInterface(string part) => new(DomainNamespace, "I" + part);

    public TypeKey PartInstance(string part) => new(DomainNamespace, part);
}
