using SWLOR.Shared.Domain.Admin.Enums;

namespace SWLOR.Shared.Domain.Admin.Contracts;

public interface IAuthorizationService
{
    /// <summary>
    /// Retrieves the authorization level of a given player.
    /// </summary>
    /// <param name="player">The player whose authorization level we're checking</param>
    /// <returns>The authorization level (player, DM, or admin)</returns>
    AuthorizationLevel GetAuthorizationLevel(uint player);
}