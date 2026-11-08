namespace SWLOR.Game.Server.Composition
{
    /// <summary>
    /// How static code reaches a converted service whose static facade has been removed (P2-Z, decision D1 of
    /// <c>Readmes/MaintainabilityRefactorFacadeRemoval.md</c>): one property per removed facade, named like the facade and
    /// typed as the service's interface. Content definitions, native hooks, support types, tools and tests import it with
    /// <c>using static SWLOR.Game.Server.Composition.GameServices;</c>, so a call keeps its text (<c>Taxi.RegisterTaxiDestination(...)</c>)
    /// and now goes through the interface. The four names that are also namespace segments (<c>Combat</c>, <c>Mimicry</c>,
    /// <c>Performance</c>, <c>Prospecting</c>) are written <c>GameServices.Combat.X</c>.
    /// </summary>
    /// <remarks>
    /// A property only resolves: it reads the same per-service slot a facade read (<see cref="FacadeAccessor"/>), so a
    /// call costs what the facade call cost and allocates nothing, and it serves the container a test installed. Classes
    /// the container builds never use it: they take the interface through their constructor (an architecture rule).
    /// The class is partial, one file per domain (<c>GameServices.World.cs</c>); a property is added in the pull request
    /// that deletes its facade, because a type and a property of one name both in scope do not compile.
    /// </remarks>
    public static partial class GameServices
    {
    }
}
