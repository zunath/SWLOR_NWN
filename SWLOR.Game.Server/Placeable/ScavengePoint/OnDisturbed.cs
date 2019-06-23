using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Placeable.ScavengePoint
{
    public class OnDisturbed: IRegisteredEvent
    {

        public bool Run(params object[] args)
        {
            NWPlayer oPC = (_.GetLastDisturbed());
            if (!oPC.IsPlayer) return false;

            NWItem oItem = (_.GetInventoryDisturbItem());
            NWPlaceable point = (Object.OBJECT_SELF);
            int disturbType = _.GetInventoryDisturbType();

            if (disturbType == _.INVENTORY_DISTURB_TYPE_ADDED)
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
                        _.CreateObject(_.OBJECT_TYPE_ITEM, seed, point.Location);

                        int perkLevel = PerkService.GetCreaturePerkLevel(oPC, PerkType.SeedPicker);
                        if (RandomService.Random(100) + 1 <= perkLevel * 10)
                        {
                            _.CreateObject(_.OBJECT_TYPE_ITEM, seed, point.Location);
                        }
                    }

                    point.Destroy();
                    FarmingService.RemoveGrowingPlant(point);
                }
            }
            return true;
        }
    }
}
