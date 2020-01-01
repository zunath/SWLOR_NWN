

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PCBasePermission: IEntity
    {
        public PCBasePermission()
        {
            ID = Guid.NewGuid();
        }
        [Key]
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
    }
}
