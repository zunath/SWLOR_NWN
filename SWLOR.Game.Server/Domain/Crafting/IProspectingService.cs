using SWLOR.Game.Server.Domain.Crafting;

namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <summary>
    /// Prospecting: owns the engine world of the harvest nodes. The sessions themselves are run by
    /// <see cref="IProspectingSessionsService"/>. Static callers reach the container's instance through <c>GameServices.Prospecting</c>.
    /// </summary>
    public interface IProspectingService
    {
        /// <summary>The one engine world of the Prospecting sessions.</summary>
        NwnProspectingWorld World { get; }
    }
}
