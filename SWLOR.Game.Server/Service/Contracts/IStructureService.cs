using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.ValueObject.Structure;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IStructureService
    {
        string BuildMenuHeader(int blueprintID);
        int CanPCBuildInLocation(NWPlayer player, Location targetLocation, StructurePermission permission);
        void ChangeBuildPrivacy(int flagID, int buildPrivacyID);
        NWPlaceable CompleteStructure(NWPlaceable constructionSite);
        NWPlaceable CreateBuildingDoor(Location houseLocation, int structureID);
        void CreateConstructionSite(NWPlayer player, Location location);
        void CreateConstructionSiteFromEntity(ConstructionSite entity);
        void CreateStructureFromEntity(PCTerritoryFlagsStructure structure);
        List<TerritoryFlagPermission> GetAllTerritorySelectablePermissions();
        List<BuildingInterior> GetBuildingInteriorsByCategoryID(int buildingCategoryID);
        BuildingOwners GetBuildingOwners(int territoryFlagID, int structureID);
        ConstructionSite GetConstructionSiteByID(int constructionSiteID);
        int GetConstructionSiteID(NWPlaceable site);
        TerritoryStructureCount GetNumberOfStructuresInTerritory(int flagID);
        PCTerritoryFlagsStructure GetPCStructureByID(int structureID);
        PCTerritoryFlag GetPCTerritoryFlagByBuildingStructureID(int buildingStructureID);
        PCTerritoryFlag GetPCTerritoryFlagByID(int territoryFlagID);
        List<PCTerritoryFlagsPermission> GetPermissionsByFlagID(int flagID);
        List<PCTerritoryFlagsPermission> GetPermissionsByPlayerID(string playerID, int flagID);
        int GetPlaceableStructureID(NWPlaceable structure);
        StructureBlueprint GetStructureBlueprintByID(int blueprintID);
        List<StructureCategory> GetStructureCategories(string playerID);
        List<StructureCategory> GetStructureCategoriesByType(string playerID, bool isTerritoryFlagCategory, bool isVanity, bool isSpecial, bool isResource, bool isBuilding);
        List<StructureBlueprint> GetStructuresByCategoryAndType(string playerID, int structureCategoryID, bool isVanity, bool isSpecial, bool isResource, bool isBuilding);
        List<StructureBlueprint> GetStructuresForPCByCategory(string playerID, int structureCategoryID);
        int GetTerritoryFlagID(NWObject flag);
        NWObject GetTerritoryFlagOwnerOfLocation(Location location);
        bool IsConstructionSiteValid(NWPlaceable site);
        bool IsPCMovingStructure(NWPlayer oPC);
        void JumpPCToBuildingInterior(NWPlayer player, NWArea area);
        void LogQuickBuildAction(NWPlayer dm, NWPlaceable completedStructure);
        void MoveStructure(NWPlayer oPC, Location location);
        void OnModuleLoad();
        void OnModuleNWNXChat(NWPlayer sender);
        bool PlayerHasPermission(NWPlayer player, StructurePermission permissionType, int flagID);
        void PreviewBuildingInterior(NWPlayer player, int buildingInteriorID);
        void RazeConstructionSite(NWPlayer player, NWPlaceable site, bool recoverMaterials);
        void RazeTerritory(NWPlaceable flag);
        void SaveChanges();
        void SelectBlueprint(NWPlayer player, NWPlaceable constructionSite, int blueprintID);
        void SetIsPCMovingStructure(NWPlayer player, NWPlaceable structure, bool isMoving);
        void SetStructureCustomName(NWPlayer player, NWPlaceable structure, string customName);
        void TogglePermissionForPlayer(PCTerritoryFlagsPermission foundPerm, string playerID, int permissionID, int flagID);
        void TransferBuildingOwnership(NWArea area, string newOwnerPlayerID);
        void TransferTerritoryOwnership(NWPlaceable flag, string newOwnerPlayerID);
        bool WillBlueprintOverlapWithExistingFlags(Location location, int blueprintID);
        void DeleteContainerItemByGlobalID(string globalID);
        void OnModuleUseFeat();
    }
}
