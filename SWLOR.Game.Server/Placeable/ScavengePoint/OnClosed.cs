using System.Linq;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Placeable.ScavengePoint
{
    public class OnClosed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable point = (NWGameObject.OBJECT_SELF);
            NWPlayer player = _.GetLastClosedBy();

            if (!player.IsPlayer) return false;

            bool isFullyHarvested = point.GetLocalInt("SCAVENGE_POINT_FULLY_HARVESTED") == 1;

            if (!point.InventoryItems.Any() && isFullyHarvested)
            {
                string seed = point.GetLocalString("SCAVENGE_POINT_SEED");
                if (!string.IsNullOrWhiteSpace(seed))
                {
                    _.CreateObject(_.OBJECT_TYPE_ITEM, seed, point.Location);
                }

                point.Destroy();
                FarmingService.RemoveGrowingPlant(point);
            }
            return false;
        }
    }
}
