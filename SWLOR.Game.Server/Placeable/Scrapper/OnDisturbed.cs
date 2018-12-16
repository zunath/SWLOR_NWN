using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.Scrapper
{
    public class OnDisturbed: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IItemService _item;
        private readonly INWNXObject _nwnxObject;

        public OnDisturbed(
            INWScript script,
            IItemService item,
            INWNXObject nwnxObject)
        {
            _ = script;
            _item = item;
            _nwnxObject = nwnxObject;
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

            // Item is a component but it was crafted. Cannot scrap crafted items.
            if (!string.IsNullOrWhiteSpace(item.GetLocalString("CRAFTER_PLAYER_ID")))
            {
                _item.ReturnItem(player, item);
                player.FloatingText("You cannot scrap crafted items.");
                return false;
            }

            // Remove the item properties
            foreach (var ip in item.ItemProperties)
            {
                var ipType = _.GetItemPropertyType(ip);
                if (ipType != (int)CustomItemPropertyType.ComponentType)
                {
                    _.RemoveItemProperty(item, ip);
                }
            }
            
            // Remove local variables (except the global ID)
            int varCount = _nwnxObject.GetLocalVariableCount(item);
            for (int index = varCount-1; index >= 0; index--)
            {
                var localVar = _nwnxObject.GetLocalVariable(item, index);

                if (localVar.Key != "GLOBAL_ID")
                {
                    switch (localVar.Type)
                    {
                        case LocalVariableType.Int:
                            item.DeleteLocalInt(localVar.Key);
                            break;
                        case LocalVariableType.Float:
                            item.DeleteLocalFloat(localVar.Key);
                            break;
                        case LocalVariableType.String:
                            item.DeleteLocalString(localVar.Key);
                            break;
                        case LocalVariableType.Object:
                            item.DeleteLocalObject(localVar.Key);
                            break;
                        case LocalVariableType.Location:
                            item.DeleteLocalLocation(localVar.Key);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
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
