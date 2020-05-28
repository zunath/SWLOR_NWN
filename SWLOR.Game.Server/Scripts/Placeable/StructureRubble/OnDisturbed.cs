using System.Linq;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Placeable.StructureRubble
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
            NWPlaceable container = (_.OBJECT_SELF);

            if (disturbType == DisturbType.Added)
            {
                ItemService.ReturnItem(creature, item);
            }
            else if (disturbType == DisturbType.Removed)
            {
                if (!container.InventoryItems.Any())
                {
                    container.Destroy();
                }
            }
        }
    }
}
