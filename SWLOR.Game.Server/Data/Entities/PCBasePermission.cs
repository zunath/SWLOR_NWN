using System.ComponentModel.DataAnnotations;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class PCBasePermission
    {
        public int PCBasePermissionID { get; set; }

        public int PCBaseID { get; set; }

        [Required]
        [StringLength(60)]
        public string PlayerID { get; set; }

        public bool CanPlaceEditStructures { get; set; }

        public bool CanAccessStructureInventory { get; set; }

        public bool CanManageBaseFuel { get; set; }

        public bool CanExtendLease { get; set; }

        public bool CanAdjustPermissions { get; set; }

        public bool CanEnterBuildings { get; set; }

        public bool CanRetrieveStructures { get; set; }

        public bool CanCancelLease { get; set; }

        public bool CanRenameStructures { get; set; }

        public virtual PCBase PCBase { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
