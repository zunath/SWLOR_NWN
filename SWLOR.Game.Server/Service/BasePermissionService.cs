using System;

using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class BasePermissionService : IBasePermissionService
    {
        private readonly IDataService _data;

        public BasePermissionService(IDataService data)
        {
            _data = data;
        }

        public bool HasBasePermission(NWPlayer player, Guid pcBaseID, BasePermission permission)
        {
            if (player.IsDM) return true;

            // Public permissions take priority over all other permissions. Check those first.
            var publicBasePermission = _data.SingleOrDefault<PCBasePermission>(x => x.PCBaseID == pcBaseID &&
                                                                                x.IsPublicPermission);

            if (publicBasePermission != null)
            {
                if (permission == BasePermission.CanPlaceEditStructures && publicBasePermission.CanPlaceEditStructures) return true;
                if (permission == BasePermission.CanAccessStructureInventory && publicBasePermission.CanAccessStructureInventory) return true;
                if (permission == BasePermission.CanManageBaseFuel && publicBasePermission.CanManageBaseFuel) return true;
                if (permission == BasePermission.CanExtendLease && publicBasePermission.CanExtendLease) return true;
                if (permission == BasePermission.CanAdjustPermissions && publicBasePermission.CanAdjustPermissions) return true;
                if (permission == BasePermission.CanEnterBuildings && publicBasePermission.CanEnterBuildings) return true;
                if (permission == BasePermission.CanRetrieveStructures && publicBasePermission.CanRetrieveStructures) return true;
                if (permission == BasePermission.CanCancelLease && publicBasePermission.CanCancelLease) return true;
                if (permission == BasePermission.CanRenameStructures && publicBasePermission.CanRenameStructures) return true;
                if (permission == BasePermission.CanEditPrimaryResidence && publicBasePermission.CanEditPrimaryResidence) return true;
                if (permission == BasePermission.CanRemovePrimaryResidence && publicBasePermission.CanRemovePrimaryResidence) return true;
                if (permission == BasePermission.CanChangeStructureMode && publicBasePermission.CanChangeStructureMode) return true;
                if (permission == BasePermission.CanAdjustPublicPermissions && publicBasePermission.CanAdjustPublicPermissions) return true;

            }

            // No matching public permissions. Now check the base permissions for this player.
            var dbPermission = _data.GetAll<PCBasePermission>()
                .SingleOrDefault(x => x.PCBaseID == pcBaseID && 
                                      x.PlayerID == player.GlobalID &&
                                      !x.IsPublicPermission);
            
            if (dbPermission == null) return false;

            if (permission == BasePermission.CanPlaceEditStructures && dbPermission.CanPlaceEditStructures) return true;
            if (permission == BasePermission.CanAccessStructureInventory && dbPermission.CanAccessStructureInventory) return true;
            if (permission == BasePermission.CanManageBaseFuel && dbPermission.CanManageBaseFuel) return true;
            if (permission == BasePermission.CanExtendLease && dbPermission.CanExtendLease) return true;
            if (permission == BasePermission.CanAdjustPermissions && dbPermission.CanAdjustPermissions) return true;
            if (permission == BasePermission.CanEnterBuildings && dbPermission.CanEnterBuildings) return true;
            if (permission == BasePermission.CanRetrieveStructures && dbPermission.CanRetrieveStructures) return true;
            if (permission == BasePermission.CanCancelLease && dbPermission.CanCancelLease) return true;
            if (permission == BasePermission.CanRenameStructures && dbPermission.CanRenameStructures) return true;
            if (permission == BasePermission.CanEditPrimaryResidence && dbPermission.CanEditPrimaryResidence) return true;
            if (permission == BasePermission.CanRemovePrimaryResidence && dbPermission.CanRemovePrimaryResidence) return true;
            if (permission == BasePermission.CanChangeStructureMode && dbPermission.CanChangeStructureMode) return true;
            if (permission == BasePermission.CanAdjustPublicPermissions && dbPermission.CanAdjustPublicPermissions) return true;

            return false;
        }

        public bool HasStructurePermission(NWPlayer player, Guid pcBaseStructureID, StructurePermission permission)
        {
            if (player.IsDM) return true;

            var dbStructure = _data.GetAll<PCBaseStructure>().Single(x => x.ID == pcBaseStructureID);

            // Public base permissions take priority over all other permissions. Check those first.
            var publicBasePermission = _data.SingleOrDefault<PCBasePermission>(x => x.PCBaseID == dbStructure.PCBaseID &&
                                                                                x.IsPublicPermission);

            if (publicBasePermission != null)
            {
                if (permission == StructurePermission.CanAccessStructureInventory && publicBasePermission.CanAccessStructureInventory) return true;
                if (permission == StructurePermission.CanPlaceEditStructures && publicBasePermission.CanPlaceEditStructures) return true;
                if (permission == StructurePermission.CanEnterBuilding && publicBasePermission.CanEnterBuildings) return true;
                if (permission == StructurePermission.CanRetrieveStructures && publicBasePermission.CanRetrieveStructures) return true;
                if (permission == StructurePermission.CanAdjustPermissions && publicBasePermission.CanAdjustPermissions) return true;
                if (permission == StructurePermission.CanRenameStructures && publicBasePermission.CanRenameStructures) return true;
                if (permission == StructurePermission.CanEditPrimaryResidence && publicBasePermission.CanEditPrimaryResidence) return true;
                if (permission == StructurePermission.CanRemovePrimaryResidence && publicBasePermission.CanRemovePrimaryResidence) return true;
                if (permission == StructurePermission.CanChangeStructureMode && publicBasePermission.CanChangeStructureMode) return true;
                if (permission == StructurePermission.CanAdjustPublicPermissions && publicBasePermission.CanAdjustPublicPermissions) return true;
            }

            // Public structure permissions are the next thing we check.
            var publicStructurePermission = _data.SingleOrDefault<PCBaseStructurePermission>(x => x.PCBaseStructureID == dbStructure.ID &&
                                                                                                  x.IsPublicPermission);

            if (publicStructurePermission != null)
            {
                if (permission == StructurePermission.CanAccessStructureInventory && publicStructurePermission.CanAccessStructureInventory) return true;
                if (permission == StructurePermission.CanPlaceEditStructures && publicStructurePermission.CanPlaceEditStructures) return true;
                if (permission == StructurePermission.CanEnterBuilding && publicStructurePermission.CanEnterBuilding) return true;
                if (permission == StructurePermission.CanRetrieveStructures && publicStructurePermission.CanRetrieveStructures) return true;
                if (permission == StructurePermission.CanAdjustPermissions && publicStructurePermission.CanAdjustPermissions) return true;
                if (permission == StructurePermission.CanRenameStructures && publicStructurePermission.CanRenameStructures) return true;
                if (permission == StructurePermission.CanEditPrimaryResidence && publicStructurePermission.CanEditPrimaryResidence) return true;
                if (permission == StructurePermission.CanRemovePrimaryResidence && publicStructurePermission.CanRemovePrimaryResidence) return true;
                if (permission == StructurePermission.CanChangeStructureMode && publicStructurePermission.CanChangeStructureMode) return true;
                if (permission == StructurePermission.CanAdjustPublicPermissions && publicStructurePermission.CanAdjustPublicPermissions) return true;
            }

            // Base permissions take priority over structure permissions. Check those next.
            var basePermission = _data.SingleOrDefault<PCBasePermission>(x => x.PlayerID == player.GlobalID && 
                                                                              x.PCBaseID == dbStructure.PCBaseID &&
                                                                              !x.IsPublicPermission);


            if (basePermission != null)
            {
                if (permission == StructurePermission.CanAccessStructureInventory && basePermission.CanAccessStructureInventory) return true;
                if (permission == StructurePermission.CanPlaceEditStructures && basePermission.CanPlaceEditStructures) return true;
                if (permission == StructurePermission.CanEnterBuilding && basePermission.CanEnterBuildings) return true;
                if (permission == StructurePermission.CanRetrieveStructures && basePermission.CanRetrieveStructures) return true;
                if (permission == StructurePermission.CanAdjustPermissions && basePermission.CanAdjustPermissions) return true;
                if (permission == StructurePermission.CanRenameStructures && basePermission.CanRenameStructures) return true;
                if (permission == StructurePermission.CanEditPrimaryResidence && basePermission.CanEditPrimaryResidence) return true;
                if (permission == StructurePermission.CanRemovePrimaryResidence && basePermission.CanRemovePrimaryResidence) return true;
                if (permission == StructurePermission.CanChangeStructureMode && basePermission.CanChangeStructureMode) return true;
                if (permission == StructurePermission.CanAdjustPublicPermissions && basePermission.CanAdjustPublicPermissions) return true;
            }

            // Didn't find a base permission. Check the structure permissions.
            var structurePermission = _data.GetAll<PCBaseStructurePermission>()
                .SingleOrDefault(x => x.PCBaseStructureID == pcBaseStructureID && 
                                      x.PlayerID == player.GlobalID &&
                                      !x.IsPublicPermission);
            if (structurePermission == null) return false;

            if (permission == StructurePermission.CanAccessStructureInventory && structurePermission.CanAccessStructureInventory) return true;
            if (permission == StructurePermission.CanPlaceEditStructures && structurePermission.CanPlaceEditStructures) return true;
            if (permission == StructurePermission.CanEnterBuilding && structurePermission.CanEnterBuilding) return true;
            if (permission == StructurePermission.CanRetrieveStructures && structurePermission.CanRetrieveStructures) return true;
            if (permission == StructurePermission.CanAdjustPermissions && structurePermission.CanAdjustPermissions) return true;
            if (permission == StructurePermission.CanRenameStructures && structurePermission.CanRenameStructures) return true;
            if (permission == StructurePermission.CanEditPrimaryResidence && structurePermission.CanEditPrimaryResidence) return true;
            if (permission == StructurePermission.CanRemovePrimaryResidence && structurePermission.CanRemovePrimaryResidence) return true;
            if (permission == StructurePermission.CanChangeStructureMode && structurePermission.CanChangeStructureMode) return true;
            if (permission == StructurePermission.CanAdjustPublicPermissions && structurePermission.CanAdjustPublicPermissions) return true;

            // Player doesn't have permission.
            return false;
        }

        public void GrantBasePermissions(NWPlayer player, Guid pcBaseID, params BasePermission[] permissions)
        {
            var dbPermission = _data.GetAll<PCBasePermission>()
                .SingleOrDefault(x => x.PCBaseID == pcBaseID && 
                                      x.PlayerID == player.GlobalID &&
                                      !x.IsPublicPermission);
            var action = DatabaseActionType.Update;

            if (dbPermission == null)
            {
                dbPermission = new PCBasePermission
                {
                    PCBaseID = pcBaseID,
                    PlayerID = player.GlobalID
                };
                action = DatabaseActionType.Insert;
            }

            foreach (var permission in permissions)
            {
                switch (permission)
                {
                    case BasePermission.CanAccessStructureInventory:
                        dbPermission.CanAccessStructureInventory = true;
                        break;
                    case BasePermission.CanPlaceEditStructures:
                        dbPermission.CanPlaceEditStructures = true;
                        break;
                    case BasePermission.CanManageBaseFuel:
                        dbPermission.CanManageBaseFuel = true;
                        break;
                    case BasePermission.CanExtendLease:
                        dbPermission.CanExtendLease = true;
                        break;
                    case BasePermission.CanAdjustPermissions:
                        dbPermission.CanAdjustPermissions = true;
                        break;
                    case BasePermission.CanEnterBuildings:
                        dbPermission.CanEnterBuildings = true;
                        break;
                    case BasePermission.CanRetrieveStructures:
                        dbPermission.CanRetrieveStructures = true;
                        break;
                    case BasePermission.CanRenameStructures:
                        dbPermission.CanRenameStructures = true;
                        break;
                    case BasePermission.CanCancelLease:
                        dbPermission.CanCancelLease = true;
                        break;
                    case BasePermission.CanEditPrimaryResidence:
                        dbPermission.CanEditPrimaryResidence = true;
                        break;
                    case BasePermission.CanRemovePrimaryResidence:
                        dbPermission.CanRemovePrimaryResidence = true;
                        break;
                    case BasePermission.CanChangeStructureMode:
                        dbPermission.CanChangeStructureMode = true;
                        break;
                    case BasePermission.CanAdjustPublicPermissions:
                        dbPermission.CanAdjustPublicPermissions = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            _data.SubmitDataChange(dbPermission, action);
        }

        public void GrantStructurePermissions(NWPlayer player, Guid pcBaseStructureID, params StructurePermission[] permissions)
        {
            var dbPermission = _data.SingleOrDefault<PCBaseStructurePermission>(x => x.PCBaseStructureID == pcBaseStructureID && 
                                                                                     x.PlayerID == player.GlobalID && 
                                                                                     !x.IsPublicPermission);
            var action = DatabaseActionType.Update;

            if (dbPermission == null)
            {
                dbPermission = new PCBaseStructurePermission
                {
                    PCBaseStructureID = pcBaseStructureID,
                    PlayerID = player.GlobalID
                };
                action = DatabaseActionType.Insert;
            }

            foreach (var permission in permissions)
            {
                switch (permission)
                {
                    case StructurePermission.CanPlaceEditStructures:
                        dbPermission.CanPlaceEditStructures = true;
                        break;
                    case StructurePermission.CanAccessStructureInventory:
                        dbPermission.CanAccessStructureInventory = true;
                        break;
                    case StructurePermission.CanEnterBuilding:
                        dbPermission.CanEnterBuilding = true;
                        break;
                    case StructurePermission.CanRetrieveStructures:
                        dbPermission.CanRetrieveStructures = true;
                        break;
                    case StructurePermission.CanAdjustPermissions:
                        dbPermission.CanAdjustPermissions = true;
                        break;
                    case StructurePermission.CanRenameStructures:
                        dbPermission.CanRenameStructures = true;
                        break;
                    case StructurePermission.CanEditPrimaryResidence:
                        dbPermission.CanEditPrimaryResidence = true;
                        break;
                    case StructurePermission.CanRemovePrimaryResidence:
                        dbPermission.CanRemovePrimaryResidence = true;
                        break;
                    case StructurePermission.CanChangeStructureMode:
                        dbPermission.CanChangeStructureMode = true;
                        break;
                    case StructurePermission.CanAdjustPublicPermissions:
                        dbPermission.CanAdjustPublicPermissions = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            _data.SubmitDataChange(dbPermission, action);
        }        
    }
}
