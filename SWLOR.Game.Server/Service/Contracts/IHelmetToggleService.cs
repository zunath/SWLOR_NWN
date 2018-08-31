using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IHelmetToggleService
    {
        void OnModuleItemEquipped();
        void OnModuleItemUnequipped();
        void ToggleHelmetDisplay(NWPlayer player);
    }
}
