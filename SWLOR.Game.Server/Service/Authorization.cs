using System;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public class Authorization
    {
        /// <summary>
        /// Retrieves the authorization level of a given player.
        /// </summary>
        /// <param name="player">The player whose authorization level we're checking</param>
        /// <returns>The authorization level (player, DM, or admin)</returns>
        public static AuthorizationLevel GetAuthorizationLevel(uint player)
        {
            var cdKey = GetPCPublicCDKey(player);

            // Check environment variable for super admin CD Key
            var superAdminCDKey = Environment.GetEnvironmentVariable("SWLOR_SUPER_ADMIN_CD_KEY");
            if (!string.IsNullOrWhiteSpace(superAdminCDKey))
            {
                if (cdKey == superAdminCDKey)
                    return AuthorizationLevel.Admin;
            }

            var dmList = DB.GetList<AuthorizedDM>("All", "AuthorizedDM") ?? new EntityList<AuthorizedDM>();

            var existing = dmList.FirstOrDefault(x => x.CDKey == cdKey);
            if (existing == null)
                return AuthorizationLevel.Player;

            return existing.Authorization;
        }
    }
}
