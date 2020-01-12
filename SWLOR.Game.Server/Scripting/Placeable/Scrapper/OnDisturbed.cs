﻿using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;
using _ = SWLOR.Game.Server.NWScript._;
using LocalVariableType = SWLOR.Game.Server.Enumeration.LocalVariableType;

namespace SWLOR.Game.Server.Scripting.Placeable.Scrapper
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
            var type = _.GetInventoryDisturbType();
            if (type != InventoryDisturbType.Added) return;
            NWPlaceable device = NWGameObject.OBJECT_SELF;
            NWPlayer player = _.GetLastDisturbed();
            NWItem item = _.GetInventoryDisturbItem();
            var componentIP = item.ItemProperties.FirstOrDefault(x => _.GetItemPropertyType(x) == ItemPropertyType.ComponentType);

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
                var ipType = _.GetItemPropertyType(ip);
                if (ipType != ItemPropertyType.ComponentType)
                {
                    _.RemoveItemProperty(item, ip);
                }
            }
            
            // Remove local variables (except the global ID)
            int varCount = NWNXObject.GetLocalVariableCount(item);
            for (int index = varCount-1; index >= 0; index--)
            {
                var localVar = NWNXObject.GetLocalVariable(item, index);

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

            return;
        }
    }
}
