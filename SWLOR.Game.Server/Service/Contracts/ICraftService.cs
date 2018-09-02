using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface ICraftService
    {
        string BuildBlueprintHeader(NWPlayer player, int blueprintID);
        void CraftItem(NWPlayer oPC, NWPlaceable device);
        CraftBlueprint GetBlueprintByID(int craftBlueprintID);
        List<CraftBlueprintCategory> GetCategoriesAvailableToPC(string playerID);
        List<CraftBlueprintCategory> GetCategoriesAvailableToPCByDeviceID(string playerID, int deviceID);
        List<CraftBlueprint> GetPCBlueprintsByCategoryID(string playerID, int categoryID);
        List<CraftBlueprint> GetPCBlueprintsByDeviceAndCategoryID(string playerID, int deviceID, int categoryID);
        string GetIngotResref(string oreResref);
        int GetIngotLevel(string oreResref);
        int GetIngotPerkLevel(string oreResref);
        CraftingData GetPlayerCraftingData(NWPlayer player);
        void ClearPlayerCraftingData(NWPlayer player, bool destroyComponents = false);
        string CalculateDifficultyDescription(int pcLevel, int blueprintLevel);
        int CalculatePCEffectiveLevel(NWPlayer pcGO, NWPlaceable device, int skillRank);
    }
}
