using System.Linq;
using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Placeable.StructureRubble
{
    public class OnDisturbed: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            int disturbType = _.GetInventoryDisturbType();
            NWItem item = (_.GetInventoryDisturbItem());
            NWCreature creature = (_.GetLastDisturbed());
            NWPlaceable container = (NWGameObject.OBJECT_SELF);

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
        }
    }
}
