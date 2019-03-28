
using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[Area]")]
    public class Area: IEntity
    {
        public Area()
        {
            ID = Guid.NewGuid();
            DateLastBaked = DateTime.UtcNow;
        }

        [ExplicitKey]
        public Guid ID { get; set; }
        public string Resref { get; set; }
        public string Name { get; set; }
        public string Tag { get; set; }
        public int ResourceSpawnTableID { get; set; }
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
        public string Walkmesh { get; set; }
        public DateTime DateLastBaked { get; set; }
        public bool AutoSpawnResources { get; set; }
        public int ResourceQuality { get; set; }
        public int? NorthwestLootTableID { get; set; }
        public int? NortheastLootTableID { get; set; }
        public int? SouthwestLootTableID { get; set; }
        public int? SoutheastLootTableID { get; set; }
        public int MaxResourceQuality { get; set; }
    }
}
