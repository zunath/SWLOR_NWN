using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IAuthorizationService
    {
        bool IsPCRegisteredAsDM(NWPlayer player);
    }
}
