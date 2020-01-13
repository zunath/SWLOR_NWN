
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    [JsonObject(MemberSerialization.OptIn)]
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
        [JsonProperty]
        public Guid ID { get; set; }
        [JsonProperty]
        public Guid PlayerID { get; set; }
        [JsonProperty]
        public string AreaResref { get; set; }
        [JsonProperty]
        public string Sector { get; set; }
        [JsonProperty]
        public DateTime DateInitialPurchase { get; set; }
        [JsonProperty]
        public DateTime DateRentDue { get; set; }
        [JsonProperty]
        public int ShieldHP { get; set; }
        [JsonProperty]
        public bool IsInReinforcedMode { get; set; }
        [JsonProperty]
        public int Fuel { get; set; }
        [JsonProperty]
        public int ReinforcedFuel { get; set; }
        [JsonProperty]
        public DateTime DateFuelEnds { get; set; }
        [JsonProperty]
        public PCBaseType PCBaseTypeID { get; set; }
        [JsonProperty]
        public ApartmentType? ApartmentBuildingID { get; set; }
        [JsonProperty]
        public string CustomName { get; set; }
        [JsonProperty]
        public BuildingStyle BuildingStyleID { get; set; }
        [JsonProperty]
        public string ShipLocation { get; set; }
        [JsonProperty]
        public int? Starcharts { get; set; }
        [JsonProperty]
        public Guid? ControlTowerStructureID { get; set; }

        [JsonProperty]
        public PCBasePermission PublicBasePermission { get; set; }
        [JsonProperty]
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
