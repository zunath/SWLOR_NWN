using System.Linq;
using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Placeable.StructureRubble
{
    public class OnDisturbed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            int disturbType = _.GetInventoryDisturbType();
            NWItem item = (_.GetInventoryDisturbItem());
            NWCreature creature = (_.GetLastDisturbed());
            NWPlaceable container = (Object.OBJECT_SELF);

            if (disturbType == INVENTORY_DISTURB_TYPE_ADDED)
            {
                ItemService.ReturnItem(creature, item);
            }
            else if (disturbType == INVENTORY_DISTURB_TYPE_REMOVED)
            {
                if (!container.InventoryItems.Any())
                {
                    container.Destroy();
                }
            }

            return true;
        }
    }
}
