using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IFarmingService
    {
        void HarvestPlant(NWPlayer player, NWItem shovel, NWPlaceable plant);
        string OnModuleExamine(string existingDescription, NWObject examinedObject);
        void OnModuleLoad();
        void RemoveGrowingPlant(NWPlaceable plant);
        GrowingPlant GetGrowingPlantByID(int growingPlantID);
        Plant GetPlantByID(int plantID);
    }
}
