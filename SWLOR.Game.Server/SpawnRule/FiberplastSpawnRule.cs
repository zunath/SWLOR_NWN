using System;
using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.SpawnRule.Contracts;

namespace SWLOR.Game.Server.SpawnRule
{
    public class FiberplastSpawnRule: ISpawnRule
    {
        private readonly IDataContext _db;
        
        public FiberplastSpawnRule(IDataContext db)
        {
            _db = db;
        }

        public void Run(NWObject target)
        {
            var dbArea = _db.Areas.Single(x => x.Resref == target.Area.Resref);
            int tier = dbArea.ResourceQuality;
            
            if (tier <= 0)
            {
                Console.WriteLine("WARNING: Area '" + target.Area.Name + "' has resources but the RESOURCE_QUALITY variable is not set. Edit the area properties and add this value to set up resources.");
                return;
            }

            int difficulty = ((tier-1) * 5) + 1;
            int lootTable = tier;
            
            target.SetLocalInt("SCAVENGE_POINT_LEVEL", difficulty);
            target.SetLocalInt("SCAVENGE_POINT_LOOT_TABLE_ID", lootTable);
        }
    }
}
