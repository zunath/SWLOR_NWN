
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PCBaseStructure: IEntity
    {
        public PCBaseStructure()
        {
            ID = Guid.NewGuid();
            CustomName = "";
            PublicStructurePermission = new PCBaseStructurePermission();
            PlayerPermissions = new Dictionary<Guid, PCBaseStructurePermission>();
            Items = new Dictionary<Guid, PCBaseStructureItem>();
        }

        [Key]
        [JsonProperty]
        public Guid ID { get; set; }
        [JsonProperty]
        public Guid PCBaseID { get; set; }
        [JsonProperty]
        public BaseStructure BaseStructureID { get; set; }
        [JsonProperty]
        public double LocationX { get; set; }
        [JsonProperty]
        public double LocationY { get; set; }
        [JsonProperty]
        public double LocationZ { get; set; }
        [JsonProperty]
        public double LocationOrientation { get; set; }
        [JsonProperty]
        public double Durability { get; set; }
        [JsonProperty]
        public BuildingStyle InteriorStyleID { get; set; }
        [JsonProperty]
        public BuildingStyle ExteriorStyleID { get; set; }
        [JsonProperty]
        public Guid? ParentPCBaseStructureID { get; set; }
        [JsonProperty]
        public string CustomName { get; set; }
        [JsonProperty]
        public int StructureBonus { get; set; }
        [JsonProperty]
        public DateTime? DateNextActivity { get; set; }
        [JsonProperty]
        public StructureModeType StructureModeID { get; set; }

        [JsonProperty]
        public PCBaseStructurePermission PublicStructurePermission { get; set; }
        [JsonProperty]
        public Dictionary<Guid, PCBaseStructurePermission> PlayerPermissions { get; set; }
        [JsonProperty]
        public Dictionary<Guid, PCBaseStructureItem> Items { get; set; }
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
    public class PCBaseStructureItem
    {
        public string ItemName { get; set; }
        public string ItemTag { get; set; }
        public string ItemResref { get; set; }
        public string ItemObject { get; set; }
    }
}
