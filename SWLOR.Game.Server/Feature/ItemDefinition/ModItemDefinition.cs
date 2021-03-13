using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class ModItemDefinition: IItemListDefinition
    {
        private delegate void ModActionDelegate(uint item);
        private static readonly Dictionary<string, ModActionDelegate> _modActions = new Dictionary<string, ModActionDelegate>();

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
                    if (!_modActions.ContainsKey(modType))
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
                    var modAction = _modActions[modType];

                    // Remove the empty socket property.
                    for (var ip = GetFirstItemProperty(target); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(target))
                    {
                        if (GetItemPropertyType(ip) == ItemPropertyType.WeaponSocket)
                        {
                            RemoveItemProperty(target, ip);
                            break;
                        }
                    }

                    modAction(target);
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
                    if (!_modActions.ContainsKey(modType))
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
                    var modAction = _modActions[modType];

                    // Remove the empty socket property.
                    for (var ip = GetFirstItemProperty(target); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(target))
                    {
                        if (GetItemPropertyType(ip) == ItemPropertyType.ArmorSocket)
                        {
                            RemoveItemProperty(target, ip);
                            break;
                        }
                    }

                    modAction(target);
                });
        }

        /// <summary>
        /// When the module loads, register all mod actions to cache for later use.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void LoadMods()
        {
            _modActions.Add("HUMAN_KILLER", item => KillerMod(item, RacialType.Human));
            _modActions.Add("ANIMAL_KILLER", item => KillerMod(item, RacialType.Animal));
            _modActions.Add("BEAST_KILLER", item => KillerMod(item, RacialType.Beast));
            _modActions.Add("VERMIN_KILLER", item => KillerMod(item, RacialType.Vermin));
            _modActions.Add("UNDEAD_KILLER", item => KillerMod(item, RacialType.Undead));
            _modActions.Add("ROBOT_KILLER", item => KillerMod(item, RacialType.Robot));
            _modActions.Add("BOTHAN_KILLER", item => KillerMod(item, RacialType.Bothan));
            _modActions.Add("CHISS_KILLER", item => KillerMod(item, RacialType.Chiss));
            _modActions.Add("ZABRAK_KILLER", item => KillerMod(item, RacialType.Zabrak));
            _modActions.Add("WOOKIEE_KILLER", item => KillerMod(item, RacialType.Wookiee));
            _modActions.Add("TWILEK_KILLER", item => KillerMod(item, RacialType.Twilek));
            _modActions.Add("CYBORG_KILLER", item => KillerMod(item, RacialType.Cyborg));
            _modActions.Add("CATHAR_KILLER", item => KillerMod(item, RacialType.Cathar));
            _modActions.Add("TRANDOSHAN_KILLER", item => KillerMod(item, RacialType.Trandoshan));
            _modActions.Add("MIRIALAN_KILLER", item => KillerMod(item, RacialType.Mirialan));
            _modActions.Add("ECHANI_KILLER", item => KillerMod(item, RacialType.Echani));
            _modActions.Add("MONCALAMARI_KILLER", item => KillerMod(item, RacialType.MonCalamari));
            _modActions.Add("UGNAUGHT_KILLER", item => KillerMod(item, RacialType.Ugnaught));
        }

        /// <summary>
        /// Killer mods grant +1 AB towards a specific racial type.
        /// Can be stacked.
        /// </summary>
        /// <param name="item">The item to receive the property.</param>
        /// <param name="racialType">The type of race to increase AB towards.</param>
        private static void KillerMod(uint item, RacialType racialType)
        {
            var amount = 1;

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.AttackBonusVsRacialGroup)
                {
                    var existingRacialType = (RacialType) GetItemPropertySubType(ip);
                    if (existingRacialType == racialType)
                    {
                        var existingBonus = GetItemPropertyCostTableValue(ip);
                        amount += existingBonus;
                    }
                }
            }

            var newIP = ItemPropertyAttackBonusVsRace(racialType, amount);
            BiowareXP2.IPSafeAddItemProperty(item, newIP, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
        }

    }
}
