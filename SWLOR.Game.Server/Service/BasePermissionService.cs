using System;
using System.Data.Entity.Migrations;
using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class BasePermissionService : IBasePermissionService
    {
        private readonly IDataContext _db;

        public BasePermissionService(IDataContext db)
        {
            _db = db;
        }

        public bool HasBasePermission(NWPlayer player, int pcBaseID, BasePermission permission)
        {
            if (player.IsDM) return true;

            var dbPermission = _db.PCBasePermissions.SingleOrDefault(x => x.PCBaseID == pcBaseID && x.PlayerID == player.GlobalID);
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

            return false;
        }

        public bool HasStructurePermission(NWPlayer player, int pcBaseStructureID, StructurePermission permission)
        {
            if (player.IsDM) return true;

            // Base permissions take priority over structure permissions. Check those first.
            var dbStructure = _db.PCBaseStructures.Single(x => x.PCBaseStructureID == pcBaseStructureID);
            var basePermission = dbStructure.PCBase.PCBasePermissions.SingleOrDefault(x => x.PlayerID == player.GlobalID);

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
            }

            // Didn't find a base permission. Check the structure permissions.
            var structurePermission = _db.PCBaseStructurePermissions.SingleOrDefault(x => x.PCBaseStructureID == pcBaseStructureID && x.PlayerID == player.GlobalID);
            if (structurePermission == null) return false;

            if (permission == StructurePermission.CanAccessStructureInventory && structurePermission.CanAccessStructureInventory) return true;
            if (permission == StructurePermission.CanPlaceEditStructures && structurePermission.CanPlaceEditStructures) return true;
            if (permission == StructurePermission.CanEnterBuilding && structurePermission.CanEnterBuilding) return true;
            if (permission == StructurePermission.CanRetrieveStructures && structurePermission.CanRetrieveStructures) return true;
            if (permission == StructurePermission.CanAdjustPermissions && structurePermission.CanAdjustPermissions) return true;
            if (permission == StructurePermission.CanRenameStructures && structurePermission.CanRenameStructures) return true;
            if (permission == StructurePermission.CanEditPrimaryResidence && structurePermission.CanEditPrimaryResidence) return true;
            if (permission == StructurePermission.CanRemovePrimaryResidence && structurePermission.CanRemovePrimaryResidence) return true;

            // Player doesn't have permission.
            return false;
        }

        public void GrantBasePermissions(NWPlayer player, int pcBaseID, params BasePermission[] permissions)
        {
            var dbPermission = _db.PCBasePermissions.SingleOrDefault(x => x.PCBaseID == pcBaseID && x.PlayerID == player.GlobalID);

            if (dbPermission == null)
            {
                dbPermission = new PCBasePermission
                {
                    PCBaseID = pcBaseID,
                    PlayerID = player.GlobalID
                };
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
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            _db.PCBasePermissions.AddOrUpdate(dbPermission);
            _db.SaveChanges();
        }

        public void GrantStructurePermissions(NWPlayer player, int pcBaseStructureID, params StructurePermission[] permissions)
        {
            var dbPermission = _db.PCBaseStructurePermissions.SingleOrDefault(x => x.PCBaseStructureID == pcBaseStructureID && x.PlayerID == player.GlobalID);

            if (dbPermission == null)
            {
                dbPermission = new PCBaseStructurePermission
                {
                    PCBaseStructureID = pcBaseStructureID,
                    PlayerID = player.GlobalID
                };
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
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            _db.PCBaseStructurePermissions.AddOrUpdate(dbPermission);
            _db.SaveChanges();
        }
    }
}
