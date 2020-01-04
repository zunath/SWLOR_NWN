using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Mod.Contracts;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;
using System.Collections.Generic;
using SWLOR.Game.Server.NWScript.Enumerations;
using static NWN._;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

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

            Skill skill;
            
            if(targetItem.GetLocalBoolean("LIGHTSABER") == true)
            {
                skill = Skill.Engineering;
            }
            else if (ArmorBaseItemTypes.Contains(targetItem.BaseItemType))
            {
                skill = Skill.Armorsmith;
            }
            else if (WeaponsmithBaseItemTypes.Contains(targetItem.BaseItemType))
            {
                skill = Skill.Weaponsmith;
            }
            else if (EngineeringBaseItemTypes.Contains(targetItem.BaseItemType))
            {
                skill = Skill.Engineering;
            }
            else return;

            int rank = SkillService.GetPCSkillRank(player, skill);
            int xp = (int)SkillService.CalculateRegisteredSkillLevelAdjustedXP(400, modLevel, rank);
            SkillService.GiveSkillXP(player, skill, xp);
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWPlayer userPlayer = (user.Object);
            NWItem targetItem = (target.Object);
            float perkBonus = 0.0f;

            if(targetItem.GetLocalBoolean("LIGHTSABER") == true)
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

        public Animation AnimationType()
        {
            return Animation.Get_Mid;
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
            if (targetItem.GetLocalBoolean("LIGHTSABER") == false && WeaponsmithBaseItemTypes.Contains(targetItem.BaseItemType))
            {
                perkLevel = PerkService.GetCreaturePerkLevel(player, PerkType.WeaponModInstallation);
            }
            else if (ArmorBaseItemTypes.Contains(targetItem.BaseItemType))
            {
                perkLevel = PerkService.GetCreaturePerkLevel(player, PerkType.ArmorModInstallation);
            }
            else if (targetItem.GetLocalBoolean("LIGHTSABER") == true || EngineeringBaseItemTypes.Contains(targetItem.BaseItemType))
            {
                perkLevel = PerkService.GetCreaturePerkLevel(player, PerkType.EngineeringModInstallation);
            }

            // Ensure item isn't equipped.
            for (int slot = 0; slot < NWNConstants.NumberOfInventorySlots; slot++)
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
        public static HashSet<BaseItemType> ArmorBaseItemTypes = new HashSet<BaseItemType>()
        {
            BaseItemType.Amulet,
            BaseItemType.Armor,
            BaseItemType.Bracer,
            BaseItemType.Belt,
            BaseItemType.Boots,
            BaseItemType.Cloak,
            BaseItemType.Gloves,
            BaseItemType.Helmet,
            BaseItemType.LargeShield,
            BaseItemType.SmallShield,
            BaseItemType.TowerShield,
            BaseItemType.Ring
        };

        private static readonly HashSet<BaseItemType> WeaponsmithBaseItemTypes = new HashSet<BaseItemType>()
        {
            BaseItemType.BastardSword,
            BaseItemType.BattleAxe,
            BaseItemType.Club,
            BaseItemType.Dagger,
            BaseItemType.Dart,
            BaseItemType.DireMace,
            BaseItemType.DoubleAxe,
            BaseItemType.DwarvenWaraxe,
            BaseItemType.GreatAxe,
            BaseItemType.GreatSword,
            BaseItemType.Grenade,
            BaseItemType.Halberd,
            BaseItemType.HandAxe,
            BaseItemType.HeavyFlail,
            BaseItemType.Kama,
            BaseItemType.Katana,
            BaseItemType.Kukri,
            BaseItemType.LightFlail,
            BaseItemType.LightHammer,
            BaseItemType.LightMace,
            BaseItemType.LongSword,
            BaseItemType.Morningstar,
            BaseItemType.QuarterStaff,
            BaseItemType.Rapier,
            BaseItemType.Scimitar,
            BaseItemType.Scythe,
            BaseItemType.ShortSpear,
            BaseItemType.ShortSword,
            BaseItemType.Shuriken,
            BaseItemType.Sickle,
            BaseItemType.ThrowingAxe,
            BaseItemType.Trident,
            BaseItemType.TwoBladedSword,
            BaseItemType.Warhammer,
            BaseItemType.Whip,
        };

        private static readonly HashSet<BaseItemType> EngineeringBaseItemTypes = new HashSet<BaseItemType>()
        {
            BaseItemType.Arrow,
            BaseItemType.Bolt,
            BaseItemType.Bullet,
            BaseItemType.HeavyCrossBow,
            BaseItemType.LightCrossBow,
            BaseItemType.LongBow,
            BaseItemType.ShortBow,
            BaseItemType.Sling,
            BaseItemType.Saberstaff,
            BaseItemType.Lightsaber

        };

    }
}
