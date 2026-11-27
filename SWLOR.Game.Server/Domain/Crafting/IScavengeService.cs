using SWLOR.Game.Server.Domain.Crafting;

namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <summary>
    /// Scavenging: owns the engine world of the scavenge sites. The visits themselves are run by
    /// <see cref="IScavengeSessionsService"/>. Static callers reach the container's instance through <c>GameServices.Scavenge</c>.
    /// </summary>
    public interface IScavengeService
    {
        /// <summary>The one engine world of the scavenge sites.</summary>
        NwnScavengeWorld World { get; }
    }
}
