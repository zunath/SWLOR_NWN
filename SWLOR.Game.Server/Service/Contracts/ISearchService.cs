using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface ISearchService
    {
        void OnChestClose(NWPlaceable oChest);
        void OnChestDisturbed(NWPlaceable oChest);
        void OnChestOpen(NWPlaceable oChest);
        void OnChestUsed(NWPlaceable oChest);
    }
}
