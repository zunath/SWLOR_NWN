﻿using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service;

using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.CraftingDevice
{
    public class OnClosed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            // Should only fire when a player walks away from the device.
            // Clean up temporary data and return all items placed inside.
            NWPlayer player = (_.GetLastClosedBy());
            NWPlaceable device = (Object.OBJECT_SELF);
            var model = CraftService.GetPlayerCraftingData(player);
            device.DestroyAllInventoryItems();
            device.IsLocked = false;
            model.IsAccessingStorage = false;

            foreach (var item in model.MainComponents)
            {
                _.CopyItem(item.Object, player.Object, _.TRUE);
                item.Destroy();
            }
            foreach (var item in model.SecondaryComponents)
            {
                _.CopyItem(item.Object, player.Object, _.TRUE);
                item.Destroy();
            }
            foreach (var item in model.TertiaryComponents)
            {
                _.CopyItem(item.Object, player.Object, _.TRUE);
                item.Destroy();
            }
            foreach (var item in model.EnhancementComponents)
            {
                _.CopyItem(item.Object, player.Object, _.TRUE);
                item.Destroy();
            }

            _.SetEventScript(device.Object, _.EVENT_SCRIPT_PLACEABLE_ON_USED, "jvm_script_1");
            _.SetEventScript(device.Object, _.EVENT_SCRIPT_PLACEABLE_ON_OPEN, string.Empty);
            _.SetEventScript(device.Object, _.EVENT_SCRIPT_PLACEABLE_ON_CLOSED, string.Empty);
            _.SetEventScript(device.Object, _.EVENT_SCRIPT_PLACEABLE_ON_INVENTORYDISTURBED, string.Empty);
            player.Data.Remove("CRAFTING_MODEL");
            return true;
        }
    }
}
