using System.Collections.Generic;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class LightsaberItemDefinition: IItemListDefinition
    {
        private const int MaxNumberOfUpgrades = 4;
        private readonly ItemBuilder _builder = new ItemBuilder();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            Lightsaber();
            Saberstaff();
            UpgradeKit();
            return _builder.Build();
        }

        private int GetLightsaberLevel(uint item)
        {
            return GetLocalInt(item , "LIGHTSABER_UPGRADE_COUNT");
        }

        private void SetLightsaberLevel(uint item, int level)
        {
            SetLocalInt(item, "LIGHTSABER_UPGRADE_COUNT", level);
        }

        private void Lightsaber()
        {
            _builder.Create("lightsaber")
                .Delay(12f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .MaxDistance(0.0f)
                .ValidationAction((user, item, target, location) =>
                {
                    var itemType = GetBaseItemType(target);
                    if (itemType != BaseItem.Lightsaber)
                    {
                        return "Only other lightsabers may be targeted.";
                    }

                    if (item == target)
                    {
                        return "A different lightsaber must be targeted to form a saberstaff.";
                    }

                    if (GetLightsaberLevel(item) != GetLightsaberLevel(target))
                    {
                        return "Both lightsabers must be the same level to form a saberstaff.";
                    }

                    if (GetItemPossessor(item) != GetItemPossessor(target))
                    {
                        return "Both lightsabers must be in your inventory.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, lightsaber1, lightsaber2, location) =>
                {
                    var lightsaber1Serialized = Object.Serialize(lightsaber1);
                    var lightsaber2Serialized = Object.Serialize(lightsaber2);

                    var level = GetLightsaberLevel(lightsaber1) + 1;
                    var saberstaff = CreateItemOnObject("saberstaff", user);

                    // Serialize the individual lightsabers onto the saberstaff
                    SetLocalString(saberstaff, "LIGHTSABER_1", lightsaber1Serialized);
                    SetLocalString(saberstaff, "LIGHTSABER_2", lightsaber2Serialized);

                    // Modify the color of the saberstaff to match that of the first lightsaber.
                    var lightsaber1Color = GetItemAppearance(lightsaber1, ItemAppearanceType.WeaponColor, 0);
                    var finalSaberstaff = CopyItemAndModify(saberstaff, ItemAppearanceType.WeaponModel, 0, lightsaber1Color, true);

                    // Adjust item properties
                    var enhancementItemProperty = ItemPropertyEnhancementBonus(level);
                    var perkRequirementItemProperty = ItemPropertyCustom(ItemPropertyType.UseLimitationPerk, (int)PerkType.SaberstaffProficiency, level);
                    BiowareXP2.IPSafeAddItemProperty(finalSaberstaff, enhancementItemProperty, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, true);
                    BiowareXP2.IPSafeAddItemProperty(finalSaberstaff, perkRequirementItemProperty, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, true);

                    // Destroy the original saberstaff, keeping the one we just copied and modified.
                    DestroyObject(saberstaff);

                    // Destroy the individual lightsabers
                    DestroyObject(lightsaber1);
                    DestroyObject(lightsaber2);

                    SendMessageToPC(user, "You combine two lightsabers to form a saberstaff.");
                });
        }

        private void Saberstaff()
        {
            _builder.Create("saberstaff")
                .Delay(12f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .MaxDistance(0.0f)
                .ValidationAction((user, item, target, location) =>
                {
                    var saber1 = GetLocalString(item, "LIGHTSABER_1");
                    var saber2 = GetLocalString(item, "LIGHTSABER_2");

                    if (string.IsNullOrWhiteSpace(saber1) ||
                        string.IsNullOrWhiteSpace(saber2))
                    {
                        return "This saberstaff cannot be dismantled.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location) =>
                {
                    var saber1 = Object.Deserialize(GetLocalString(item, "LIGHTSABER_1"));
                    var saber2 = Object.Deserialize(GetLocalString(item, "LIGHTSABER_2"));

                    Object.AcquireItem(user, saber1);
                    Object.AcquireItem(user, saber2);

                    DestroyObject(item);

                    SendMessageToPC(user, "Your saberstaff has been dismantled into two independent lightsabers.");
                });
        }

        private void UpgradeKit()
        {
            _builder.Create("saber_upgrade_kit")
                .Delay(12f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .MaxDistance(0.0f)
                .ValidationAction((user, item, target, location) =>
                {
                    if (!GetIsPC(user) || GetIsDM(user))
                    {
                        return "Only players may use this kit.";
                    }

                    var itemType = GetBaseItemType(target);
                    var numberOfUpgrades = GetLightsaberLevel(target);
                    if (numberOfUpgrades >= MaxNumberOfUpgrades)
                    {
                        return $"Lightsabers may only be upgraded {MaxNumberOfUpgrades} times.";
                    }

                    if (itemType != BaseItem.Lightsaber)
                    {
                        return "Only lightsabers may be upgraded with this kit.";
                    }

                    var playerId = GetObjectUUID(user);
                    var dbPlayer = DB.Get<Player>(playerId);

                    if (dbPlayer.CharacterType != CharacterType.ForceSensitive)
                    {
                        return "Only force sensitive characters may use this kit.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location) =>
                {
                    var numberOfUpgrades = GetLightsaberLevel(target) + 1;
                    SetLightsaberLevel(target, numberOfUpgrades);

                    var bonus = 1 + numberOfUpgrades;
                    var enhancementItemProperty = ItemPropertyEnhancementBonus(bonus);
                    var perkRequirementItemProperty = ItemPropertyCustom(ItemPropertyType.UseLimitationPerk, (int)PerkType.LightsaberProficiency, bonus);

                    BiowareXP2.IPSafeAddItemProperty(target, enhancementItemProperty, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, true);
                    BiowareXP2.IPSafeAddItemProperty(target, perkRequirementItemProperty, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, true);

                    DestroyObject(item);
                    SendMessageToPC(user, $"Your lightsaber has been upgraded to level {bonus}.");
                });

        }
    }
}
