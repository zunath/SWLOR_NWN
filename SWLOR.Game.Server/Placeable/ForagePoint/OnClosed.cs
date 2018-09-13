using System.Linq;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.ForagePoint
{
    public class OnClosed: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IFarmingService _farming;

        public OnClosed(INWScript script,
            IFarmingService farming)
        {
            _ = script;
            _farming = farming;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable point = NWPlaceable.Wrap(Object.OBJECT_SELF);

            bool isFullyHarvested = point.GetLocalInt("FORAGE_POINT_FULLY_HARVESTED") == 1;

            if (!point.InventoryItems.Any() && isFullyHarvested)
            {
                string seed = point.GetLocalString("FORAGE_POINT_SEED");
                if (!string.IsNullOrWhiteSpace(seed))
                {
                    _.CreateObject(NWScript.OBJECT_TYPE_ITEM, seed, point.Location);
                }

                point.Destroy();
                _farming.RemoveGrowingPlant(point);
            }
            return false;
        }
    }
}
