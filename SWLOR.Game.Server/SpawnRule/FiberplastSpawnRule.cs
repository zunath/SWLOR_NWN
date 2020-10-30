using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;
using SWLOR.Game.Server.SpawnRule.Contracts;

namespace SWLOR.Game.Server.SpawnRule
{
    public class FiberplastSpawnRule: ISpawnRule
    {
        public void Run(NWObject target, params object[] args)
        {
            var dbArea = DataService.Area.GetByResref(target.Area.Resref);
            var tier = dbArea.ResourceQuality;
            
            if (tier <= 0)
            {
                Console.WriteLine("WARNING: Area '" + target.Area.Name + "' has resources but the RESOURCE_QUALITY variable is not set. Edit the area properties and add this value to set up resources.");
                return;
            }

            var difficulty = ((tier-1) * 5) + 1;
            var lootTable = tier;
            
            target.SetLocalInt("SCAVENGE_POINT_LEVEL", difficulty);
            target.SetLocalInt("SCAVENGE_POINT_LOOT_TABLE_ID", lootTable);
        }
    }
}
