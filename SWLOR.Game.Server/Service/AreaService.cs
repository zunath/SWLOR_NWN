using System;
using System.Data.Entity.Migrations;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
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
                dbArea.DailyUpkeep = area.GetLocalInt("WEEKLY_UPKEEP");
                dbArea.IsBuildable = 
                    area.GetLocalInt("IS_BUILDABLE") == 1 && 
                    dbArea.Width == 32 && 
                    dbArea.Height == 32 &&
                    dbArea.PurchasePrice > 0 &&
                    dbArea.DailyUpkeep > 0;
                dbArea.IsActive = true;
                
                _db.Areas.AddOrUpdate(dbArea);
            }

            _db.SaveChanges();
        }

        public NWArea CreateAreaInstance(string areaResref, string areaName)
        {
            string tag = Guid.NewGuid().ToString("N");
            var area = NWArea.Wrap(_.CreateArea(areaResref, tag, areaName));

            area.SetLocalInt("IS_AREA_INSTANCE", TRUE);

            return area;
        }
    }
}
