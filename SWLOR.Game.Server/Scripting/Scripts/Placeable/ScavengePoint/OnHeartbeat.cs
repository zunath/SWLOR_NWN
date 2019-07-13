using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;

namespace SWLOR.Game.Server.Scripting.Scripts.Placeable.ScavengePoint
{
    public class OnHeartbeat: IScript
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
            bool isFullyHarvested = point.GetLocalInt("SCAVENGE_POINT_FULLY_HARVESTED") == 1;
            if (isFullyHarvested)
            {
                int despawnTicks = point.GetLocalInt("SCAVENGE_POINT_REFILL_TICKS") - 1;
                if (despawnTicks <= 0) despawnTicks = 0;

                point.SetLocalInt("SCAVENGE_POINT_REFILL_TICKS", despawnTicks);
            }

        }
    }
}
