
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Dapper.Contrib.Extensions.Table("[Areas]")]
    public class Area: IEntity
    {
        public Area()
        {
            AreaID = Guid.NewGuid().ToString();
            DateLastBaked = DateTime.UtcNow;
        }

        [ExplicitKey]
        public string AreaID { get; set; }
        public string Resref { get; set; }
        public string Name { get; set; }
        public string Tag { get; set; }
        public int ResourceSpawnTableID { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsBuildable { get; set; }
        public string NorthwestOwner { get; set; }
        public string NortheastOwner { get; set; }
        public string SouthwestOwner { get; set; }
        public string SoutheastOwner { get; set; }
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
