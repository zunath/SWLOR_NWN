using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Service.ProspectingService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>Prospecting: owns the engine world of the harvest nodes. The sessions are run through <c>ProspectingService.ProspectingSessions</c>.</summary>
    public static class Prospecting
    {
        private static IProspectingService Instance => FacadeAccessor.Resolve<IProspectingService>();

        /// <summary>The one engine world of the Prospecting sessions.</summary>
        public static NwnProspectingWorld World => Instance.World;
    }
}
