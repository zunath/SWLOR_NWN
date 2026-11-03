using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// A service that has been converted to the dependency-injection pattern (Readmes/DependencyInjection.md): a static
/// facade <c>Service.&lt;Name&gt;</c> that forwards to an instance class <c>&lt;Name&gt;Service</c> behind the interface
/// <c>I&lt;Name&gt;Service</c>, both in <c>Domain.&lt;Domain&gt;</c>. A service made of more than one class lists the others
/// as <see cref="Extras"/>: further static facades (static classes of its namespace with state or event handlers) and
/// parts (the classes a large service is split into by responsibility). Each extra has an instance class of its own.
/// </summary>
internal sealed record ConvertedService(string Name, string Domain)
{
    public const string ServerNamespace = "SWLOR.Game.Server";

    /// <summary>
    /// The classes of the service beyond the main one, sorted by name. Each has its own interface and instance class
    /// (<see cref="ConvertedPieces"/>); an extra facade also has a static class, a part does not.
    /// </summary>
    public IReadOnlyList<ExtraPiece> Extras { get; init; } = Array.Empty<ExtraPiece>();

    /// <summary>The names of the further static facades, relative to <c>SWLOR.Game.Server.Service</c> (<c>QuestService.QuestEncounter</c>).</summary>
    public IReadOnlyList<string> ExtraFacades => Extras.Where(extra => extra.HasFacade).Select(extra => extra.Name).ToList();

    /// <summary>The names of the parts the service is split into (<c>PropertyCatalog</c>).</summary>
    public IReadOnlyList<string> Parts => Extras.Where(extra => !extra.HasFacade).Select(extra => extra.Name).ToList();

    /// <summary>The static class content code and handlers keep calling.</summary>
    public TypeKey Facade => new(ServerNamespace + ".Service", Name);

    /// <summary>The namespace of the interface and the instance class.</summary>
    public string DomainNamespace => ServerNamespace + ".Domain." + Domain;

    public TypeKey Interface => new(DomainNamespace, "I" + Name + "Service");

    public TypeKey Instance => new(DomainNamespace, Name + "Service");

    public DependencyUnit Unit => new(UnitKind.Service, Name);

    /// <summary>The main facade with its interface and instance class, then the pieces of each extra.</summary>
    public IReadOnlyList<ConvertedPieces> Pieces =>
        new[] { new ConvertedPieces(Name, Facade, Interface, Instance) }
            .Concat(Extras.Select(extra => ConvertedPieces.ForExtra(this, extra)))
            .ToList();

    /// <summary>Every static facade of the service: the main one first, then the extra ones (parts have none).</summary>
    public IReadOnlyList<TypeKey> Facades => Pieces.Where(pieces => pieces.Facade is not null).Select(pieces => pieces.Facade!.Value).ToList();
}
