using NWN;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IBaseService
    {
        void OnModuleUseFeat();
        void OnModuleLoad();
        PCTempBaseData GetPlayerTempData(NWPlayer player);
        void ClearPlayerTempData(NWPlayer player);
        void PurchaseArea(NWPlayer player, NWArea area, string sector);
        void OnModuleHeartbeat();
        PCBaseStructure GetBaseControlTower(int pcBaseID);
        string CanPlaceStructure(NWCreature player, NWItem structureItem, Location targetLocation, int structureID);
        string GetSectorOfLocation(Location targetLocation);
        NWItem ConvertStructureToItem(PCBaseStructure pcBaseStructure, NWObject target);
        void ClearPCBaseByID(int pcBaseID, bool doSave = true);
        void ApplyCraftedItemLocalVariables(NWItem item, BaseStructure structure);
        double GetPowerInUse(int pcBaseID);
        double GetCPUInUse(int pcBaseID);
        NWPlaceable SpawnStructure(NWArea area, int pcBaseStructureID);
        NWPlaceable SpawnBuildingDoor(int doorSpawnProcedure, NWPlaceable building, Location locationOverride = null);
        void JumpPCToBuildingInterior(NWPlayer player, NWArea area);
        string GetPlayerIDOwnerOfSector(Area dbArea, string sector);
        void DoPlayerExitBuildingInstance(NWPlayer player, NWPlaceable door = null);
        void OnModuleNWNXChat(NWPlayer sender);
    }
}