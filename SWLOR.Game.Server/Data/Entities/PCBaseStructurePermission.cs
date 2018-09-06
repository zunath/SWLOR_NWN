using System.ComponentModel.DataAnnotations;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class PCBaseStructurePermission
    {
        public int PCBaseStructurePermissionID { get; set; }

        public int PCBaseStructureID { get; set; }

        [Required]
        [StringLength(60)]
        public string PlayerID { get; set; }

        public bool CanPlaceEditStructures { get; set; }

        public bool CanAccessStructureInventory { get; set; }

        public bool CanEnterBuilding { get; set; }

        public bool CanRetrieveStructures { get; set; }

        public bool CanAdjustPermissions { get; set; }

        public virtual PCBaseStructure PCBaseStructure { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
