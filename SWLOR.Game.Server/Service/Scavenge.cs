using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Service.ScavengeService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>Scavenging: owns the engine world of the scavenge sites. The visits are run through <c>ScavengeService.ScavengeSessions</c>.</summary>
    public static class Scavenge
    {
        private static IScavengeService Instance => FacadeAccessor.Resolve<IScavengeService>();

        /// <summary>The one engine world of the scavenge sites.</summary>
        public static NwnScavengeWorld World => Instance.World;
    }
}
