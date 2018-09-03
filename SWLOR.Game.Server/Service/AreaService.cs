using System;
using System.Data.Entity.Migrations;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Service
{
    public class AreaService : IAreaService
    {
        private readonly INWScript _;
        private readonly IDataContext _db;

        public AreaService(
            INWScript script,
            IDataContext db)
        {
            _ = script;
            _db = db;
        }

        public void OnModuleLoad()
        {
            var areas = NWModule.Get().Areas;
            var dbAreas = _db.Areas.Where(x => x.IsActive).ToList();
            dbAreas.ForEach(x => x.IsActive = false);

            foreach (var area in areas)
            {
                var dbArea = dbAreas.SingleOrDefault(x => x.Resref == area.Resref);

                if (dbArea == null)
                {
                    dbArea = new Area
                    {
                        AreaID = Guid.NewGuid().ToString("N"),
                        Resref = area.Resref
                    };
                }

                dbArea.Name = area.Name;
                dbArea.Tag = area.Tag;
                dbArea.ResourceQuality = area.GetLocalInt("RESOURCE_QUALITY");
                dbArea.Width = _.GetAreaSize(AREA_WIDTH, area.Object);
                dbArea.Height = _.GetAreaSize(AREA_HEIGHT, area.Object);
                dbArea.PurchasePrice = area.GetLocalInt("PURCHASE_PRICE");
                dbArea.WeeklyUpkeep = area.GetLocalInt("WEEKLY_UPKEEP");
                dbArea.IsBuildable = 
                    area.GetLocalInt("IS_BUILDABLE") == 1 && 
                    dbArea.Width == 32 && 
                    dbArea.Height == 32 &&
                    dbArea.PurchasePrice > 0 &&
                    dbArea.WeeklyUpkeep > 0;
                dbArea.IsActive = true;
                
                _db.Areas.AddOrUpdate(dbArea);
            }

            _db.SaveChanges();
        }

        public void PurchaseArea(NWPlayer player, NWArea area, string sector)
        {
            if(sector != AreaSector.Northwest && sector != AreaSector.Northeast &&
                sector != AreaSector.Southwest && sector != AreaSector.Southeast)
                throw new ArgumentException(nameof(sector) + " must match one of the valid sector values: NE, NW, SE, SW");

            if (area.Width < 32) throw new Exception("Area must be at least 32 tiles wide.");
            if (area.Height < 32) throw new Exception("Area must be at least 32 tiles high.");


            var dbArea = _db.Areas.Single(x => x.Resref == area.Resref);
            string existingOwner = string.Empty;
            switch (sector)
            {
                case AreaSector.Northwest: existingOwner = dbArea.NorthwestOwner; break;
                case AreaSector.Northeast: existingOwner = dbArea.NortheastOwner; break;
                case AreaSector.Southwest: existingOwner = dbArea.SouthwestOwner; break;
                case AreaSector.Southeast: existingOwner = dbArea.SoutheastOwner; break;
            }

            if (!string.IsNullOrWhiteSpace(existingOwner))
            {
                player.SendMessage("Another player already owns that sector.");
                return;
            }

            if (player.Gold < dbArea.PurchasePrice)
            {
                player.SendMessage("You do not have enough credits to purchase that sector.");
                return;
            }

            player.AssignCommand(() => _.TakeGoldFromCreature(dbArea.PurchasePrice, player.Object, TRUE));
            
            switch (sector)
            {
                case AreaSector.Northwest: dbArea.NorthwestOwner = player.GlobalID; break;
                case AreaSector.Northeast: dbArea.NortheastOwner = player.GlobalID; break;
                case AreaSector.Southwest: dbArea.SouthwestOwner = player.GlobalID; break;
                case AreaSector.Southeast: dbArea.SoutheastOwner = player.GlobalID; break;
            }
            
            _db.SaveChanges();

            player.FloatingText("You purchase " + area.Name + " (" + sector + ") for " + dbArea.PurchasePrice + " credits.");
        }

    }
}
