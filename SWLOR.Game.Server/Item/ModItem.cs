using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Mod.Contracts;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;
using System.Collections.Generic;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.NWN.Enum.Item;
using static SWLOR.Game.Server.NWN._;

namespace SWLOR.Game.Server.Item
{
    public class ModItem : IActionItem
    {
        public string CustomKey => null;

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem modItem, NWObject target, Location targetLocation, CustomData customData)
        {
            NWPlayer player = (user.Object);
            NWItem targetItem = (target.Object);
            ModSlots slots = ModService.GetModSlots(targetItem);
            ItemPropertyType modType = ModService.GetModType(modItem);
            int modID = modItem.GetLocalInt("RUNE_ID");
            string[] modArgs = modItem.GetLocalString("RUNE_VALUE").Split(',');
            int modLevel = modItem.RecommendedLevel;
            int levelIncrease = modItem.LevelIncrease;

            var mod = ModService.GetModHandler(modID);
            mod.Apply(player, targetItem, modArgs);

            string description = mod.Description(player, targetItem, modArgs);
            bool usePrismatic = false;
            switch (modType)
            {
                case ItemPropertyType.RedMod:
                    if (slots.FilledRedSlots < slots.RedSlots)
                    {
                        targetItem.SetLocalInt("MOD_SLOT_RED_" + (slots.FilledRedSlots + 1), modID);
                        targetItem.SetLocalString("MOD_SLOT_RED_DESC_" + (slots.FilledRedSlots + 1), description);
                        player.SendMessage("Mod installed into " + ColorTokenService.Red("red") + " slot #" + (slots.FilledRedSlots + 1));
                    }
                    else usePrismatic = true;
                    break;
                case ItemPropertyType.BlueMod:
                    if (slots.FilledBlueSlots < slots.BlueSlots)
                    {
                        targetItem.SetLocalInt("MOD_SLOT_BLUE_" + (slots.FilledBlueSlots + 1), modID);
                        targetItem.SetLocalString("MOD_SLOT_BLUE_DESC_" + (slots.FilledBlueSlots + 1), description);
                        player.SendMessage("Mod installed into " + ColorTokenService.Blue("blue") + " slot #" + (slots.FilledBlueSlots + 1));
                    }
                    else usePrismatic = true;
                    break;
                case ItemPropertyType.GreenMod:
                    if (slots.FilledGreenSlots < slots.GreenSlots)
                    {
                        targetItem.SetLocalInt("MOD_SLOT_GREEN_" + (slots.FilledGreenSlots + 1), modID);
                        targetItem.SetLocalString("MOD_SLOT_GREEN_DESC_" + (slots.FilledGreenSlots + 1), description);
                        player.SendMessage("Mod installed into " + ColorTokenService.Green("green") + " slot #" + (slots.FilledGreenSlots + 1));
                    }
                    else usePrismatic = true;
                    break;
                case ItemPropertyType.YellowMod:
                    if (slots.FilledYellowSlots < slots.YellowSlots)
                    {
                        targetItem.SetLocalInt("MOD_SLOT_YELLOW_" + (slots.FilledYellowSlots + 1), modID);
                        targetItem.SetLocalString("MOD_SLOT_YELLOW_DESC_" + (slots.FilledYellowSlots + 1), description);
                        player.SendMessage("Mod installed into " + ColorTokenService.Yellow("yellow") + " slot #" + (slots.FilledYellowSlots + 1));
                    }
                    else usePrismatic = true;
                    break;
            }

            if (usePrismatic)
            {
                string prismaticText = ModService.PrismaticString();
                targetItem.SetLocalInt("MOD_SLOT_PRISMATIC_" + (slots.FilledPrismaticSlots + 1), modID);
                targetItem.SetLocalString("MOD_SLOT_PRISMATIC_DESC_" + (slots.FilledPrismaticSlots + 1), description);
                player.SendMessage("Mod installed into " + prismaticText + " slot #" + (slots.FilledPrismaticSlots + 1));
            }

            targetItem.RecommendedLevel += levelIncrease;
            modItem.Destroy();

            SkillType skillType;
            
            if(GetLocalBool(targetItem, "LIGHTSABER") == true)
            {
                skillType = SkillType.Engineering;
            }
            else if (ArmorBaseItemTypes.Contains(targetItem.BaseItemType))
            {
                skillType = SkillType.Armorsmith;
            }
            else if (WeaponsmithBaseItemTypes.Contains(targetItem.BaseItemType))
            {
                skillType = SkillType.Weaponsmith;
            }
            else if (EngineeringBaseItemTypes.Contains(targetItem.BaseItemType))
            {
                skillType = SkillType.Engineering;
            }
            else return;

            int rank = SkillService.GetPCSkillRank(player, skillType);
            int xp = (int)SkillService.CalculateRegisteredSkillLevelAdjustedXP(400, modLevel, rank);
            SkillService.GiveSkillXP(player, skillType, xp);
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWPlayer userPlayer = (user.Object);
            NWItem targetItem = (target.Object);
            float perkBonus = 0.0f;

            if(GetLocalBool(targetItem, "LIGHTSABER") == true)
            {
                perkBonus = PerkService.GetCreaturePerkLevel(userPlayer, PerkType.SpeedyEngineering) * 0.1f;
            }
            else if (ArmorBaseItemTypes.Contains(targetItem.BaseItemType))
            {
                perkBonus = PerkService.GetCreaturePerkLevel(userPlayer, PerkType.SpeedyArmorsmith) * 0.1f;
            }
            else if (WeaponsmithBaseItemTypes.Contains(targetItem.BaseItemType))
            {
                perkBonus = PerkService.GetCreaturePerkLevel(userPlayer, PerkType.SpeedyWeaponsmith) * 0.1f;
            }
            else if (EngineeringBaseItemTypes.Contains(targetItem.BaseItemType))
            {
                perkBonus = PerkService.GetCreaturePerkLevel(userPlayer, PerkType.SpeedyEngineering) * 0.1f;
            }


            float seconds = 18.0f - (18.0f * perkBonus);
            if (seconds <= 0.1f) seconds = 0.1f;
            return seconds;
        }

        public bool FaceTarget()
        {
            return false;
        }

        public Animation AnimationID()
        {
            return Animation.LoopingGetMid;
        }

        public float MaxDistance(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return 0;
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return false;
        }

        public string IsValidTarget(NWCreature user, NWItem mod, NWObject target, Location targetLocation)
        {
            if (target.ObjectType != ObjectType.Item) return "Only items may be targeted by mods.";
            if (!user.IsPlayer) return "Only players may use mods.";
            NWPlayer player = (user.Object);
            NWItem targetItem = (target.Object);

            int modLevel = mod.RecommendedLevel;
            int itemLevel = targetItem.RecommendedLevel;
            int requiredPerkLevel = modLevel / 5;
            if (requiredPerkLevel <= 0) requiredPerkLevel = 1;
            int perkLevel = 0;
            ItemPropertyType modType = ModService.GetModType(mod);
            ModSlots modSlots = ModService.GetModSlots(targetItem);
            int modID = mod.GetLocalInt("RUNE_ID");
            string[] modArgs = mod.GetLocalString("RUNE_VALUE").Split(',');

            // Check for a misconfigured mod item.
            if (modType == ItemPropertyType.Invalid) return "Mod color couldn't be found. Notify an admin that this mod item is not set up properly.";
            if (modID <= 0) return "Mod ID couldn't be found. Notify an admin that this mod item is not set up properly.";
            if (modArgs.Length <= 0) return "Mod value couldn't be found. Notify an admin that this mod item is not set up properly.";

            // No available slots on target item
            if (modType == ItemPropertyType.RedMod && !modSlots.CanRedModBeAdded) return "That item has no available red mod slots.";
            if (modType == ItemPropertyType.BlueMod && !modSlots.CanBlueModBeAdded) return "That item has no available blue mod slots.";
            if (modType == ItemPropertyType.GreenMod && !modSlots.CanGreenModBeAdded) return "That item has no available green mod slots.";
            if (modType == ItemPropertyType.YellowMod && !modSlots.CanYellowModBeAdded) return "That item has no available yellow mod slots.";

            // Get the perk level based on target item type and mod type.
            if (GetLocalBool(targetItem, "LIGHTSABER") == false && WeaponsmithBaseItemTypes.Contains(targetItem.BaseItemType))
            {
                perkLevel = PerkService.GetCreaturePerkLevel(player, PerkType.WeaponModInstallation);
            }
            else if (ArmorBaseItemTypes.Contains(targetItem.BaseItemType))
            {
                perkLevel = PerkService.GetCreaturePerkLevel(player, PerkType.ArmorModInstallation);
            }
            else if (GetLocalBool(targetItem, "LIGHTSABER") == true || EngineeringBaseItemTypes.Contains(targetItem.BaseItemType))
            {
                perkLevel = PerkService.GetCreaturePerkLevel(player, PerkType.EngineeringModInstallation);
            }

            // Ensure item isn't equipped.
            for (int slot = 0; slot < NumberOfInventorySlots; slot++)
            {
                if (_.GetItemInSlot((InventorySlot)slot, user.Object) == targetItem.Object)
                {
                    return "Targeted item must be unequipped before installing a mod.";
                }
            }

            // Check for perk level requirement
            if (perkLevel < requiredPerkLevel) return "You do not have the necessary perk rank required. (Required: " + requiredPerkLevel + ", Your level: " + perkLevel + ")";

            // Can't modify items above perk level * 10
            if (itemLevel > perkLevel * 10) return "Your current perks allow you to add mods to items up to level " + perkLevel * 10 + ". This item is level " + itemLevel + " so you can't install a mod into it.";

            // Item must be in the user's inventory.
            if (!targetItem.Possessor.Equals(player)) return "Targeted item must be in your inventory.";

            // It's possible that this mod is no longer usable. Notify the player if we can't find one registered.
            if (!ModService.IsModHandlerRegistered(modID))
                return "Unfortunately, this mod can no longer be used.";

            var handler = ModService.GetModHandler(modID);
            // Run the individual mod's rules for application. Will return the error message or a null.
            return handler.CanApply(player, targetItem, modArgs);
        }

        public bool AllowLocationTarget()
        {
            return false;
        }

        //------------------------------------------------------------------------------------------------------------
        // Note - these are very similar to the lists in ItemService.  However, there are some  differences
        // based on where items are crafted.  For example, gloves are only in ArmorBaseItems here but are also in
        // WeaponBaseItems in ItemService, and engineering items are in their own list here.  These lists are only 
        // used for the purpose of mod perks, the lists in ItemService are used for "is this item a weapon/armor/etc". 
        //------------------------------------------------------------------------------------------------------------
        public static HashSet<BaseItem> ArmorBaseItemTypes = new HashSet<BaseItem>()
        {
            BaseItem.Amulet,
            BaseItem.Armor,
            BaseItem.Bracer,
            BaseItem.Belt,
            BaseItem.Boots,
            BaseItem.Cloak,
            BaseItem.Gloves,
            BaseItem.Helmet,
            BaseItem.LargeShield,
            BaseItem.SmallShield,
            BaseItem.TowerShield,
            BaseItem.Ring
        };

        private static readonly HashSet<BaseItem> WeaponsmithBaseItemTypes = new HashSet<BaseItem>()
        {
            BaseItem.BastardSword,
            BaseItem.BattleAxe,
            BaseItem.Club,
            BaseItem.Dagger,
            BaseItem.Dart,
            BaseItem.DireMace,
            BaseItem.DoubleAxe,
            BaseItem.DwarvenWarAxe,
            BaseItem.GreatAxe,
            BaseItem.GreatSword,
            BaseItem.Grenade,
            BaseItem.Halberd,
            BaseItem.HandAxe,
            BaseItem.HeavyFlail,
            BaseItem.Kama,
            BaseItem.Katana,
            BaseItem.Kukri,
            BaseItem.LightFlail,
            BaseItem.LightHammer,
            BaseItem.LightMace,
            BaseItem.Longsword,
            BaseItem.MorningStar,
            BaseItem.QuarterStaff,
            BaseItem.Rapier,
            BaseItem.Scimitar,
            BaseItem.Scythe,
            BaseItem.ShortSpear,
            BaseItem.ShortSword,
            BaseItem.Shuriken,
            BaseItem.Sickle,
            BaseItem.ThrowingAxe,
            BaseItem.Trident,
            BaseItem.TwoBladedSword,
            BaseItem.WarHammer,
            BaseItem.Whip,
        };

        private static readonly HashSet<BaseItem> EngineeringBaseItemTypes = new HashSet<BaseItem>()
        {
            BaseItem.Arrow,
            BaseItem.Bolt,
            BaseItem.Bullet,
            BaseItem.HeavyCrossbow,
            BaseItem.LightCrossbow,
            BaseItem.Longbow,
            BaseItem.ShortBow,
            BaseItem.Sling,
            BaseItem.Saberstaff,
            BaseItem.Lightsaber

        };

    }
}
