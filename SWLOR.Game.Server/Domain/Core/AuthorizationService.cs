using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using AuthorizationLevel = SWLOR.Game.Server.Enumeration.AuthorizationLevel;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// Authorization: the logic that used to be static in <c>Service.Authorization</c>, moved here unchanged. The facade
    /// forwards every call to the container's instance.
    /// </summary>
    public class AuthorizationService : IAuthorizationService
    {
        private readonly IDBService _db;

        public AuthorizationService(IDBService db)
        {
            _db = db;
        }

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

            var query = new DBQuery<AuthorizedDM>()
                .AddFieldSearch(nameof(AuthorizedDM.CDKey), cdKey, false);
            var existing = _db.Search(query).FirstOrDefault();
            if (existing == null)
                return AuthorizationLevel.Player;

            return existing.Authorization;
        }
    }
}
