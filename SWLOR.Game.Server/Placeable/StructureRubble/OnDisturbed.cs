using System.Linq;
using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN._;

namespace SWLOR.Game.Server.Placeable.StructureRubble
{
    public class OnDisturbed: IRegisteredEvent
    {
        
        private readonly IItemService _item;

        public OnDisturbed(
            
            IItemService item)
        {
            
            _item = item;
        }

        public bool Run(params object[] args)
        {
            int disturbType = _.GetInventoryDisturbType();
            NWItem item = (_.GetInventoryDisturbItem());
            NWCreature creature = (_.GetLastDisturbed());
            NWPlaceable container = (Object.OBJECT_SELF);

            if (disturbType == INVENTORY_DISTURB_TYPE_ADDED)
            {
                _item.ReturnItem(creature, item);
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
