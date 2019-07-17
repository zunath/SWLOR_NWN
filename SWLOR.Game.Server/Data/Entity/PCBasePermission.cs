

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCBasePermission]")]
    public class PCBasePermission: IEntity
    {
        public PCBasePermission()
        {
            ID = Guid.NewGuid();
        }
        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PCBaseID { get; set; }
        public Guid PlayerID { get; set; }
        public bool CanPlaceEditStructures { get; set; }
        public bool CanAccessStructureInventory { get; set; }
        public bool CanManageBaseFuel { get; set; }
        public bool CanExtendLease { get; set; }
        public bool CanAdjustPermissions { get; set; }
        public bool CanEnterBuildings { get; set; }
        public bool CanRetrieveStructures { get; set; }
        public bool CanCancelLease { get; set; }
        public bool CanRenameStructures { get; set; }
        public bool CanEditPrimaryResidence { get; set; }
        public bool CanRemovePrimaryResidence { get; set; }
        public bool CanChangeStructureMode { get; set; }
        public bool IsPublicPermission { get; set; }
        public bool CanAdjustPublicPermissions { get; set; }
        public bool CanDockStarship { get; set; }
        public bool CanFlyStarship { get; set; }

        public IEntity Clone()
        {
            return new PCBasePermission
            {
                ID = ID,
                PCBaseID = PCBaseID,
                PlayerID = PlayerID,
                CanPlaceEditStructures = CanPlaceEditStructures,
                CanAccessStructureInventory = CanAccessStructureInventory,
                CanManageBaseFuel = CanManageBaseFuel,
                CanExtendLease = CanExtendLease,
                CanAdjustPermissions = CanAdjustPermissions,
                CanEnterBuildings = CanEnterBuildings,
                CanRetrieveStructures = CanRetrieveStructures,
                CanCancelLease = CanCancelLease,
                CanRenameStructures = CanRenameStructures,
                CanEditPrimaryResidence = CanEditPrimaryResidence,
                CanRemovePrimaryResidence = CanRemovePrimaryResidence,
                CanChangeStructureMode = CanChangeStructureMode,
                IsPublicPermission = IsPublicPermission,
                CanAdjustPublicPermissions = CanAdjustPublicPermissions,
                CanDockStarship = CanDockStarship,
                CanFlyStarship = CanFlyStarship
            };
        }
    }
}
