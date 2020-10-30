using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;
using SWLOR.Game.Server.SpawnRule.Contracts;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.SpawnRule
{
    public class FiberplastSpawnRule: ISpawnRule
    {
        public void Run(NWObject target, params object[] args)
        {
            var area = GetArea(target);
            var areaName = GetName(area);
            var areaResref = GetResRef(area);
            var dbArea = DataService.Area.GetByResref(areaResref);
            var tier = dbArea.ResourceQuality;
            
            if (tier <= 0)
            {
                Console.WriteLine("WARNING: Area '" + areaName + "' has resources but the RESOURCE_QUALITY variable is not set. Edit the area properties and add this value to set up resources.");
                return;
            }

            var difficulty = ((tier-1) * 5) + 1;
            var lootTable = tier;
            
            target.SetLocalInt("SCAVENGE_POINT_LEVEL", difficulty);
            target.SetLocalInt("SCAVENGE_POINT_LOOT_TABLE_ID", lootTable);
        }
    }
}
