using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Mod.Contracts;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Item
{
    public class ModItem : IActionItem
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly IItemService _item;
        private readonly IModService _mod;
        private readonly IDataContext _db;
        private readonly IColorTokenService _color;
        private readonly ISkillService _skill;

        public ModItem(
            INWScript script,
            IPerkService perk,
            IItemService item,
            IModService mod,
            IDataContext db,
            IColorTokenService color,
            ISkillService skill)
        {
            _perk = perk;
            _item = item;
            _ = script;
            _mod = mod;
            _db = db;
            _color = color;
            _skill = skill;
        }

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem modItem, NWObject target, Location targetLocation, CustomData customData)
        {
            NWPlayer player = (user.Object);
            NWItem targetItem = (target.Object);
            ModSlots slots = _mod.GetModSlots(targetItem);
            CustomItemPropertyType modType = _mod.GetModType(modItem);
            int modID = modItem.GetLocalInt("RUNE_ID");
            string[] modArgs = modItem.GetLocalString("RUNE_VALUE").Split(',');
            int modLevel = modItem.RecommendedLevel;
            int levelIncrease = modItem.LevelIncrease;

            var dbMod = _db.Mods.Single(x => x.ModID == modID && x.IsActive);
            App.ResolveByInterface<IMod>("Mod." + dbMod.Script, mod =>
            {
                mod.Apply(player, targetItem, modArgs);

                string description = mod.Description(player, targetItem, modArgs);
                bool usePrismatic = false;
                switch (modType)
                {
                    case CustomItemPropertyType.RedMod:
                        if (slots.FilledRedSlots < slots.RedSlots)
                        {
                            targetItem.SetLocalInt("MOD_SLOT_RED_" + (slots.FilledRedSlots + 1), modID);
                            targetItem.SetLocalString("MOD_SLOT_RED_DESC_" + (slots.FilledRedSlots + 1), description);
                            player.SendMessage("Mod installed into " + _color.Red("red") + " slot #" + (slots.FilledRedSlots + 1));
                        }
                        else usePrismatic = true;
                        break;
                    case CustomItemPropertyType.BlueMod:
                        if (slots.FilledBlueSlots < slots.BlueSlots)
                        {
                            targetItem.SetLocalInt("MOD_SLOT_BLUE_" + (slots.FilledBlueSlots + 1), modID);
                            targetItem.SetLocalString("MOD_SLOT_BLUE_DESC_" + (slots.FilledBlueSlots + 1), description);
                            player.SendMessage("Mod installed into " + _color.Blue("blue") + " slot #" + (slots.FilledBlueSlots + 1));
                        }
                        else usePrismatic = true;
                        break;
                    case CustomItemPropertyType.GreenMod:
                        if (slots.FilledBlueSlots < slots.GreenSlots)
                        {
                            targetItem.SetLocalInt("MOD_SLOT_GREEN_" + (slots.FilledGreenSlots + 1), modID);
                            targetItem.SetLocalString("MOD_SLOT_GREEN_DESC_" + (slots.FilledGreenSlots + 1), description);
                            player.SendMessage("Mod installed into " + _color.Green("green") + " slot #" + (slots.FilledGreenSlots + 1));
                        }
                        else usePrismatic = true;
                        break;
                    case CustomItemPropertyType.YellowMod:
                        if (slots.FilledBlueSlots < slots.YellowSlots)
                        {
                            targetItem.SetLocalInt("MOD_SLOT_YELLOW_" + (slots.FilledYellowSlots + 1), modID);
                            targetItem.SetLocalString("MOD_SLOT_YELLOW_DESC_" + (slots.FilledYellowSlots + 1), description);
                            player.SendMessage("Mod installed into " + _color.Yellow("yellow") + " slot #" + (slots.FilledYellowSlots + 1));
                        }
                        else usePrismatic = true;
                        break;
                }

                if (usePrismatic)
                {
                    string prismaticText = _mod.PrismaticString();
                    targetItem.SetLocalInt("MOD_SLOT_PRISMATIC_" + (slots.FilledPrismaticSlots + 1), modID);
                    targetItem.SetLocalString("MOD_SLOT_PRISMATIC_DESC_" + (slots.FilledPrismaticSlots + 1), description);
                    player.SendMessage("Mod installed into " + prismaticText + " slot #" + (slots.FilledPrismaticSlots + 1));
                }

                targetItem.RecommendedLevel += levelIncrease;
                modItem.Destroy();

                SkillType skillType;
                if (_item.ArmorBaseItemTypes.Contains(targetItem.BaseItemType))
                {
                    skillType = SkillType.Armorsmith;
                }
                else if (_item.WeaponBaseItemTypes.Contains(targetItem.BaseItemType))
                {
                    skillType = SkillType.Weaponsmith;
                }
                else return;

                int rank = _skill.GetPCSkillRank(player, skillType);
                int xp = (int)_skill.CalculateRegisteredSkillLevelAdjustedXP(400, modLevel, rank);
                _skill.GiveSkillXP(player, skillType, xp);
            });
            
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWPlayer userPlayer = (user.Object);
            NWItem targetItem = (target.Object);
            float perkBonus = 0.0f;

            if (_item.ArmorBaseItemTypes.Contains(targetItem.BaseItemType))
            {
                perkBonus = _perk.GetPCPerkLevel(userPlayer, PerkType.SpeedyArmorsmith) * 0.1f;
            }
            else if (_item.WeaponBaseItemTypes.Contains(targetItem.BaseItemType))
            {
                perkBonus = _perk.GetPCPerkLevel(userPlayer, PerkType.SpeedyWeaponsmith) * 0.1f;
            }

            float seconds = 18.0f - (18.0f * perkBonus);
            if (seconds <= 0.1f) seconds = 0.1f;
            return seconds;
        }

        public bool FaceTarget()
        {
            return false;
        }

        public int AnimationID()
        {
            return NWScript.ANIMATION_LOOPING_GET_MID;
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
            if (target.ObjectType != NWScript.OBJECT_TYPE_ITEM) return "Only items may be targeted by mods.";
            if (!user.IsPlayer) return "Only players may use mods.";
            NWPlayer player = (user.Object);
            NWItem targetItem = (target.Object);

            int modLevel = mod.RecommendedLevel;
            int itemLevel = targetItem.RecommendedLevel;
            int requiredPerkLevel = modLevel / 5;
            if (requiredPerkLevel <= 0) requiredPerkLevel = 1;
            int perkLevel = 0;
            CustomItemPropertyType modType = _mod.GetModType(mod);
            ModSlots modSlots = _mod.GetModSlots(targetItem);
            int modID = mod.GetLocalInt("RUNE_ID");
            string[] modArgs = mod.GetLocalString("RUNE_VALUE").Split(',');

            // Check for a misconfigured mod item.
            if (modType == CustomItemPropertyType.Unknown) return "Mod color couldn't be found. Notify an admin that this mod item is not set up properly.";
            if (modID <= 0) return "Mod ID couldn't be found. Notify an admin that this mod item is not set up properly.";
            if (modArgs.Length <= 0) return "Mod value couldn't be found. Notify an admin that this mod item is not set up properly.";

            // No available slots on target item
            if (modType == CustomItemPropertyType.RedMod && !modSlots.CanRedModBeAdded) return "That item has no available red mod slots.";
            if (modType == CustomItemPropertyType.BlueMod && !modSlots.CanBlueModBeAdded) return "That item has no available blue mod slots.";
            if (modType == CustomItemPropertyType.GreenMod && !modSlots.CanGreenModBeAdded) return "That item has no available green mod slots.";
            if (modType == CustomItemPropertyType.YellowMod && !modSlots.CanYellowModBeAdded) return "That item has no available yellow mod slots.";

            // Get the perk level based on target item type and mod type.
            if (_item.WeaponBaseItemTypes.Contains(targetItem.BaseItemType))
            {
                switch (modType)
                {
                    case CustomItemPropertyType.RedMod:
                        perkLevel = _perk.GetPCPerkLevel(player, PerkType.CombatModInstallationWeapons);
                        break;
                    case CustomItemPropertyType.BlueMod:
                        perkLevel = _perk.GetPCPerkLevel(player, PerkType.MagicModInstallationWeapons);
                        break;
                    case CustomItemPropertyType.GreenMod:
                        perkLevel = _perk.GetPCPerkLevel(player, PerkType.CraftingModInstallationWeapons);
                        break;
                    case CustomItemPropertyType.YellowMod:
                        perkLevel = _perk.GetPCPerkLevel(player, PerkType.SpecialModInstallationWeapons);
                        break;
                    default:
                        perkLevel = 0;
                        break;
                }
            }
            else if (_item.ArmorBaseItemTypes.Contains(targetItem.BaseItemType))
            {
                switch (modType)
                {
                    case CustomItemPropertyType.RedMod:
                        perkLevel = _perk.GetPCPerkLevel(player, PerkType.CombatModInstallationArmors);
                        break;
                    case CustomItemPropertyType.BlueMod:
                        perkLevel = _perk.GetPCPerkLevel(player, PerkType.MagicModInstallationArmors);
                        break;
                    case CustomItemPropertyType.GreenMod:
                        perkLevel = _perk.GetPCPerkLevel(player, PerkType.CraftingModInstallationArmors);
                        break;
                    case CustomItemPropertyType.YellowMod:
                        perkLevel = _perk.GetPCPerkLevel(player, PerkType.SpecialModInstallationArmors);
                        break;
                    default:
                        perkLevel = 0;
                        break;
                }
            }

            // Ensure item isn't equipped.
            for (int slot = 0; slot < NWScript.NUM_INVENTORY_SLOTS; slot++)
            {
                if (_.GetItemInSlot(slot, user.Object) == targetItem.Object)
                {
                    return "Targeted item must be unequipped before installing a mod.";
                }
            }

            // Check for perk level requirement
            if (perkLevel < requiredPerkLevel) return "You do not have the necessary perk rank required. (Required: " + requiredPerkLevel + ")";

            // Can't modify items above perk level * 10
            if (itemLevel > perkLevel * 10) return "Your current perks allow you to add mods to items up to level " + perkLevel * 10 + ". This item is level " + itemLevel + " so you can't install a mod into it.";

            // Item must be in the user's inventory.
            if (!targetItem.Possessor.Equals(player)) return "Targeted item must be in your inventory.";

            // Look for a database entry for this mod type.
            var dbMod = _db.Mods.SingleOrDefault(x => x.ModID == modID && x.IsActive);
            if (dbMod == null)
            {
                return "Couldn't find a matching mod ID in the database. Inform an admin of this issue.";
            }

            // Run the individual mod's rules for application. Will return the error message or a null.
            string canApply = App.ResolveByInterface<IMod, string>("Mod." + dbMod.Script, 
                (modRules) => modRules.CanApply(player, targetItem, modArgs));

            return canApply;
        }

        public bool AllowLocationTarget()
        {
            return false;
        }
    }
}
