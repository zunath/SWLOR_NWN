using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.AreaInstance.Contracts;
using SWLOR.Game.Server.GameObject;
using static NWN._;

namespace SWLOR.Game.Server.AreaInstance.CoxxionBase
{
    public class OnSpawn: IAreaInstance
    {
        public void Run(NWArea area)
        {
            var doors = new List<NWObject>();
            
            var obj = _.GetFirstObjectInArea(area);
            while (_.GetIsObjectValid(obj) == TRUE)
            {
                int colorID = _.GetLocalInt(obj, "DOOR_COLOR");

                if (colorID > 0)
                {
                    doors.Add(obj);
                }

                obj = _.GetNextObjectInArea(area);
            }

            area.Data["DOORS"] = doors;
        }
    }
}
