using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Placeable.Scrapper
{
    public class OnDisturbed: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IItemService _item;

        public OnDisturbed(
            INWScript script,
            IItemService item)
        {
            _ = script;
            _item = item;
        }

        public bool Run(params object[] args)
        {
            int type = _.GetInventoryDisturbType();
            if (type != INVENTORY_DISTURB_TYPE_ADDED) return true;
            NWPlaceable device = Object.OBJECT_SELF;
            NWPlayer player = _.GetLastDisturbed();
            NWItem item = _.GetInventoryDisturbItem();
            var componentIP = item.ItemProperties.FirstOrDefault(x => _.GetItemPropertyType(x) == (int)CustomItemPropertyType.ComponentType);

            // Not a component. Return the item.
            if (componentIP == null)
            {
                _item.ReturnItem(player, item);
                player.FloatingText("You cannot scrap this item.");
                return false;
            }

            foreach (var ip in item.ItemProperties)
            {
                var ipType = _.GetItemPropertyType(ip);
                if (ipType != (int)CustomItemPropertyType.ComponentType)
                {
                    _.RemoveItemProperty(item, ip);
                }
            }

            if (!item.Name.Contains("(SCRAPPED)"))
            {
                item.Name = item.Name + " (SCRAPPED)";
            }

            device.AssignCommand(() =>
            {
                _.ActionGiveItem(item, player);
            });

            return true;
        }
    }
}
