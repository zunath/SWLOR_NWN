using System;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Service.Legacy;
using Object = SWLOR.Game.Server.Core.NWNX.Object;

namespace SWLOR.Game.Server.Scripts.Placeable.Scrapper
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
            var type = NWScript.GetInventoryDisturbType();
            if (type != DisturbType.Added) return;
            NWPlaceable device = NWScript.OBJECT_SELF;
            NWPlayer player = NWScript.GetLastDisturbed();
            NWItem item = NWScript.GetInventoryDisturbItem();
            var componentIP = item.ItemProperties.FirstOrDefault(x => NWScript.GetItemPropertyType(x) == ItemPropertyType.ComponentType);

            // Not a component. Return the item.
            if (componentIP == null)
            {
                ItemService.ReturnItem(player, item);
                player.FloatingText("You cannot scrap this item.");
                return;
            }

            // Item is a component but it was crafted. Cannot scrap crafted items.
            if (!string.IsNullOrWhiteSpace(item.GetLocalString("CRAFTER_PLAYER_ID")))
            {
                ItemService.ReturnItem(player, item);
                player.FloatingText("You cannot scrap crafted items.");
                return;
            }

            // Remove the item properties
            foreach (var ip in item.ItemProperties)
            {
                var ipType = NWScript.GetItemPropertyType(ip);
                if (ipType != ItemPropertyType.ComponentType)
                {
                    NWScript.RemoveItemProperty(item, ip);
                }
            }
            
            // Remove local variables (except the global ID)
            var varCount = Object.GetLocalVariableCount(item);
            for (var index = varCount-1; index >= 0; index--)
            {
                var localVar = Object.GetLocalVariable(item, index);

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
                NWScript.ActionGiveItem(item, player);
            });

            return;
        }
    }
}
