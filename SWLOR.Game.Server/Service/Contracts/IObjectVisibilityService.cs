using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IObjectVisibilityService
    {
        void AdjustVisibility(NWPlayer player, NWObject target, bool isVisible);
        void OnClientEnter();
        void OnModuleLoad();
    }
}