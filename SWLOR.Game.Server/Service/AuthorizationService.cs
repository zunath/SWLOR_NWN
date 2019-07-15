using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;



namespace SWLOR.Game.Server.Service
{
    public static class AuthorizationService
    {
        public static bool IsPCRegisteredAsDM(NWPlayer player)
        {
            if (player.IsDM) return true;
            if (!player.IsPlayer) return false;

            string cdKey = _.GetPCPublicCDKey(player.Object);

            AuthorizedDM entity = DataService.AuthorizedDM.GetByCDKeyAndActiveOrDefault(cdKey);
            return entity != null;
        }
    }
}
