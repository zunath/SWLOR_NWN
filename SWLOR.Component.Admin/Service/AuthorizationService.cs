using SWLOR.Component.Admin.Entity;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Admin.Contracts;
using SWLOR.Shared.Domain.Repositories;
using AuthorizationLevel = SWLOR.Shared.Domain.Admin.Enums.AuthorizationLevel;

namespace SWLOR.Component.Admin.Service
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly IAuthorizedDMRepository _authorizedDMRepository;

        public AuthorizationService(IAuthorizedDMRepository authorizedDMRepository)
        {
            _authorizedDMRepository = authorizedDMRepository;
        }

        /// <summary>
        /// Retrieves the authorization level of a given player.
        /// </summary>
        /// <param name="player">The player whose authorization level we're checking</param>
        /// <returns>The authorization level (player, DM, or admin)</returns>
        public AuthorizationLevel GetAuthorizationLevel(uint player)
        {
            var cdKey = GetPCPublicCDKey(player);

            // Check environment variable for super admin CD Key
            var superAdminCDKey = Environment.GetEnvironmentVariable("SWLOR_SUPER_ADMIN_CD_KEY");
            if (!string.IsNullOrWhiteSpace(superAdminCDKey))
            {
                if (cdKey == superAdminCDKey)
                    return AuthorizationLevel.Admin;
            }

            var existing = _authorizedDMRepository.GetByCDKey(cdKey);
            if (existing == null)
                return AuthorizationLevel.Player;

            return existing.Authorization;
        }
    }
}
