using AuthorizationLevel = SWLOR.Game.Server.Enumeration.AuthorizationLevel;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// The authorization level of a player. The static facade <c>Service.Authorization</c> forwards to the container's
    /// instance of this interface.
    /// </summary>
    public interface IAuthorizationService
    {
        /// <summary>
        /// Retrieves the authorization level of a given player.
        /// </summary>
        /// <param name="player">The player whose authorization level we're checking</param>
        /// <returns>The authorization level (player, DM, or admin)</returns>
        AuthorizationLevel GetAuthorizationLevel(uint player);
    }
}
