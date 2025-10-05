using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Bioware;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.ValueObjects;

namespace SWLOR.Component.Inventory.Definitions.ItemDefinition
{
    public class SaberUpgradeItemDefinition: IItemListDefinition
    {
        private const int MaxNumberOfUpgrades = 1;
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        private IItemBuilder Builder => _serviceProvider.GetRequiredService<IItemBuilder>();

        public SaberUpgradeItemDefinition(IDatabaseService db, IServiceProvider serviceProvider)
        {
            _db = db;
            _serviceProvider = serviceProvider;
        }

        public Dictionary<string, ItemDetail> BuildItems()
        {
            UpgradeKit();
            return Builder.Build();
        }

        private int GetWeaponLevel(uint item)
        {
            return GetLocalInt(item , "LIGHTSABER_UPGRADE_COUNT");
        }

        private void SetLightsaberLevel(uint item, int level)
        {
            SetLocalInt(item, "LIGHTSABER_UPGRADE_COUNT", level);
        }

        private void CreateKit(string tag, string itemName, BaseItemType expectedItemType, int upgradeNumber, int dmgIncrease)
        {
            Builder.Create(tag)
                .Delay(12f)
                .PlaysAnimation(AnimationType.LoopingGetMid)
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

                    if (GetItemInSlot(InventorySlotType.RightHand, user) == target ||
                        GetItemInSlot(InventorySlotType.LeftHand, user) == target)
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
            CreateKit("saber_upg1", "Lightsaber", BaseItemType.Lightsaber, 1, 4);
            CreateKit("saberstaff_upg1", "Saberstaff", BaseItemType.Saberstaff, 1, 4);
        }
    }
}
