using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class ModItemDefinition: IItemListDefinition
    {
        public Dictionary<string, ItemDetail> BuildItems()
        {
            var builder = new ItemBuilder();
            WeaponMod(builder);
            ArmorMod(builder);

            return builder.Build();
        }

        private void WeaponMod(ItemBuilder builder)
        {
            builder.Create("MOD_WEAPON")
                .Delay(6.0f)
                .MaxDistance(0.0f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .ReducesItemCharge()
                .ValidationAction((user, item, target, location) =>
                {
                    // Ensure the mod type is set on the item.
                    var modType = GetLocalString(item, "MOD_TYPE");
                    if (string.IsNullOrWhiteSpace(modType))
                    {
                        Log.Write(LogGroup.Error, $"Mod type is unspecified on item '{GetName(item)}'. Set the local variable 'MOD_TYPE' on the item in the toolset.");
                        return "Mod is not registered in the system. Notify an admin this item is bugged.";
                    }

                    // Ensure the mod type is registered in code.
                    if (!ItemMod.IsRegistered(modType))
                    {
                        Log.Write(LogGroup.Error, $"Mod type '{modType}' is not programmed. Add it to the ModItemDefinition.cs class.");
                        return "Mod is not registered in the system. Notify an admin this mod type is bugged.";
                    }

                    // Ensure target is an item.
                    if (GetObjectType(target) != ObjectType.Item)
                        return "Only weapons may be targeted.";

                    // Ensure target is a weapon.
                    var itemType = GetBaseItemType(target);
                    if (!Item.WeaponBaseItemTypes.Contains(itemType))
                        return "Only weapons may be targeted.";

                    // Look for a Weapon Socket property. If one is found, this is a valid target.
                    for (var ip = GetFirstItemProperty(target); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(target))
                    {
                        if (GetItemPropertyType(ip) == ItemPropertyType.WeaponSocket)
                            return string.Empty;
                    }

                    // Couldn't find any open sockets. Return an error message.
                    return "No open sockets could be found on the targeted weapon.";
                })
                .ApplyAction((user, item, target, location) =>
                {
                    var modType = GetLocalString(item, "MOD_TYPE");
                    var mod = ItemMod.GetByKey(modType);

                    // Remove the empty socket property.
                    for (var ip = GetFirstItemProperty(target); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(target))
                    {
                        if (GetItemPropertyType(ip) == ItemPropertyType.WeaponSocket)
                        {
                            RemoveItemProperty(target, ip);
                            break;
                        }
                    }

                    mod.ApplyItemModAction?.Invoke(user, item, target);
                });
        }

        private void ArmorMod(ItemBuilder builder)
        {
            builder.Create("MOD_ARMOR")
                .Delay(6.0f)
                .MaxDistance(0.0f)
                .ValidationAction((user, item, target, location) =>
                {
                    // Ensure the mod type is set on the item.
                    var modType = GetLocalString(item, "MOD_TYPE");
                    if (string.IsNullOrWhiteSpace(modType))
                    {
                        Log.Write(LogGroup.Error, $"Mod type is unspecified on item '{GetName(item)}'. Set the local variable 'MOD_TYPE' on the item in the toolset.");
                        return "Mod is not registered in the system. Notify an admin this item is bugged.";
                    }

                    // Ensure the mod type is registered in code.
                    if (!ItemMod.IsRegistered(modType))
                    {
                        Log.Write(LogGroup.Error, $"Mod type '{modType}' is not programmed. Add it to the ModItemDefinition.cs class.");
                        return "Mod is not registered in the system. Notify an admin this mod type is bugged.";
                    }

                    // Ensure target is an item.
                    if (GetObjectType(target) != ObjectType.Item)
                        return "Only armor may be targeted.";

                    // Ensure target is an armor.
                    var itemType = GetBaseItemType(target);
                    if (!Item.ArmorBaseItemTypes.Contains(itemType))
                        return "Only armor may be targeted.";

                    // Look for an Armor Socket property. If one is found, this is a valid target.
                    for (var ip = GetFirstItemProperty(target); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(target))
                    {
                        if (GetItemPropertyType(ip) == ItemPropertyType.ArmorSocket)
                            return string.Empty;
                    }

                    // Couldn't find any open sockets. Return an error message.
                    return "No open sockets could be found on the targeted armor.";
                })
                .ApplyAction((user, item, target, location) =>
                {
                    var modType = GetLocalString(item, "MOD_TYPE");
                    var mod = ItemMod.GetByKey(modType);

                    // Remove the empty socket property.
                    for (var ip = GetFirstItemProperty(target); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(target))
                    {
                        if (GetItemPropertyType(ip) == ItemPropertyType.ArmorSocket)
                        {
                            RemoveItemProperty(target, ip);
                            break;
                        }
                    }

                    mod.ApplyItemModAction?.Invoke(user, item, target);
                });
        }
    }
}
