namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// A class of a converted service beyond the main one, as <c>ConvertedServices.json</c> lists it under <c>extras</c>:
/// <c>{ "class": "QuestEncounter" }</c> for a further class of the service that static code reaches through a
/// <c>GameServices</c> property named like its type, or <c>{ "part": "PropertyCatalog" }</c> for a class a large service is
/// split into by responsibility (the other classes of the service take it through their constructors). Either way the piece
/// has an instance class <c>&lt;Type&gt;Service</c> behind an interface <c>I&lt;Type&gt;Service</c> in the domain of the
/// service, registered in the container like the service itself.
/// </summary>
/// <param name="Name">The plain type name the interface and the instance class are named after.</param>
/// <param name="HasAccessorProperty">True for a class static code reaches through <c>GameServices</c>, false for a part.</param>
internal sealed record ExtraPiece(string Name, bool HasAccessorProperty)
{
    public static ExtraPiece Class(string name) => new(name, true);

    public static ExtraPiece Part(string name) => new(name, false);

    /// <summary>The JSON property that lists this kind of extra.</summary>
    public string Kind => HasAccessorProperty ? "class" : "part";

    /// <summary>How a failure message names the extra: <c>class QuestEncounter</c> or <c>part PropertyCatalog</c>.</summary>
    public string Describe() => $"{Kind} {Name}";
}
