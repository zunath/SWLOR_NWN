using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// A service that has been converted to the dependency-injection pattern (Readmes/DependencyInjection.md): a static
/// facade <c>Service.&lt;Name&gt;</c> that forwards to an instance class <c>&lt;Name&gt;Service</c> behind the interface
/// <c>I&lt;Name&gt;Service</c>, both in <c>Domain.&lt;Domain&gt;</c>. A service whose namespace holds other static classes
/// with state or event handlers lists them as <see cref="ExtraFacades"/>; each forwards to an instance class of its own.
/// </summary>
internal sealed record ConvertedService(string Name, string Domain)
{
    public const string ServerNamespace = "SWLOR.Game.Server";

    /// <summary>
    /// Further static facades of the service, as names relative to <c>SWLOR.Game.Server.Service</c>
    /// (<c>QuestService.QuestEncounter</c>). Each has its own interface and instance class (<see cref="ConvertedPieces"/>).
    /// </summary>
    public IReadOnlyList<string> ExtraFacades { get; init; } = Array.Empty<string>();

    /// <summary>The static class content code and handlers keep calling.</summary>
    public TypeKey Facade => new(ServerNamespace + ".Service", Name);

    /// <summary>The namespace of the interface and the instance class.</summary>
    public string DomainNamespace => ServerNamespace + ".Domain." + Domain;

    public TypeKey Interface => new(DomainNamespace, "I" + Name + "Service");

    public TypeKey Instance => new(DomainNamespace, Name + "Service");

    public DependencyUnit Unit => new(UnitKind.Service, Name);

    /// <summary>The main facade with its interface and instance class, then those of each extra facade.</summary>
    public IReadOnlyList<ConvertedPieces> Pieces =>
        new[] { new ConvertedPieces(Name, Facade, Interface, Instance) }
            .Concat(ExtraFacades.Select(facade => ConvertedPieces.ForExtraFacade(this, facade)))
            .ToList();

    /// <summary>Every static facade of the service: the main one first, then the extra ones.</summary>
    public IReadOnlyList<TypeKey> Facades => Pieces.Select(pieces => pieces.Facade).ToList();
}
