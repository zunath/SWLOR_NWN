namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// A class of a converted service beyond the main one, as <c>ConvertedServices.json</c> lists it under <c>extras</c>:
/// <c>{ "facade": "QuestService.QuestEncounter" }</c> for a further static facade (a static class of the service's
/// namespace with state or event handlers, which stays static so handler names do not change), or
/// <c>{ "part": "PropertyCatalog" }</c> for a class a large service is split into by responsibility (no facade: the
/// main facade forwards to it). Either way the piece has an instance class <c>&lt;Type&gt;Service</c> behind an interface
/// <c>I&lt;Type&gt;Service</c> in the domain of the service, registered in the container like the service itself.
/// </summary>
/// <param name="Name">For a facade, the type name relative to <c>SWLOR.Game.Server.Service</c>; for a part, the plain name.</param>
/// <param name="HasFacade">True for an extra facade, false for a part.</param>
internal sealed record ExtraPiece(string Name, bool HasFacade)
{
    public static ExtraPiece Facade(string name) => new(name, true);

    public static ExtraPiece Part(string name) => new(name, false);

    /// <summary>The last segment of the name: the type the interface and the instance class are named after.</summary>
    public string TypeName => TypeNameOf(Name);

    /// <summary>The JSON property that lists this kind of extra.</summary>
    public string Kind => HasFacade ? "facade" : "part";

    /// <summary>How a failure message names the extra: <c>extra facade QuestService.QuestEncounter</c> or <c>part PropertyCatalog</c>.</summary>
    public string Describe() => HasFacade ? $"extra facade {Name}" : $"part {Name}";

    public static string TypeNameOf(string name) => name[(name.LastIndexOf('.') + 1)..];
}
