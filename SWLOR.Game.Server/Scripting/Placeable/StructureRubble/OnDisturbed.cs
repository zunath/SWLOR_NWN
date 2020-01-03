using System.Linq;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripting.Placeable.StructureRubble
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
            var disturbType = _.GetInventoryDisturbType();
            NWItem item = (_.GetInventoryDisturbItem());
            NWCreature creature = (_.GetLastDisturbed());
            NWPlaceable container = (NWGameObject.OBJECT_SELF);

            if (disturbType == InventoryDisturbType.Added)
            {
                ItemService.ReturnItem(creature, item);
            }
            else if (disturbType == InventoryDisturbType.Removed)
            {
                if (!container.InventoryItems.Any())
                {
                    container.Destroy();
                }
            }
        }
    }
}
