using SWLOR.Game.Server.Service.ScavengeService;

namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <summary>
    /// Scavenging: owns the engine world of the scavenge sites. The visits themselves are run by
    /// <see cref="IScavengeSessionsService"/>. The static facade <c>Service.Scavenge</c> forwards to it.
    /// </summary>
    public interface IScavengeService
    {
        /// <summary>The one engine world of the scavenge sites.</summary>
        NwnScavengeWorld World { get; }
    }
}
