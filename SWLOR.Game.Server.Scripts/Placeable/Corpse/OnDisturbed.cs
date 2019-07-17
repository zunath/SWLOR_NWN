using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Placeable.Corpse
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
            NWCreature looter = _.GetLastDisturbed();
            NWItem item = _.GetInventoryDisturbItem();
            int type = _.GetInventoryDisturbType();

            looter.AssignCommand(() =>
            {
                _.ActionPlayAnimation(_.ANIMATION_LOOPING_GET_LOW, 1.0f, 1.0f);
            });

            if (type == _.INVENTORY_DISTURB_TYPE_ADDED)
            {
                ItemService.ReturnItem(looter, item);
                looter.SendMessage("You cannot place items inside of corpses.");
            }
            else if (type == _.INVENTORY_DISTURB_TYPE_REMOVED)
            {
                NWItem copy = item.GetLocalObject("CORPSE_ITEM_COPY");

                if (copy.IsValid)
                {
                    copy.Destroy();
                }

                item.DeleteLocalObject("CORPSE_ITEM_COPY");
            }
        }
    }
}
