namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// One class of a converted service resolved to its types: the main one, or an extra (a further class or a part).
/// </summary>
public sealed record ConvertedServiceTypes(string Name, string Domain, Type Contract, Type Instance)
{
    /// <summary>
    /// For the main class of a service split into parts, the interfaces of its parts (<c>ICombatQueriesService</c> for
    /// <c>Combat</c>), in the order the list names them. Empty for every other class.
    /// </summary>
    public IReadOnlyList<Type> Parts { get; init; } = Array.Empty<Type>();

    /// <summary>
    /// The full name of the static facade this class had before the branch removed it (P2-Z), when it had one: only
    /// <c>tools/Refactor/HandlerRenames.txt</c> still names it, as the master name of each handler that moved to
    /// <see cref="Instance"/>. Null for a part, which never had one.
    /// </summary>
    public string? RemovedFacade { get; init; }

    /// <summary>
    /// The name of the <c>GameServices</c> property static code reaches this class through: the service name, the type name of a
    /// further class, or the name of a part of a split service (<c>Combat</c>). Null for a part static code does not reach.
    /// </summary>
    public string? AccessorProperty { get; init; }

    /// <summary>
    /// True for the classes of a service whose members went to several classes (<c>Combat</c>; the <c>"members"</c> of its
    /// line in <c>tools/Refactor/RemovedFacades.json</c>): the facade's handlers went to the part that owns each.
    /// </summary>
    public bool IsSplitFacade { get; init; }
}
