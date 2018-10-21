using System.Collections.Generic;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface ICraftService
    {
        string BuildBlueprintHeader(NWPlayer player, long blueprintID, bool showAddedComponentList);
        void CraftItem(NWPlayer oPC, NWPlaceable device);
        CraftBlueprint GetBlueprintByID(long craftBlueprintID);
        List<CraftBlueprintCategory> GetCategoriesAvailableToPC(string playerID);
        List<CraftBlueprintCategory> GetCategoriesAvailableToPCByDeviceID(string playerID, int deviceID);
        List<CraftBlueprint> GetPCBlueprintsByCategoryID(string playerID, long categoryID);
        List<CraftBlueprint> GetPCBlueprintsByDeviceAndCategoryID(string playerID, int deviceID, long categoryID);
        string GetIngotResref(string oreResref);
        int GetIngotLevel(string oreResref);
        int GetIngotPerkLevel(string oreResref);
        CraftingData GetPlayerCraftingData(NWPlayer player);
        void ClearPlayerCraftingData(NWPlayer player, bool destroyComponents = false);
        string CalculateDifficultyDescription(int pcLevel, int blueprintLevel);
        int CalculatePCEffectiveLevel(NWPlayer player, int skillRank, SkillType skill);
        void OnNWNXChat();
        void OnModuleUseFeat();
    }
}
