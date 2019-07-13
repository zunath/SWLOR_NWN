using System.Linq;
using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripting.Scripts.Placeable.ScavengePoint
{
    public class OnClosed: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlaceable point = (NWGameObject.OBJECT_SELF);
            NWPlayer player = _.GetLastClosedBy();

            if (!player.IsPlayer) return;

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
        }
    }
}
