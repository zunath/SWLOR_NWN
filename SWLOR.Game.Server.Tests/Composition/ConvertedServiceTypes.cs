namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// One class of a converted service resolved to its types: the main one, or an extra (a further static facade or a part).
/// A part has no static facade, so <see cref="Facade"/> is null for it, and so is a facade the branch removed.
/// </summary>
public sealed record ConvertedServiceTypes(string Name, string Domain, Type? Facade, Type Contract, Type Instance)
{
    /// <summary>
    /// For the main class of a service split into parts, the interfaces of its parts (<c>ICombatQueriesService</c> for
    /// <c>Combat</c>), in the order the list names them: its facade may forward a member a part owns straight to the
    /// part. Empty for every other class.
    /// </summary>
    public IReadOnlyList<Type> Parts { get; init; } = Array.Empty<Type>();

    /// <summary>
    /// The full name of the static facade this class had, when its service is marked <c>"facadeRemoved": true</c> (P2-Z):
    /// static code reaches it through <c>GameServices</c> and its handlers are on <see cref="Instance"/>. Null otherwise.
    /// </summary>
    public string? RemovedFacade { get; init; }

    /// <summary>
    /// The name of a part of a split service, when the facade that forwarded to it was removed (P2-ZP): a part never had a
    /// facade of its own, but static code reaches it through the <c>GameServices</c> property of this name. Null otherwise.
    /// </summary>
    public string? AccessorProperty { get; init; }

    /// <summary>
    /// True for the classes of a removed facade whose members went to several classes (<c>Combat</c>; the <c>"members"</c> of
    /// its line in <c>tools/Refactor/RemovedFacades.json</c>): the facade's handlers went to the part that owns each.
    /// </summary>
    public bool IsSplitFacade { get; init; }
}
