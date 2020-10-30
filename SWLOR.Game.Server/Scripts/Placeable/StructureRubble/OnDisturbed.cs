using System.Linq;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;
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
            var disturbType = NWScript.GetInventoryDisturbType();
            NWItem item = (NWScript.GetInventoryDisturbItem());
            NWCreature creature = (NWScript.GetLastDisturbed());
            NWPlaceable container = (NWScript.OBJECT_SELF);

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
