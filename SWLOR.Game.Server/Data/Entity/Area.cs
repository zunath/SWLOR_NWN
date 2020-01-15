
using System;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Area: IEntity
    {
        [Key]
        [JsonProperty]
        public string Resref { get; set; }
        [JsonProperty]
        public string Name { get; set; }
        [JsonProperty]
        public string Tag { get; set; }
        [JsonProperty]
        public Spawn ResourceSpawnTableID { get; set; }
        [JsonProperty]
        public int Width { get; set; }
        [JsonProperty]
        public int Height { get; set; }
        [JsonProperty]
        public bool IsBuildable { get; set; }
        [JsonProperty]
        public Guid? NorthwestOwner { get; set; }
        [JsonProperty]
        public Guid? NortheastOwner { get; set; }
        [JsonProperty]
        public Guid? SouthwestOwner { get; set; }
        [JsonProperty]
        public Guid? SoutheastOwner { get; set; }
        [JsonProperty]
        public bool IsActive { get; set; }
        [JsonProperty]
        public int PurchasePrice { get; set; }
        [JsonProperty]
        public int DailyUpkeep { get; set; }
        [JsonProperty]
        public bool AutoSpawnResources { get; set; }
        [JsonProperty]
        public int ResourceQuality { get; set; }
        [JsonProperty]
        public LootTable? NorthwestLootTableID { get; set; }
        [JsonProperty]
        public LootTable? NortheastLootTableID { get; set; }
        [JsonProperty]
        public LootTable? SouthwestLootTableID { get; set; }
        [JsonProperty]
        public LootTable? SoutheastLootTableID { get; set; }
        [JsonProperty]
        public int MaxResourceQuality { get; set; }
    }
}
