using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Core;
using AuthorizationLevel = SWLOR.Game.Server.Enumeration.AuthorizationLevel;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the authorization levels. The logic lives in <see cref="IAuthorizationService"/>; every member
    /// here forwards to the container's instance.
    /// </summary>
    public static class Authorization
    {
        private static IAuthorizationService Instance => FacadeAccessor.Resolve<IAuthorizationService>();

        /// <summary>
        /// Retrieves the authorization level of a given player.
        /// </summary>
        /// <param name="player">The player whose authorization level we're checking</param>
        /// <returns>The authorization level (player, DM, or admin)</returns>
        public static AuthorizationLevel GetAuthorizationLevel(uint player)
        {
            return Instance.GetAuthorizationLevel(player);
        }
    }
}
