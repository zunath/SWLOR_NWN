
using System;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    public class Area: IEntity
    {
        public Area()
        {
            ID = Guid.NewGuid();
        }

        [Key]
        public Guid ID { get; set; }
        public string Resref { get; set; }
        public string Name { get; set; }
        public string Tag { get; set; }
        public Spawn ResourceSpawnTableID { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsBuildable { get; set; }
        public Guid? NorthwestOwner { get; set; }
        public Guid? NortheastOwner { get; set; }
        public Guid? SouthwestOwner { get; set; }
        public Guid? SoutheastOwner { get; set; }
        public bool IsActive { get; set; }
        public int PurchasePrice { get; set; }
        public int DailyUpkeep { get; set; }
        public bool AutoSpawnResources { get; set; }
        public int ResourceQuality { get; set; }
        public LootTable? NorthwestLootTableID { get; set; }
        public LootTable? NortheastLootTableID { get; set; }
        public LootTable? SouthwestLootTableID { get; set; }
        public LootTable? SoutheastLootTableID { get; set; }
        public int MaxResourceQuality { get; set; }
    }
}
