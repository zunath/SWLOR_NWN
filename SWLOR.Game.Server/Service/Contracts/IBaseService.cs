﻿using System;
using NWN;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
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
        void ToggleInstanceObjectPower(NWArea area, bool isPoweredOn);
        PCBaseStructure GetBaseControlTower(Guid pcBaseID);
        string CanPlaceStructure(NWCreature player, NWItem structureItem, NWLocation targetLocation, int structureID);
        string GetSectorOfLocation(NWLocation targetLocation);
        NWItem ConvertStructureToItem(PCBaseStructure pcBaseStructure, NWObject target);
        void BootPlayersOutOfInstance(Guid pcBaseStructureID);
        void ClearPCBaseByID(Guid pcBaseID, bool displayExplosion = false, bool impoundItems = true);
        void ApplyCraftedItemLocalVariables(NWItem item, BaseStructure structure);
        double GetPowerInUse(Guid pcBaseID);
        double GetCPUInUse(Guid pcBaseID);
        NWPlaceable SpawnStructure(NWArea area, Guid pcBaseStructureID);
        NWPlaceable SpawnBuildingDoor(string doorRule, NWPlaceable building, NWLocation locationOverride = null);
        void JumpPCToBuildingInterior(NWPlayer player, NWArea area);
        Guid? GetPlayerIDOwnerOfSector(Area dbArea, string sector);
        void DoPlayerExitBuildingInstance(NWPlayer player, NWPlaceable door = null);
        void OnModuleNWNXChat(NWPlayer sender);
        int CalculateMaxShieldHP(PCBaseStructure controlTower);
        int CalculateMaxFuel(Guid pcBaseID);
        int CalculateMaxReinforcedFuel(Guid pcBaseID);
        int CalculateResourceCapacity(Guid pcBaseID);
        double GetMaxBaseCPU(Guid pcBaseID);
        double GetMaxBasePower(Guid pcBaseID);
        string UpgradeControlTower(NWCreature user, NWItem item, NWObject target);
        NWPlaceable FindPlaceableFromStructureID(string pcBaseStructureID);
        NWArea GetAreaInstance(Guid instanceID, bool isBase);
        NWArea CreateAreaInstance(NWPlayer player, Guid instanceID, bool isBase);
        void OnAreaEnter();
    }
}