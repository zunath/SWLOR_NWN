using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// A service that has been converted to the dependency-injection pattern (Readmes/DependencyInjection.md): a static
/// facade <c>Service.&lt;Name&gt;</c> that forwards to an instance class <c>&lt;Name&gt;Service</c> behind the interface
/// <c>I&lt;Name&gt;Service</c>, both in <c>Domain.&lt;Domain&gt;</c>.
/// </summary>
internal sealed record ConvertedService(string Name, string Domain)
{
    public const string ServerNamespace = "SWLOR.Game.Server";

    /// <summary>The static class content code and handlers keep calling.</summary>
    public TypeKey Facade => new(ServerNamespace + ".Service", Name);

    /// <summary>The namespace of the interface and the instance class.</summary>
    public string DomainNamespace => ServerNamespace + ".Domain." + Domain;

    public TypeKey Interface => new(DomainNamespace, "I" + Name + "Service");

    public TypeKey Instance => new(DomainNamespace, Name + "Service");

    public DependencyUnit Unit => new(UnitKind.Service, Name);
}
