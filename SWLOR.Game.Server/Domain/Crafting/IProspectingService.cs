using SWLOR.Game.Server.Service.ProspectingService;

namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <summary>
    /// Prospecting: owns the engine world of the harvest nodes. The sessions themselves are run by
    /// <see cref="IProspectingSessionsService"/>. The static facade <c>Service.Prospecting</c> forwards to it.
    /// </summary>
    public interface IProspectingService
    {
        /// <summary>The one engine world of the Prospecting sessions.</summary>
        NwnProspectingWorld World { get; }
    }
}
