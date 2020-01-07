
using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PCBaseStructure: IEntity
    {
        public PCBaseStructure()
        {
            ID = Guid.NewGuid();
            CustomName = "";
            PlayerPermissions = new Dictionary<Guid, PCBaseStructurePermission>();
            PublicStructurePermission = new PCBaseStructurePermission();
        }

        [Key]
        public Guid ID { get; set; }
        public Guid PCBaseID { get; set; }
        public BaseStructure BaseStructureID { get; set; }
        public double LocationX { get; set; }
        public double LocationY { get; set; }
        public double LocationZ { get; set; }
        public double LocationOrientation { get; set; }
        public double Durability { get; set; }
        public BuildingStyle InteriorStyleID { get; set; }
        public BuildingStyle ExteriorStyleID { get; set; }
        public Guid? ParentPCBaseStructureID { get; set; }
        public string CustomName { get; set; }
        public int StructureBonus { get; set; }
        public DateTime? DateNextActivity { get; set; }
        public StructureModeType StructureModeID { get; set; }

        public PCBaseStructurePermission PublicStructurePermission { get; set; }
        public Dictionary<Guid, PCBaseStructurePermission> PlayerPermissions { get; set; }
    }

    public class PCBaseStructurePermission
    {
        public bool CanPlaceEditStructures { get; set; }
        public bool CanAccessStructureInventory { get; set; }
        public bool CanEnterBuilding { get; set; }
        public bool CanRetrieveStructures { get; set; }
        public bool CanAdjustPermissions { get; set; }
        public bool CanRenameStructures { get; set; }
        public bool CanEditPrimaryResidence { get; set; }
        public bool CanRemovePrimaryResidence { get; set; }
        public bool CanChangeStructureMode { get; set; }
        public bool CanAdjustPublicPermissions { get; set; }
        public bool CanFlyStarship { get; set; }
    }
}
