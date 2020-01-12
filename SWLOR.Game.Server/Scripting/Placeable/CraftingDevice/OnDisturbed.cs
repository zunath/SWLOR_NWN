﻿using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Scripting.Placeable.CraftingDevice
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
            
            if (type == InventoryDisturbType.Removed)
            {
                HandleRemoveItem();
            }
            else if (type == InventoryDisturbType.Added)
            {
                HandleAddItem();
            }
        }


        private void HandleAddItem()
        {
            NWPlayer oPC = (_.GetLastDisturbed());
            NWItem oItem = (_.GetInventoryDisturbItem());
            if (oItem.Resref == "cft_confirm") return;
            if (oPC.IsBusy)
            {
                ItemService.ReturnItem(oPC, oItem);
                oPC.SendMessage("You are too busy right now.");
                return;
            }

            var model = CraftService.GetPlayerCraftingData(oPC);
            var bp = CraftService.GetBlueprintByID(model.Blueprint);
            var mainComponent = bp.MainComponentType.GetAttribute<ComponentType, ComponentTypeAttribute>();
            var secondaryComponent = bp.SecondaryComponentType.GetAttribute<ComponentType, ComponentTypeAttribute>(); 
            var tertiaryComponent = bp.TertiaryComponentType.GetAttribute<ComponentType, ComponentTypeAttribute>();

            NWPlaceable storage = _.GetObjectByTag("craft_temp_store");

            List<NWItem> list = null;
            ComponentType allowedType = ComponentType.None;
            bool reachedCap = false;
            bool reachedEnhancementLimit = false;

            string componentName = string.Empty;
            switch (model.Access)
            {
                case CraftingAccessType.MainComponent:
                    allowedType = bp.MainComponentType;
                    reachedCap = model.MainMaximum < model.MainComponents.Count + 1;
                    list = model.MainComponents;
                    componentName = mainComponent.Name;
                    break;
                case CraftingAccessType.SecondaryComponent:
                    allowedType = bp.SecondaryComponentType;
                    reachedCap = model.SecondaryMaximum < model.SecondaryComponents.Count + 1;
                    list = model.SecondaryComponents;
                    componentName = secondaryComponent.Name;
                    break;
                case CraftingAccessType.TertiaryComponent:
                    allowedType = bp.TertiaryComponentType;
                    reachedCap = model.TertiaryMaximum < model.TertiaryComponents.Count + 1;
                    list = model.TertiaryComponents;
                    componentName = tertiaryComponent.Name;
                    break;
                case CraftingAccessType.Enhancement:
                    allowedType = ComponentType.Enhancement;
                    reachedCap = bp.EnhancementSlots < model.EnhancementComponents.Count + 1;
                    reachedEnhancementLimit = model.PlayerPerkLevel / 2 < model.EnhancementComponents.Count + 1;
                    list = model.EnhancementComponents;
                    componentName = "Enhancement";
                    break;
            }

            if (list == null)
            {
                ItemService.ReturnItem(oPC, oItem);
                oPC.FloatingText("There was an issue getting the item data. Notify an admin.");
                return;
            }

            if (reachedCap)
            {
                ItemService.ReturnItem(oPC, oItem);
                oPC.FloatingText("You cannot add any more components of that type.");
                return;
            }

            if (reachedEnhancementLimit)
            {
                ItemService.ReturnItem(oPC, oItem);
                oPC.FloatingText("Your perk level does not allow you to attach any more enhancements to this item.");
                return;
            }

            var props = oItem.ItemProperties.ToList();
            var allowedItemTypes = new List<CustomItemType>();
            CustomItemType finishedItemType = ItemService.GetCustomItemTypeByResref(bp.Resref);

            foreach (var ip in props)
            {
                if (_.GetItemPropertyType(ip) == ItemPropertyType.ComponentItemTypeRestriction)
                {
                    int restrictionType = _.GetItemPropertyCostTableValue(ip);
                    allowedItemTypes.Add((CustomItemType)restrictionType);
                }
            }

            if (allowedItemTypes.Count > 0)
            {
                if (!allowedItemTypes.Contains(finishedItemType))
                {
                    oPC.FloatingText("This component cannot be used with this type of blueprint.");
                    ItemService.ReturnItem(oPC, oItem);
                    return;
                }
            }

            foreach (var ip in props)
            {
                if (_.GetItemPropertyType(ip) == ItemPropertyType.ComponentType)
                {
                    int compType = _.GetItemPropertyCostTableValue(ip);
                    if (compType == (int) allowedType)
                    {
                        oItem.GetOrAssignGlobalID();
                        NWItem copy = (_.CopyItem(oItem.Object, storage.Object, true));
                        list.Add(copy);
                        return;
                    }
                }
            }

            oPC.FloatingText("Only " + componentName + " components may be used with this component type.");
            ItemService.ReturnItem(oPC, oItem);
        }

        private void HandleRemoveItem()
        {
            NWPlayer oPC = (_.GetLastDisturbed());
            NWItem oItem = (_.GetInventoryDisturbItem());
            NWPlaceable device = (NWGameObject.OBJECT_SELF);
            NWPlaceable storage = (_.GetObjectByTag("craft_temp_store"));
            var model = CraftService.GetPlayerCraftingData(oPC);
            if (oPC.IsBusy)
            {
                ItemService.ReturnItem(device, oItem);
                oPC.SendMessage("You are too busy right now.");
                return;
            }

            if (oItem.Resref == "cft_confirm")
            {
                oItem.Destroy();
                device.DestroyAllInventoryItems();
                device.IsLocked = false;
                model.IsAccessingStorage = false;
                DialogService.StartConversation(oPC, device, "CraftItem");
                return;
            }

            List<NWItem> items = null;

            switch(model.Access)
            {
                case CraftingAccessType.MainComponent:
                    items = model.MainComponents;
                    break;
                case CraftingAccessType.SecondaryComponent:
                    items = model.SecondaryComponents;
                    break;
                case CraftingAccessType.TertiaryComponent:
                    items = model.TertiaryComponents;
                    break;
                case CraftingAccessType.Enhancement:
                    items = model.EnhancementComponents;
                    break;
            }

            NWItem copy = storage.InventoryItems.SingleOrDefault(x => x.GlobalID == oItem.GlobalID);
            NWItem listItem = items?.SingleOrDefault(x => x.GlobalID == oItem.GlobalID);
            if (listItem == null || copy == null || !copy.IsValid) return;

            copy.Destroy();
            items.Remove(listItem);

        }
        
    }
}
