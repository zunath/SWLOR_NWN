using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class AuthorizationService: IAuthorizationService
    {
        private readonly INWScript _;
        private readonly IDataService _data;

        public AuthorizationService(INWScript script, IDataService data)
        {
            _ = script;
            _data = data;
        }

        public bool IsPCRegisteredAsDM(NWPlayer player)
        {
            if (player.IsDM) return true;
            if (!player.IsPlayer) return false;

            string cdKey = _.GetPCPublicCDKey(player.Object);

            AuthorizedDM entity = _data.SingleOrDefault<AuthorizedDM>(x => x.CDKey == cdKey && x.IsActive);
            return entity != null;
        }
    }
}
