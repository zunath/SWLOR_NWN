using System.Linq;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.Scripting.Contracts;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Scripting.Placeable.ScavengePoint
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
                point.Destroy();
            }
        }
    }
}
