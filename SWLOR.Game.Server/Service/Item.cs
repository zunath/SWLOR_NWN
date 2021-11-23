﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.PerkService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Item
    {
        private static readonly Dictionary<string, ItemDetail> _items = new Dictionary<string, ItemDetail>();

        /// <summary>
        /// When the module loads, all item details are loaded into the cache.
        /// </summary>
        [NWNEventHandler("mod_cache")]
        public static void CacheData()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IItemListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IItemListDefinition)Activator.CreateInstance(type);
                var items = instance.BuildItems();

                foreach (var (itemTag, itemDetail) in items)
                {
                    _items[itemTag] = itemDetail;
                }
            }

            Console.WriteLine($"Loaded {_items.Count} items.");
        }
        
        /// <summary>
        /// When an item is used, if its tag is in the item cache, run it through the action item process.
        /// </summary>
        [NWNEventHandler("item_use_bef")]
        public static void UseItem()
        {
            var user = OBJECT_SELF;
            void CheckPosition(uint actionUser, string actionId, Vector3 originalPosition)
            {
                // Action ended, no need to continue checking.
                if (!GetLocalBool(actionUser, actionId)) return;

                var position = GetPosition(actionUser);

                if (position.X != originalPosition.X ||
                    position.Y != originalPosition.Y ||
                    position.Z != originalPosition.Z)
                {
                    Activity.ClearBusy(actionUser);
                    SendMessageToPC(actionUser, "You move and interrupt your action.");
                    PlayerPlugin.StopGuiTimingBar(actionUser, string.Empty);
                    return;
                }

                DelayCommand(0.1f, () => CheckPosition(actionUser, actionId, originalPosition));
            }

            var item = StringToObject(EventsPlugin.GetEventData("ITEM_OBJECT_ID"));
            var itemTag = GetTag(item);

            // Not in the cache. Skip.
            if (!_items.ContainsKey(itemTag))
                return;

            var target = StringToObject(EventsPlugin.GetEventData("TARGET_OBJECT_ID"));
            var area = GetArea(user);
            var targetPositionX = (float)Convert.ToDouble(EventsPlugin.GetEventData("TARGET_POSITION_X"));
            var targetPositionY = (float)Convert.ToDouble(EventsPlugin.GetEventData("TARGET_POSITION_Y"));
            var targetPositionZ = (float)Convert.ToDouble(EventsPlugin.GetEventData("TARGET_POSITION_Z"));
            var targetPosition = GetIsObjectValid(target) ? GetPosition(target) : Vector3(targetPositionX, targetPositionY, targetPositionZ);
            var targetLocation = GetIsObjectValid(target) ? GetLocation(target) : Location(area, targetPosition, 0.0f);
            var userPosition = GetPosition(user);

            // Bypass the NWN "item use" animation.
            EventsPlugin.SkipEvent();

            // Check item property requirements.
            if (!CanCreatureUseItem(user, item))
            {
                SendMessageToPC(user, "You do not meet the requirements to use this item.");
                return;
            }

            // User is busy
            if (Activity.IsBusy(user))
            {
                SendMessageToPC(user, "You are busy.");
                return;
            }

            var itemDetail = _items[itemTag];
            var validationMessage = itemDetail.ValidateAction == null ? string.Empty : itemDetail.ValidateAction(user, item, target, targetLocation);

            // Failed validation.
            if(!string.IsNullOrWhiteSpace(validationMessage))
            {
                SendMessageToPC(user, validationMessage);
                return;
            }

            // Send the initialization message, if there is one.
            var initializationMessage = itemDetail.InitializationMessageAction == null
                ? string.Empty
                : itemDetail.InitializationMessageAction(user, item, target, targetLocation);
            if (!string.IsNullOrWhiteSpace(initializationMessage))
            {
                SendMessageToPC(user, initializationMessage);
            }

            var maxDistance = itemDetail.CalculateDistanceAction?.Invoke(user, item, target, targetLocation) ?? 3.5f;
            // Distance checks, if necessary for this item.
            if (maxDistance > 0.0f)
            {
                // Target is valid - check distance between objects.
                if (GetIsObjectValid(target) &&
                    (GetDistanceBetween(user, target) > maxDistance ||
                     area != GetArea(target)))
                {
                    SendMessageToPC(user, "Your target is too far away.");
                    return;
                }
                // Target is invalid - check distance between locations.
                else if (!GetIsObjectValid(target) &&
                         (GetDistanceBetweenLocations(GetLocation(user), targetLocation) > maxDistance ||
                          area != GetAreaFromLocation(targetLocation)))
                {
                    SendMessageToPC(user, "That location is too far away.");
                    return;
                }
            }

            // Make the user turn to face the target if configured.
            if (itemDetail.UserFacesTarget)
            {
                AssignCommand(user, () => SetFacingPoint(targetPosition));
            }

            var delay = itemDetail.DelayAction?.Invoke(user, item, target, targetLocation) ?? 0.0f;
            // Play an animation if configured.
            if (itemDetail.ActivationAnimation != Animation.Invalid)
            {
                AssignCommand(user, () => ActionPlayAnimation(itemDetail.ActivationAnimation, 1.0f, delay));
            }

            // Play the timing bar for a player user.
            if (delay > 0.0f &&
                GetIsPC(user))
            {
                PlayerPlugin.StartGuiTimingBar(user, delay);
            }

            // Apply the item's action if specified.
            if (itemDetail.ApplyAction != null)
            {
                var actionId = Guid.NewGuid().ToString();
                Activity.SetBusy(user, ActivityStatusType.UseItem);
                SetLocalBool(user, actionId, true);
                CheckPosition(user, actionId, userPosition);

                DelayCommand(delay + 0.1f, () =>
                {
                    DeleteLocalBool(user, actionId);
                    Activity.ClearBusy(user);

                    var updatedPosition = GetPosition(user);

                    // Check if user has moved.
                    if (userPosition.X != updatedPosition.X ||
                        userPosition.Y != updatedPosition.Y ||
                        userPosition.Z != updatedPosition.Z)
                    {
                        return;
                    }

                    // Rerun validation since things may have changed since the user started the action.
                    validationMessage = itemDetail.ValidateAction == null ? string.Empty : itemDetail.ValidateAction(user, item, target, targetLocation);
                    if (!string.IsNullOrWhiteSpace(validationMessage))
                    {
                        SendMessageToPC(user, validationMessage);
                        return;
                    }

                    itemDetail.ApplyAction(user, item, target, targetLocation);

                    // Reduce item charge if specified.
                    var reducesItemCharge = itemDetail.ReducesItemChargeAction?.Invoke(user, item, target, targetLocation) ?? false;
                    if (reducesItemCharge)
                    {
                        SetItemCharges(item, GetItemCharges(item) - 1);
                    }
                });
            }
        }

        /// <summary>
        /// Checks all of the "Use Limitation: Perk" item properties on an item against a creature's effective level in the required perk.
        /// If player meets or exceeds the level required for all item properties, returns true. Otherwise returns false.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="item">The item to pull requirements from.</param>
        /// <returns>true if all requirements met, false otherwise</returns>
        public static bool CanCreatureUseItem(uint creature, uint item)
        {
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.UseLimitationPerk)
                {
                    var perkType = (PerkType)GetItemPropertySubType(ip);
                    var levelRequired = GetItemPropertyCostTableValue(ip);

                    if (Perk.GetEffectivePerkLevel(creature, perkType) < levelRequired)
                        return false;
                }
            }

            return true;
        }
        
        /// <summary>
        /// Returns an item to a target.
        /// </summary>
        /// <param name="target">The target receiving the item.</param>
        /// <param name="item">The item being returned.</param>
        public static void ReturnItem(uint target, uint item)
        {
            if (GetHasInventory(item))
            {
                var possessor = GetItemPossessor(item);
                AssignCommand(possessor, () =>
                {
                    ActionGiveItem(item, target);
                });
            }
            else
            {
                CopyItem(item, target, true);
                DestroyObject(item);
            }
        }

        /// <summary>
        /// Returns the number of items in an object's inventory.
        /// Returns -1 if target does not have an inventory
        /// </summary>
        /// <param name="obj">The object to check</param>
        /// <returns>-1 if obj doesn't have an inventory, otherwise returns the number of items in the inventory</returns>
        public static int GetInventoryItemCount(uint obj)
        {
            if (!GetHasInventory(obj)) return -1;

            int count = 0;
            var item = GetFirstItemInInventory(obj);
            while (GetIsObjectValid(item))
            {
                count++;
                item = GetNextItemInInventory(obj);
            }

            return count;
        }

        /// <summary>
        /// Retrieves the armor type of an item.
        /// This is based on the Use Limitation: Perk property.
        /// If it's not specified, ArmorType.Invalid will be returned.
        /// </summary>
        /// <param name="item">The item to be checked.</param>
        /// <returns>The ArmorType value of the item. Returns ArmorType.Invalid if neither Light or Heavy are found.</returns>
        public static ArmorType GetArmorType(uint item)
        {
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) != ItemPropertyType.UseLimitationPerk) continue;

                var perkType = (PerkType) GetItemPropertySubType(ip);
                if (Perk.HeavyArmorPerks.Contains(perkType))
                {
                    return ArmorType.Heavy;
                }
                else if (Perk.LightArmorPerks.Contains(perkType))
                {
                    return ArmorType.Light;
                }
            }

            return ArmorType.Invalid;
        }

        /// <summary>
        /// Retrieves the list of weapon base item types.
        /// </summary>
        public static List<BaseItem> WeaponBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.BastardSword,
            BaseItem.Longsword,
            BaseItem.Katana,
            BaseItem.Scimitar,
            BaseItem.BattleAxe,
            BaseItem.Dagger,
            BaseItem.Rapier,
            BaseItem.ShortSword,
            BaseItem.Kukri,
            BaseItem.Sickle,
            BaseItem.Whip,
            BaseItem.HandAxe,
            BaseItem.Lightsaber,
            BaseItem.GreatAxe,
            BaseItem.GreatSword,
            BaseItem.DwarvenWarAxe,
            BaseItem.Halberd,
            BaseItem.Scythe,
            BaseItem.ShortSpear,
            BaseItem.Trident,
            BaseItem.DoubleAxe,
            BaseItem.TwoBladedSword,
            BaseItem.Saberstaff,
            BaseItem.Katar,
            BaseItem.QuarterStaff,
            BaseItem.LightMace,
            BaseItem.Pistol,
            BaseItem.ThrowingAxe,
            BaseItem.Shuriken,
            BaseItem.Dart,
            BaseItem.Cannon,
            BaseItem.Longbow,
        };

        /// <summary>
        /// Retrieves the list of armor base item types.
        /// </summary>
        public static List<BaseItem> ArmorBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.Armor,
            BaseItem.Helmet,
            BaseItem.Cloak,
            BaseItem.Belt,
            BaseItem.Amulet,
            BaseItem.Boots,
            BaseItem.LargeShield,
            BaseItem.SmallShield,
            BaseItem.TowerShield,
            BaseItem.Gloves,
            BaseItem.Bracer,
            BaseItem.Ring
        };

        /// <summary>
        /// Retrieves the list of Vibroblade base item types.
        /// </summary>
        public static List<BaseItem> VibrobladeBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.BastardSword,
            BaseItem.Longsword,
            BaseItem.Katana,
            BaseItem.Scimitar,
            BaseItem.BattleAxe
        };

        /// <summary>
        /// Retrieves the list of Finesse Vibroblade base item types.
        /// </summary>
        public static List<BaseItem> FinesseVibrobladeBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.Dagger,
            BaseItem.Rapier,
            BaseItem.ShortSword,
            BaseItem.Kukri,
            BaseItem.Sickle,
            BaseItem.Whip,
            BaseItem.HandAxe,
        };

        /// <summary>
        /// Retrieves the list of Lightsaber base item types.
        /// </summary>
        public static List<BaseItem> LightsaberBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.Lightsaber
        };

        /// <summary>
        /// Retrieves the list of Heavy Vibroblade base item types.
        /// </summary>
        public static List<BaseItem> HeavyVibrobladeBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.GreatAxe,
            BaseItem.GreatSword,
            BaseItem.DwarvenWarAxe
        };

        /// <summary>
        /// Retrieves the list of Polearm base item types.
        /// </summary>
        public static List<BaseItem> PolearmBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.Halberd,
            BaseItem.Scythe,
            BaseItem.ShortSpear,
            BaseItem.Trident
        };

        /// <summary>
        /// Retrieves the list of Twin Blade base item types.
        /// </summary>
        public static List<BaseItem> TwinBladeBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.DoubleAxe,
            BaseItem.TwoBladedSword
        };

        /// <summary>
        /// Retrieves the list of Saberstaff base item types.
        /// </summary>
        public static List<BaseItem> SaberstaffBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.Saberstaff,
        };

        /// <summary>
        /// Retrieves the list of Katar base item types.
        /// </summary>
        public static List<BaseItem> KatarBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.Katar
        };

        /// <summary>
        /// Retrieves the list of Staff base item types.
        /// </summary>
        public static List<BaseItem> StaffBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.QuarterStaff,
            BaseItem.LightMace,
            BaseItem.Club,
            BaseItem.MorningStar
        };

        /// <summary>
        /// Retrieves the list of Pistol base item types.
        /// </summary>
        public static List<BaseItem> PistolBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.Pistol
        };

        /// <summary>
        /// Retrieves the list of Throwing Weapon base item types.
        /// </summary>
        public static List<BaseItem> ThrowingWeaponBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.ThrowingAxe,
            BaseItem.Shuriken,
            BaseItem.Dart
        };
        
        /// <summary>
        /// Retrieves the list of Rifle base item types.
        /// </summary>
        public static List<BaseItem> RifleBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.Longbow
        };

        /// <summary>
        /// Retrieves the list of One-Handed weapon types.
        /// These are the weapons which are held in one hand and not necessarily associated with the One-Handed skill.
        /// </summary>
        public static List<BaseItem> OneHandedMeleeItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.BastardSword,
            BaseItem.Longsword,
            BaseItem.Katana,
            BaseItem.Scimitar,
            BaseItem.BattleAxe,
            BaseItem.Dagger,
            BaseItem.Rapier,
            BaseItem.ShortSword,
            BaseItem.Kukri,
            BaseItem.Sickle,
            BaseItem.Whip,
            BaseItem.HandAxe,
            BaseItem.Lightsaber,
            BaseItem.ShortSpear,
            BaseItem.Katar,
        };

        /// <summary>
        /// Retrieves the list of Two-Handed melee weapon types.
        /// These are the weapons which are held in two hand and not necessarily associated with the Two-Handed skill.
        /// </summary>
        public static List<BaseItem> TwoHandedMeleeItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.GreatAxe,
            BaseItem.GreatSword,
            BaseItem.DwarvenWarAxe,
            BaseItem.Halberd,
            BaseItem.Scythe,
            BaseItem.Trident,
            BaseItem.DoubleAxe,
            BaseItem.TwoBladedSword,
            BaseItem.Saberstaff,
            BaseItem.QuarterStaff,
            BaseItem.LightMace,
        };


        /// <summary>
        /// Retrieves the icon used on the UIs. 
        /// </summary>
        /// <param name="item">The item to retrieve the icon for.</param>
        /// <returns>A resref of the icon to use.</returns>
        public static string GetIconResref(uint item)
        {
            var baseItem = GetBaseItemType(item);

            if (baseItem == BaseItem.Cloak) // Cloaks use PLTs so their default icon doesn't really work
                return "iit_cloak";
            else if (baseItem == BaseItem.SpellScroll || baseItem == BaseItem.EnchantedScroll)
            {// Scrolls get their icon from the cast spell property
                if (GetItemHasItemProperty(item, ItemPropertyType.CastSpell))
                {
                    for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                    {
                        if (GetItemPropertyType(ip) == ItemPropertyType.CastSpell)
                            return Get2DAString("iprp_spells", "Icon", GetItemPropertySubType(ip));
                    }
                }
            }
            else if (Get2DAString("baseitems", "ModelType", (int)baseItem) == "0")
            {// Create the icon resref for simple modeltype items
                var sSimpleModelId = GetItemAppearance(item, ItemAppearanceType.SimpleModel, 0).ToString();
                while (GetStringLength(sSimpleModelId) < 3)
                {
                    sSimpleModelId = "0" + sSimpleModelId;
                }

                var sDefaultIcon = Get2DAString("baseitems", "DefaultIcon", (int)baseItem);
                switch (baseItem)
                {
                    case BaseItem.MiscSmall:
                    case BaseItem.CraftMaterialSmall:
                        sDefaultIcon = "iit_smlmisc_" + sSimpleModelId;
                        break;
                    case BaseItem.MiscMedium:
                    case BaseItem.CraftMaterialMedium:
                    case BaseItem.CraftBase:
                        sDefaultIcon = "iit_midmisc_" + sSimpleModelId;
                        break;
                    case BaseItem.MiscLarge:
                        sDefaultIcon = "iit_talmisc_" + sSimpleModelId;
                        break;
                    case BaseItem.MiscThin:
                        sDefaultIcon = "iit_thnmisc_" + sSimpleModelId;
                        break;
                }

                var nLength = GetStringLength(sDefaultIcon);
                if (GetSubString(sDefaultIcon, nLength - 4, 1) == "_")// Some items have a default icon of xx_yyy_001, we strip the last 4 symbols if that is the case
                    sDefaultIcon = GetStringLeft(sDefaultIcon, nLength - 4);
                var sIcon = sDefaultIcon + "_" + sSimpleModelId;
                if (ResManGetAliasFor(sIcon, ResType.TGA) != "")// Check if the icon actually exists, if not, we'll fall through and return the default icon
                    return sIcon;
            }

            // For everything else use the item's default icon
            return Get2DAString("baseitems", "DefaultIcon", (int)baseItem);
        }

        /// <summary>
        /// Builds a string containing all of the item properties on an item.
        /// </summary>
        /// <param name="item">The item to use.</param>
        /// <returns>A string containing all of the item properties.</returns>
        public static string BuildItemPropertyString(uint item)
        {
            var sb = new StringBuilder();

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                BuildSingleItemPropertyString(sb, ip);
                sb.Append("\n");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds a list of strings containing all of the item properties on an item.
        /// </summary>
        /// <param name="item">The item to use.</param>
        /// <returns>A list containing all of the item properties.</returns>
        public static GuiBindingList<string> BuildItemPropertyList(uint item)
        {
            var list = new GuiBindingList<string>();
            var sb = new StringBuilder();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                BuildSingleItemPropertyString(sb, ip);
                list.Add(sb.ToString());
                sb.Clear();
            }

            return list;
        }

        private static void BuildSingleItemPropertyString(StringBuilder sb, ItemProperty ip)
        {
            var typeId = (int)GetItemPropertyType(ip);
            var name = GetStringByStrRef(Convert.ToInt32(Get2DAString("itempropdef", "GameStrRef", typeId)));
            sb.Append(name);

            var subTypeId = GetItemPropertySubType(ip);
            if (subTypeId != -1)
            {
                var subTypeResref = Get2DAString("itempropdef", "SubTypeResRef", typeId);
                var strRefId = StringToInt(Get2DAString(subTypeResref, "Name", subTypeId));
                if (strRefId != 0)
                {
                    var text = $" {GetStringByStrRef(strRefId)}";
                    sb.Append(text);
                }
            }

            var param1 = GetItemPropertyParam1(ip);
            if (param1 != -1)
            {
                var paramResref = Get2DAString("iprp_paramtable", "TableResRef", param1);
                var strRef = StringToInt(Get2DAString(paramResref, "Name", GetItemPropertyParam1Value(ip)));
                if (strRef != 0)
                {
                    var text = $" {GetStringByStrRef(strRef)}";
                    sb.Append(text);
                }
            }

            var costTable = GetItemPropertyCostTable(ip);
            if (costTable != -1)
            {
                var costTableResref = Get2DAString("iprp_costtable", "Name", costTable);
                var strRef = StringToInt(Get2DAString(costTableResref, "Name", GetItemPropertyCostTableValue(ip)));
                if (strRef != 0)
                {
                    var text = $" {GetStringByStrRef(strRef)}";
                    sb.Append(text);
                }
            }
        }

        /// <summary>
        /// Determines whether an item can be stored persistently in the database.
        /// </summary>
        /// <param name="player">The player attempting to persistently store the item.</param>
        /// <param name="item">The item being stored.</param>
        /// <returns>An error message if validation fails, otherwise an empty string if it succeeds.</returns>
        public static string CanBePersistentlyStored(uint player, uint item)
        {
            if (GetItemPossessor(item) != player)
            {
                return "Item must be in your inventory.";
            }

            if (GetHasInventory(item))
            {
                return "Containers cannot be stored.";
            }

            if (GetBaseItemType(item) == BaseItem.Gold)
            {
                return "Credits cannot be placed inside.";
            }

            if (GetItemCursedFlag(item))
            {
                return "That item cannot be stored.";
            }

            return string.Empty;
        }
    }
}
