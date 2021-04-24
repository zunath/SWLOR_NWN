using System.Collections.Generic;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class LightsaberKitItemDefinition: IItemListDefinition
    {
        private const int MaxNumberOfUpgrades = 4;
        private readonly ItemBuilder _builder = new ItemBuilder();

        public Dictionary<string, ItemDetail> BuildItems()
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
                    var numberOfUpgrades = GetLocalInt(target, "LIGHTSABER_UPGRADE_COUNT");
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
                    var numberOfUpgrades = GetLocalInt(target, "LIGHTSABER_UPGRADE_COUNT") + 1;
                    SetLocalInt(target, "LIGHTSABER_UPGRADE_COUNT", numberOfUpgrades);

                    var bonus = 1 + numberOfUpgrades;
                    var enhancementItemProperty = ItemPropertyEnhancementBonus(bonus);
                    var perkRequirementItemProperty = ItemPropertyCustom(ItemPropertyType.UseLimitationPerk, (int)PerkType.LightsaberProficiency, bonus);

                    BiowareXP2.IPSafeAddItemProperty(target, enhancementItemProperty, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, true);
                    BiowareXP2.IPSafeAddItemProperty(target, perkRequirementItemProperty, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, true);

                    DestroyObject(item);
                    SendMessageToPC(user, $"Your lightsaber has been upgraded to level {bonus}.");
                });

            return _builder.Build();
        }
    }
}
