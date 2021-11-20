﻿using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;
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
            var type = _.GetInventoryDisturbType();

            looter.AssignCommand(() =>
            {
                _.ActionPlayAnimation(Animation.LoopingGetLow, 1.0f, 1.0f);
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
