﻿using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Placeable.CraftingDevice
{
    public class OnOpened: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable device = (Object.OBJECT_SELF);
            NWPlayer oPC = (_.GetLastOpenedBy());
            var model = CraftService.GetPlayerCraftingData(oPC);
            
            if (model.Access != CraftingAccessType.None)
            {
                NWItem menuItem = (_.CreateItemOnObject("cft_confirm", device.Object));
                NWPlaceable storage = (_.GetObjectByTag("craft_temp_store"));
                var storageItems = storage.InventoryItems.ToList();
                List<NWItem> list = null;

                if (model.Access == CraftingAccessType.MainComponent)
                {
                    menuItem.Name = "Confirm Main Components";
                    list = model.MainComponents;
                }
                else if (model.Access == CraftingAccessType.SecondaryComponent)
                {
                    menuItem.Name = "Confirm Secondary Components";
                    list = model.SecondaryComponents;
                }
                else if (model.Access == CraftingAccessType.TertiaryComponent)
                {
                    menuItem.Name = "Confirm Tertiary Components";
                    list = model.TertiaryComponents;
                }
                else if (model.Access == CraftingAccessType.Enhancement)
                {
                    menuItem.Name = "Confirm Enhancement Components";
                    list = model.EnhancementComponents;
                }

                if (list == null)
                {
                    oPC.FloatingText("Error locating component list. Notify an admin.");
                    return false;
                }

                foreach (var item in list)
                {
                    NWItem storageItem = storageItems.Single(x => x.GlobalID == item.GlobalID);
                    _.CopyItem(storageItem.Object, device.Object, _.TRUE);
                }

                oPC.FloatingText("Place the components inside the container and then click the item named '" + menuItem.Name + "' to continue.");
            }

            device.IsLocked = true;
            return true;
        }


    }
}
