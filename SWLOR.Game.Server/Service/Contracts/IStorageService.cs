using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IStorageService
    {
        void OnChestDisturbed(NWPlaceable oChest);
        void OnChestOpened(NWPlaceable oChest);
    }
}
