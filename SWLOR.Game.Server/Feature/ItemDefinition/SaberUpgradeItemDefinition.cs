using System.Collections.Generic;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class SaberUpgradeItemDefinition: IItemListDefinition
    {
        private const string SaberTierVariable = "SABER_TIER";

        // Tier 6 is the display tier 5.5 (Chiro).
        private const int ChiroTier = 6;

        private static readonly Dictionary<int, int> _lightsaberDamage = new()
        {
            [1] = 5, [2] = 9, [3] = 13, [4] = 17, [5] = 21, [ChiroTier] = 24
        };
        private static readonly Dictionary<int, int> _saberstaffDamage = new()
        {
            [1] = 7, [2] = 11, [3] = 15, [4] = 19, [5] = 25, [ChiroTier] = 29
        };

        private static readonly Dictionary<int, int> _requiredSkill = new()
        {
            [1] = 0, [2] = 10, [3] = 20, [4] = 30, [5] = 40, [ChiroTier] = 50
        };

        // iprp_skill rows for the weapon skill requirement property.
        private const int LightsaberSkillSubtype = 38;
        private const int SaberstaffSkillSubtype = 42;

        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            CreateKit("saber_upg2", "Lightsaber", BaseItem.Lightsaber, 2);
            CreateKit("saber_upg3", "Lightsaber", BaseItem.Lightsaber, 3);
            CreateKit("saber_upg4", "Lightsaber", BaseItem.Lightsaber, 4);
            CreateKit("saber_upg5", "Lightsaber", BaseItem.Lightsaber, 5);
            CreateKit("saber_upgchi", "Lightsaber", BaseItem.Lightsaber, ChiroTier);

            CreateKit("staff_upg2", "Saberstaff", BaseItem.Saberstaff, 2);
            CreateKit("staff_upg3", "Saberstaff", BaseItem.Saberstaff, 3);
            CreateKit("staff_upg4", "Saberstaff", BaseItem.Saberstaff, 4);
            CreateKit("staff_upg5", "Saberstaff", BaseItem.Saberstaff, 5);
            CreateKit("staff_upgchi", "Saberstaff", BaseItem.Saberstaff, ChiroTier);

            return _builder.Build();
        }

        private static int GetSaberTier(uint item)
        {
            return GetLocalInt(item, SaberTierVariable);
        }

        private static string TierLabel(int tier)
        {
            return tier == ChiroTier ? "5.5" : tier.ToString();
        }

        private void CreateKit(string tag, string itemName, BaseItem expectedItemType, int targetTier)
        {
            var damageByTier = expectedItemType == BaseItem.Saberstaff
                ? _saberstaffDamage
                : _lightsaberDamage;
            var skillSubtype = expectedItemType == BaseItem.Saberstaff
                ? SaberstaffSkillSubtype
                : LightsaberSkillSubtype;

            _builder.Create(tag)
                .Delay(12f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .MaxDistance(0.0f)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (!GetIsPC(user) || GetIsDM(user))
                    {
                        return "Only players may use this kit.";
                    }

                    if (GetBaseItemType(target) != expectedItemType)
                    {
                        return $"Only {itemName.ToLower()}s may be upgraded with this kit.";
                    }

                    var currentTier = GetSaberTier(target);
                    if (currentTier <= 0)
                    {
                        return $"This {itemName.ToLower()} cannot be upgraded with kits.";
                    }

                    if (currentTier != targetTier - 1)
                    {
                        return $"This kit upgrades tier {TierLabel(targetTier - 1)} {itemName.ToLower()}s to tier {TierLabel(targetTier)}. This weapon is tier {TierLabel(currentTier)}.";
                    }

                    var playerId = GetObjectUUID(user);
                    var dbPlayer = DB.Get<Player>(playerId);

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
                    // Add the tier-to-tier DMG delta on top of whatever DMG the weapon
                    // carries, preserving bonuses gained from enhancements or submission
                    // tokens at the workbench.
                    var totalDMG = damageByTier[targetTier] - damageByTier[targetTier - 1];

                    for (var ip = GetFirstItemProperty(target); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(target))
                    {
                        if (GetItemPropertyType(ip) == ItemPropertyType.DMG)
                        {
                            totalDMG += GetItemPropertyCostTableValue(ip);
                        }
                    }

                    var dmgItemProperty = ItemPropertyCustom(ItemPropertyType.DMG, -1, totalDMG);
                    BiowareXP2.IPSafeAddItemProperty(target, dmgItemProperty, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);

                    // Raise the weapon's skill requirement to the new tier's requirement.
                    for (var ip = GetFirstItemProperty(target); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(target))
                    {
                        if (GetItemPropertyType(ip) == ItemPropertyType.RequiresSkill)
                        {
                            RemoveItemProperty(target, ip);
                        }
                    }

                    var skillRequirement = ItemPropertyCustom(ItemPropertyType.RequiresSkill, skillSubtype, _requiredSkill[targetTier]);
                    BiowareXP2.IPSafeAddItemProperty(target, skillRequirement, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);

                    SetLocalInt(target, SaberTierVariable, targetTier);
                    DestroyObject(item);
                    SendMessageToPC(user, $"Your {itemName.ToLower()} has been upgraded to tier {TierLabel(targetTier)}.");
                });
        }
    }
}
