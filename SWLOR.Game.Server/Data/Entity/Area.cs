
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[Areas]")]
    public class Area: IEntity
    {
        public Area()
        {
            AreaID = Guid.NewGuid();
            DateLastBaked = DateTime.UtcNow;
        }

        [ExplicitKey]
        public Guid AreaID { get; set; }
        public string Resref { get; set; }
        public string Name { get; set; }
        public string Tag { get; set; }
        public Guid ResourceSpawnTableID { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsBuildable { get; set; }
        public Guid NorthwestOwner { get; set; }
        public Guid NortheastOwner { get; set; }
        public Guid SouthwestOwner { get; set; }
        public Guid SoutheastOwner { get; set; }
        public bool IsActive { get; set; }
        public int PurchasePrice { get; set; }
        public int DailyUpkeep { get; set; }
        public string Walkmesh { get; set; }
        public DateTime DateLastBaked { get; set; }
        public bool AutoSpawnResources { get; set; }
        public int ResourceQuality { get; set; }
        public Guid? NorthwestLootTableID { get; set; }
        public Guid? NortheastLootTableID { get; set; }
        public Guid? SouthwestLootTableID { get; set; }
        public Guid? SoutheastLootTableID { get; set; }
        public int MaxResourceQuality { get; set; }
    }
}
