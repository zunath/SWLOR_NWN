using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.Corpse
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
            NWCreature looter = NWScript.GetLastDisturbed();
            NWItem item = NWScript.GetInventoryDisturbItem();
            var type = NWScript.GetInventoryDisturbType();

            looter.AssignCommand(() =>
            {
                NWScript.ActionPlayAnimation(Animation.LoopingGetLow, 1.0f, 1.0f);
            });

            if (type == DisturbType.Added)
            {
                ItemService.ReturnItem(looter, item);
                looter.SendMessage("You cannot place items inside of corpses.");
            }
            else if (type == DisturbType.Removed)
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
