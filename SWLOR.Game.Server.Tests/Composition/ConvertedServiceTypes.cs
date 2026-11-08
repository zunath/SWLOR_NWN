namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// One class of a converted service resolved to its types: the main one, or an extra (a further static facade or a part).
/// A part has no static facade, so <see cref="Facade"/> is null for it, and so is a facade the branch removed.
/// </summary>
public sealed record ConvertedServiceTypes(string Name, string Domain, Type? Facade, Type Contract, Type Instance)
{
    /// <summary>
    /// The full name of the static facade this class had, when its service is marked <c>"facadeRemoved": true</c> (P2-Z):
    /// static code reaches it through <c>GameServices</c> and its handlers are on <see cref="Instance"/>. Null otherwise.
    /// </summary>
    public string? RemovedFacade { get; init; }
}
