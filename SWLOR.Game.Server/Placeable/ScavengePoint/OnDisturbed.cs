using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.ScavengePoint
{
    public class OnDisturbed: IRegisteredEvent
    {
        
        private readonly IFarmingService _farming;
        private readonly IRandomService _random;
        private readonly IItemService _item;
        private readonly IPerkService _perk;

        public OnDisturbed(
            
            IFarmingService farming,
            IRandomService random,
            IItemService item,
            IPerkService perk)
        {
            
            _farming = farming;
            _random = random;
            _item = item;
            _perk = perk;
        }

        public bool Run(params object[] args)
        {
            NWPlayer oPC = (_.GetLastDisturbed());
            if (!oPC.IsPlayer) return false;

            NWItem oItem = (_.GetInventoryDisturbItem());
            NWPlaceable point = (Object.OBJECT_SELF);
            int disturbType = _.GetInventoryDisturbType();

            if (disturbType == _.INVENTORY_DISTURB_TYPE_ADDED)
            {
                _item.ReturnItem(oPC, oItem);
            }
            else
            {
                if (!point.InventoryItems.Any() && point.GetLocalInt("SCAVENGE_POINT_FULLY_HARVESTED") == 1)
                {
                    string seed = point.GetLocalString("SCAVENGE_POINT_SEED");
                    if (!string.IsNullOrWhiteSpace(seed))
                    {
                        _.CreateObject(_.OBJECT_TYPE_ITEM, seed, point.Location);

                        int perkLevel = _perk.GetPCPerkLevel(oPC, PerkType.SeedPicker);
                        if (_random.Random(100) + 1 <= perkLevel * 10)
                        {
                            _.CreateObject(_.OBJECT_TYPE_ITEM, seed, point.Location);
                        }
                    }

                    point.Destroy();
                    _farming.RemoveGrowingPlant(point);
                }
            }
            return true;
        }
    }
}
