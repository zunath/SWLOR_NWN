﻿using System.Linq;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Placeable.ScavengePoint
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
            NWPlayer oPC = (_.GetLastDisturbed());
            if (!oPC.IsPlayer) return;

            NWItem oItem = (_.GetInventoryDisturbItem());
            NWPlaceable point = (_.OBJECT_SELF);
            var disturbType = _.GetInventoryDisturbType();

            if (disturbType == DisturbType.Added)
            {
                ItemService.ReturnItem(oPC, oItem);
            }
            else
            {
                if (!point.InventoryItems.Any() && point.GetLocalInt("SCAVENGE_POINT_FULLY_HARVESTED") == 1)
                {
                    string seed = point.GetLocalString("SCAVENGE_POINT_SEED");
                    if (!string.IsNullOrWhiteSpace(seed))
                    {
                        _.CreateObject(ObjectType.Item, seed, point.Location);

                        int perkLevel = PerkService.GetCreaturePerkLevel(oPC, PerkType.SeedPicker);
                        if (RandomService.Random(100) + 1 <= perkLevel * 10)
                        {
                            _.CreateObject(ObjectType.Item, seed, point.Location);
                        }
                    }

                    point.Destroy();
                }
            }
        }
    }
}
