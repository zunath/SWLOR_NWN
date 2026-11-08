namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// One class of a converted service resolved to its types: the main one, or an extra (a further static facade or a part).
/// A part has no static facade, so <see cref="Facade"/> is null for it.
/// </summary>
public sealed record ConvertedServiceTypes(string Name, string Domain, Type? Facade, Type Contract, Type Instance)
{
    /// <summary>
    /// For the main class of a service split into parts, the interfaces of its parts (<c>ICombatQueriesService</c> for
    /// <c>Combat</c>), in the order the list names them: its facade may forward a member a part owns straight to the
    /// part. Empty for every other class.
    /// </summary>
    public IReadOnlyList<Type> Parts { get; init; } = Array.Empty<Type>();
}
