
using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PCBase: IEntity
    {
        public PCBase()
        {
            ID = Guid.NewGuid();
            CustomName = "";

            PlayerBasePermissions = new Dictionary<Guid, PCBasePermission>();
            PublicBasePermission = new PCBasePermission();
        }

        [Key]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public string AreaResref { get; set; }
        public string Sector { get; set; }
        public DateTime DateInitialPurchase { get; set; }
        public DateTime DateRentDue { get; set; }
        public int ShieldHP { get; set; }
        public bool IsInReinforcedMode { get; set; }
        public int Fuel { get; set; }
        public int ReinforcedFuel { get; set; }
        public DateTime DateFuelEnds { get; set; }
        public int PCBaseTypeID { get; set; }
        public ApartmentType? ApartmentBuildingID { get; set; }
        public string CustomName { get; set; }
        public BuildingStyle BuildingStyleID { get; set; }
        public string ShipLocation { get; set; }
        public int? Starcharts { get; set; }

        public PCBasePermission PublicBasePermission { get; set; }
        public Dictionary<Guid, PCBasePermission> PlayerBasePermissions { get; set; }
    }

    public class PCBasePermission
    {
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
        public bool CanAdjustPublicPermissions { get; set; }
        public bool CanDockStarship { get; set; }
        public bool CanFlyStarship { get; set; }
    }
}
