
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Enumeration;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Item
    {
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
        /// Retrieves the list of Knuckles base item types.
        /// </summary>
        public static List<BaseItem> KnucklesBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.Knuckles
        };

        /// <summary>
        /// Retrieves the list of Staff base item types.
        /// </summary>
        public static List<BaseItem> StaffBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.QuarterStaff,
            BaseItem.LightMace
        };

        /// <summary>
        /// Retrieves the list of Pistol base item types.
        /// </summary>
        public static List<BaseItem> PistolBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.ShortBow
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
        /// Retrieves the list of Cannon base item types.
        /// </summary>
        public static List<BaseItem> CannonBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.HeavyCrossbow
        };

        /// <summary>
        /// Retrieves the list of Rifle base item types.
        /// </summary>
        public static List<BaseItem> RifleBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.Longbow
        };
    }
}
