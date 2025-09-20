using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Bioware;
using SWLOR.Shared.Core.Service;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class SaberUpgradeItemDefinition: IItemListDefinition
    {
        private const int MaxNumberOfUpgrades = 1;
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            UpgradeKit();
            return _builder.Build();
        }

        private int GetWeaponLevel(uint item)
        {
            return GetLocalInt(item , "LIGHTSABER_UPGRADE_COUNT");
        }

        private void SetLightsaberLevel(uint item, int level)
        {
            SetLocalInt(item, "LIGHTSABER_UPGRADE_COUNT", level);
        }

        private void CreateKit(string tag, string itemName, BaseItem expectedItemType, int upgradeNumber, int dmgIncrease)
        {
            _builder.Create(tag)
                .Delay(12f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .MaxDistance(0.0f)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var itemType = GetBaseItemType(target);
                    var numberOfUpgrades = GetWeaponLevel(target);

                    if (!GetIsPC(user) || GetIsDM(user))
                    {
                        return "Only players may use this kit.";
                    }

                    if (itemType != expectedItemType)
                    {
                        return $"Only {itemName.ToLower()}s may be upgraded with this kit.";
                    }

                    if (numberOfUpgrades >= MaxNumberOfUpgrades)
                    {
                        return $"{itemName}s may only be upgraded {MaxNumberOfUpgrades} time(s).";
                    }

                    if (numberOfUpgrades + 1 != upgradeNumber)
                    {
                        return $"This kit cannot be used on this item.";
                    }

                    var playerId = GetObjectUUID(user);
                    var dbPlayer = _db.Get<Player>(playerId);

                    if (dbPlayer.CharacterType != CharacterType.ForceSensitive)
                    {
                        return "Only force sensitive characters may use this kit.";
                    }

                    if (GetItemInSlot(InventorySlot.RightHand, user) == target ||
                        GetItemInSlot(InventorySlot.LeftHand, user) == target)
                    {
                        return "Weapon must be unequipped.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var numberOfUpgrades = GetWeaponLevel(target) + 1;
                    var physicalDMG = 0;

                    for (var ip = GetFirstItemProperty(target); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(target))
                    {
                        var type = GetItemPropertyType(ip);
                        var subType = GetItemPropertySubType(ip);
                        if (type == ItemPropertyType.DMG && subType == (int)CombatDamageType.Physical)
                        {
                            physicalDMG += GetItemPropertyCostTableValue(ip);
                        }
                    }

                    physicalDMG += dmgIncrease;

                    var dmgItemProperty = ItemPropertyCustom(ItemPropertyType.DMG, (int)CombatDamageType.Physical, physicalDMG);
                    BiowareXP2.IPSafeAddItemProperty(target, dmgItemProperty, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);

                    DestroyObject(item);
                    SendMessageToPC(user, $"Your {itemName.ToLower()} has been upgraded to level {numberOfUpgrades}.");
                    SetLightsaberLevel(target, numberOfUpgrades);
                });
        }

        private void UpgradeKit()
        {
            CreateKit("saber_upg1", "Lightsaber", BaseItem.Lightsaber, 1, 4);
            CreateKit("saberstaff_upg1", "Saberstaff", BaseItem.Saberstaff, 1, 4);
        }
    }
}
