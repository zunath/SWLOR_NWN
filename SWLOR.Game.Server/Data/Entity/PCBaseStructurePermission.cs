
using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCBaseStructurePermission]")]
    public class PCBaseStructurePermission: IEntity
    {
        public PCBaseStructurePermission()
        {
            ID = Guid.NewGuid();
        }
        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PCBaseStructureID { get; set; }
        public Guid PlayerID { get; set; }
        public bool CanPlaceEditStructures { get; set; }
        public bool CanAccessStructureInventory { get; set; }
        public bool CanEnterBuilding { get; set; }
        public bool CanRetrieveStructures { get; set; }
        public bool CanAdjustPermissions { get; set; }
        public bool CanRenameStructures { get; set; }
        public bool CanEditPrimaryResidence { get; set; }
        public bool CanRemovePrimaryResidence { get; set; }
        public bool CanChangeStructureMode { get; set; }
        public bool IsPublicPermission { get; set; }
        public bool CanAdjustPublicPermissions { get; set; }
        public bool CanFlyStarship { get; set; }

        public IEntity Clone()
        {
            return new PCBaseStructurePermission
            {
                ID = ID,
                PCBaseStructureID = PCBaseStructureID,
                PlayerID = PlayerID,
                CanPlaceEditStructures = CanPlaceEditStructures,
                CanAccessStructureInventory = CanAccessStructureInventory,
                CanEnterBuilding = CanEnterBuilding,
                CanRetrieveStructures = CanRetrieveStructures,
                CanAdjustPermissions = CanAdjustPermissions,
                CanRenameStructures = CanRenameStructures,
                CanEditPrimaryResidence = CanEditPrimaryResidence,
                CanRemovePrimaryResidence = CanRemovePrimaryResidence,
                CanChangeStructureMode = CanChangeStructureMode,
                IsPublicPermission = IsPublicPermission,
                CanAdjustPublicPermissions = CanAdjustPublicPermissions,
                CanFlyStarship = CanFlyStarship
            };
        }
    }
}
