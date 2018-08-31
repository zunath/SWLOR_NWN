using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.CraftingDevice
{
    public class OnOpened: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly ICraftService _craft;

        public OnOpened(INWScript script,
            ICraftService craft)
        {
            _ = script;
            _craft = craft;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable device = NWPlaceable.Wrap(Object.OBJECT_SELF);
            NWPlayer oPC = NWPlayer.Wrap(_.GetLastOpenedBy());
            var model = _craft.GetPlayerCraftingData(oPC);
            
            if (model.Access != CraftingAccessType.None)
            {
                NWItem menuItem = NWItem.Wrap(_.CreateItemOnObject("cft_confirm", device.Object));
                NWPlaceable storage = NWPlaceable.Wrap(_.GetObjectByTag("craft_temp_store"));
                var storageItems = storage.InventoryItems;
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
                    _.CopyItem(storageItem.Object, device.Object, NWScript.TRUE);
                }

                oPC.FloatingText("Place the components inside the container and then click the item named '" + menuItem.Name + "' to continue.");
            }

            device.IsLocked = true;
            return true;
        }


    }
}
