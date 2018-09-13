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
        private readonly INWScript _;
        private readonly IFarmingService _farming;
        private readonly IRandomService _random;
        private readonly IItemService _item;
        private readonly IPerkService _perk;

        public OnDisturbed(
            INWScript script,
            IFarmingService farming,
            IRandomService random,
            IItemService item,
            IPerkService perk)
        {
            _ = script;
            _farming = farming;
            _random = random;
            _item = item;
            _perk = perk;
        }

        public bool Run(params object[] args)
        {
            NWPlayer oPC = NWPlayer.Wrap(_.GetLastDisturbed());
            NWItem oItem = NWItem.Wrap(_.GetInventoryDisturbItem());
            NWPlaceable point = NWPlaceable.Wrap(Object.OBJECT_SELF);
            int disturbType = _.GetInventoryDisturbType();

            if (disturbType == NWScript.INVENTORY_DISTURB_TYPE_ADDED)
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
                        _.CreateObject(NWScript.OBJECT_TYPE_ITEM, seed, point.Location);

                        int perkLevel = _perk.GetPCPerkLevel(oPC, PerkType.SeedPicker);
                        if (_random.Random(100) + 1 <= perkLevel * 10)
                        {
                            _.CreateObject(NWScript.OBJECT_TYPE_ITEM, seed, point.Location);
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
