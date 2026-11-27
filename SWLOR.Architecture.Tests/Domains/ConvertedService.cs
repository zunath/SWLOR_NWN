using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// A service that has been converted to the dependency-injection pattern (Readmes/DependencyInjection.md): an instance class
/// <c>&lt;Name&gt;Service</c> behind the interface <c>I&lt;Name&gt;Service</c>, both in <c>Domain.&lt;Domain&gt;</c>, which static
/// code reaches through the <c>GameServices</c> property named <c>&lt;Name&gt;</c>. A service made of more than one class lists
/// the others as <see cref="Extras"/>: further classes (each with a <c>GameServices</c> property) and parts (the classes a large
/// service is split into by responsibility). Each extra has an instance class of its own.
/// </summary>
internal sealed record ConvertedService(string Name, string Domain)
{
    public const string ServerNamespace = "SWLOR.Game.Server";

    /// <summary>
    /// The classes of the service beyond the main one, sorted by name. Each has its own interface and instance class
    /// (<see cref="ConvertedPieces"/>); a class also has a <c>GameServices</c> property, a part does not.
    /// </summary>
    public IReadOnlyList<ExtraPiece> Extras { get; init; } = Array.Empty<ExtraPiece>();

    /// <summary>The static accessor static code reaches the services through: <c>SWLOR.Game.Server.Composition.GameServices</c>.</summary>
    public static TypeKey Accessor => new(ServerNamespace + ".Composition", "GameServices");

    /// <summary>The names of the further classes that have a <c>GameServices</c> property (<c>QuestEncounter</c>).</summary>
    public IReadOnlyList<string> ExtraClasses => Extras.Where(extra => extra.HasAccessorProperty).Select(extra => extra.Name).ToList();

    /// <summary>The names of the parts the service is split into (<c>PropertyCatalog</c>).</summary>
    public IReadOnlyList<string> Parts => Extras.Where(extra => !extra.HasAccessorProperty).Select(extra => extra.Name).ToList();

    /// <summary>The namespace of the interface and the instance class.</summary>
    public string DomainNamespace => ServerNamespace + ".Domain." + Domain;

    public TypeKey Interface => new(DomainNamespace, "I" + Name + "Service");

    public TypeKey Instance => new(DomainNamespace, Name + "Service");

    public DependencyUnit Unit => new(UnitKind.Service, Name);

    /// <summary>The main interface and instance class, then the pieces of each extra.</summary>
    public IReadOnlyList<ConvertedPieces> Pieces =>
        new[] { new ConvertedPieces(Name, Interface, Instance, Name) }
            .Concat(Extras.Select(extra => ConvertedPieces.ForExtra(this, extra)))
            .ToList();
}
