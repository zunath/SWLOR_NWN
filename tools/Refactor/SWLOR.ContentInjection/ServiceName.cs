namespace SWLOR.ContentInjection;

/// <summary>
/// A name content reaches a service by: a former <c>GameServices</c> property (<c>Stat</c>, typed <c>IStatService</c>), a content
/// singleton under its class name (<c>PlaceableScripts</c>), or the content factory (<c>Content</c>).
/// </summary>
/// <param name="Name">The name the call text uses.</param>
/// <param name="Type">The type of the injected property, as written (<c>IStatService</c>, <c>PlaceableScripts</c>).</param>
/// <param name="Namespace">The namespace of <paramref name="Type"/>.</param>
/// <param name="IsSingleton">True for a content singleton (injected under its class name, typed as the class).</param>
internal sealed record ServiceName(string Name, string Type, string Namespace, bool IsSingleton = false)
{
    public const string ContentName = "Content";
    public const string ContentType = "IContentFactory";
    public const string ContentNamespace = "SWLOR.Game.Server.Core";

    public static ServiceName Content { get; } = new(ContentName, ContentType, ContentNamespace);

    public string FullType => Namespace + "." + Type;
}
