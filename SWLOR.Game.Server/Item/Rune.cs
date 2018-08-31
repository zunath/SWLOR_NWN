using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.Rune.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Item
{
    public class Rune : IActionItem
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly IItemService _item;
        private readonly IRuneService _rune;
        private readonly IDataContext _db;
        private readonly IColorTokenService _color;
        private readonly ISkillService _skill;

        public Rune(
            INWScript script,
            IPerkService perk,
            IItemService item,
            IRuneService rune,
            IDataContext db,
            IColorTokenService color,
            ISkillService skill)
        {
            _perk = perk;
            _item = item;
            _ = script;
            _rune = rune;
            _db = db;
            _color = color;
            _skill = skill;
        }

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem runeItem, NWObject target, Location targetLocation, CustomData customData)
        {
            NWPlayer player = NWPlayer.Wrap(user.Object);
            NWItem targetItem = NWItem.Wrap(target.Object);
            RuneSlots slots = _rune.GetRuneSlots(targetItem);
            CustomItemPropertyType runeType = _rune.GetRuneType(runeItem);
            int runeID = runeItem.GetLocalInt("RUNE_ID");
            string[] runeArgs = runeItem.GetLocalString("RUNE_VALUE").Split(',');
            int runeLevel = runeItem.RecommendedLevel;
            int levelIncrease = runeItem.LevelIncrease;

            var dbRune = _db.Runes.Single(x => x.RuneID == runeID && x.IsActive);
            IRune rune = App.ResolveByInterface<IRune>("Rune." + dbRune.Script);
            rune.Apply(player, targetItem, runeArgs);

            string description = rune.Description(player, targetItem, runeArgs);
            bool usePrismatic = false;
            switch (runeType)
            {
                case CustomItemPropertyType.RedRune:
                    if (slots.FilledRedSlots < slots.RedSlots)
                    {
                        targetItem.SetLocalInt("RUNIC_SLOT_RED_" + (slots.FilledRedSlots + 1), runeID);
                        targetItem.SetLocalString("RUNIC_SLOT_RED_DESC_" + (slots.FilledRedSlots + 1), description);
                        player.SendMessage("Rune installed into " + _color.Red("red") + " slot #" + (slots.FilledRedSlots + 1));
                    }
                    else usePrismatic = true;
                    break;
                case CustomItemPropertyType.BlueRune:
                    if (slots.FilledBlueSlots < slots.BlueSlots)
                    {
                        targetItem.SetLocalInt("RUNIC_SLOT_BLUE_" + (slots.FilledBlueSlots + 1), runeID);
                        targetItem.SetLocalString("RUNIC_SLOT_BLUE_DESC_" + (slots.FilledBlueSlots + 1), description);
                        player.SendMessage("Rune installed into " + _color.Blue("blue") + " slot #" + (slots.FilledBlueSlots + 1));
                    }
                    else usePrismatic = true;
                    break;
                case CustomItemPropertyType.GreenRune:
                    if (slots.FilledBlueSlots < slots.GreenSlots)
                    {
                        targetItem.SetLocalInt("RUNIC_SLOT_GREEN_" + (slots.FilledGreenSlots + 1), runeID);
                        targetItem.SetLocalString("RUNIC_SLOT_GREEN_DESC_" + (slots.FilledGreenSlots + 1), description);
                        player.SendMessage("Rune installed into " + _color.Green("green") + " slot #" + (slots.FilledGreenSlots + 1));
                    }
                    else usePrismatic = true;
                    break;
                case CustomItemPropertyType.YellowRune:
                    if (slots.FilledBlueSlots < slots.YellowSlots)
                    {
                        targetItem.SetLocalInt("RUNIC_SLOT_YELLOW_" + (slots.FilledYellowSlots + 1), runeID);
                        targetItem.SetLocalString("RUNIC_SLOT_YELLOW_DESC_" + (slots.FilledYellowSlots + 1), description);
                        player.SendMessage("Rune installed into " + _color.Yellow("yellow") + " slot #" + (slots.FilledYellowSlots + 1));
                    }
                    else usePrismatic = true;
                    break;
            }

            if (usePrismatic)
            {
                string prismaticText = _rune.PrismaticString();
                targetItem.SetLocalInt("RUNIC_SLOT_PRISMATIC_" + (slots.FilledPrismaticSlots + 1), runeID);
                targetItem.SetLocalString("RUNIC_SLOT_PRISMATIC_DESC_" + (slots.FilledPrismaticSlots + 1), description);
                player.SendMessage("Rune installed into " + prismaticText + " slot #" + (slots.FilledPrismaticSlots + 1));
            }

            targetItem.RecommendedLevel += levelIncrease;
            runeItem.Destroy();

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

            PCSkill pcSkill = _skill.GetPCSkill(player, skillType);
            int xp = (int)_skill.CalculateRegisteredSkillLevelAdjustedXP(400, runeLevel, pcSkill.Rank);
            _skill.GiveSkillXP(player, skillType, xp);
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWPlayer userPlayer = NWPlayer.Wrap(user.Object);
            NWItem targetItem = NWItem.Wrap(target.Object);
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

        public float MaxDistance()
        {
            return 0;
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return false;
        }

        public string IsValidTarget(NWCreature user, NWItem rune, NWObject target, Location targetLocation)
        {
            if (target.ObjectType != NWScript.OBJECT_TYPE_ITEM) return "Only items may be targeted by Runes.";
            if (!user.IsPlayer) return "Only players may use runes.";
            NWPlayer player = NWPlayer.Wrap(user.Object);
            NWItem targetItem = NWItem.Wrap(target.Object);

            int runeLevel = rune.RecommendedLevel;
            int itemLevel = targetItem.RecommendedLevel;
            int requiredPerkLevel = runeLevel / 5;
            if (requiredPerkLevel <= 0) requiredPerkLevel = 1;
            int perkLevel = 0;
            CustomItemPropertyType runeType = _rune.GetRuneType(rune);
            RuneSlots runeSlots = _rune.GetRuneSlots(targetItem);
            int runeID = rune.GetLocalInt("RUNE_ID");
            string[] runeArgs = rune.GetLocalString("RUNE_VALUE").Split(',');

            // Check for a misconfigured rune item.
            if (runeType == CustomItemPropertyType.Unknown) return "Rune color couldn't be found. Notify an admin that this rune item is not set up properly.";
            if (runeID <= 0) return "Rune ID couldn't be found. Notify an admin that this rune item is not set up properly.";
            if (runeArgs.Length <= 0) return "Rune value couldn't be found. Notify an admin that this rune item is not set up properly.";

            // No available slots on target item
            if (runeType == CustomItemPropertyType.RedRune && !runeSlots.CanRedRuneBeAdded) return "That item has no available red runic slots.";
            if (runeType == CustomItemPropertyType.BlueRune && !runeSlots.CanBlueRuneBeAdded) return "That item has no available blue runic slots.";
            if (runeType == CustomItemPropertyType.GreenRune && !runeSlots.CanGreenRuneBeAdded) return "That item has no available green runic slots.";
            if (runeType == CustomItemPropertyType.YellowRune && !runeSlots.CanYellowRuneBeAdded) return "That item has no available yellow runic slots.";

            // Get the perk level based on target item type and rune type.
            if (_item.WeaponBaseItemTypes.Contains(targetItem.BaseItemType))
            {
                switch (runeType)
                {
                    case CustomItemPropertyType.RedRune:
                        perkLevel = _perk.GetPCPerkLevel(player, PerkType.CombatRuneInstallationWeapons);
                        break;
                    case CustomItemPropertyType.BlueRune:
                        perkLevel = _perk.GetPCPerkLevel(player, PerkType.MagicRuneInstallationWeapons);
                        break;
                    case CustomItemPropertyType.GreenRune:
                        perkLevel = _perk.GetPCPerkLevel(player, PerkType.CraftingRuneInstallationWeapons);
                        break;
                    case CustomItemPropertyType.YellowRune:
                        perkLevel = _perk.GetPCPerkLevel(player, PerkType.SpecialRuneInstallationWeapons);
                        break;
                    default:
                        perkLevel = 0;
                        break;
                }
            }
            else if (_item.ArmorBaseItemTypes.Contains(targetItem.BaseItemType))
            {
                switch (runeType)
                {
                    case CustomItemPropertyType.RedRune:
                        perkLevel = _perk.GetPCPerkLevel(player, PerkType.CombatRuneInstallationArmors);
                        break;
                    case CustomItemPropertyType.BlueRune:
                        perkLevel = _perk.GetPCPerkLevel(player, PerkType.MagicRuneInstallationArmors);
                        break;
                    case CustomItemPropertyType.GreenRune:
                        perkLevel = _perk.GetPCPerkLevel(player, PerkType.CraftingRuneInstallationArmors);
                        break;
                    case CustomItemPropertyType.YellowRune:
                        perkLevel = _perk.GetPCPerkLevel(player, PerkType.SpecialRuneInstallationArmors);
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
                    return "Targeted item must be unequipped before installing a rune.";
                }
            }

            // Check for perk level requirement
            if (perkLevel < requiredPerkLevel) return "You do not have the necessary perk rank required. (Required: " + requiredPerkLevel + ")";

            // Can't modify items above perk level * 10
            if (itemLevel > perkLevel * 10) return "Your current perks allow you to add runes to items up to level " + perkLevel * 10 + ". This item is level " + itemLevel + " so you can't install a rune into it.";

            // Item must be in the user's inventory.
            if (!targetItem.Possessor.Equals(player)) return "Targeted item must be in your inventory.";

            // Look for a database entry for this rune type.
            var dbRune = _db.Runes.SingleOrDefault(x => x.RuneID == runeID && x.IsActive);
            if (dbRune == null)
            {
                return "Couldn't find a matching rune ID in the database. Inform an admin of this issue.";
            }

            // Run the individual rune's rules for application. Will return the error message or a null.
            IRune runeRules = App.ResolveByInterface<IRune>("Rune." + dbRune.Script);
            return runeRules.CanApply(player, targetItem, runeArgs);
        }

        public bool AllowLocationTarget()
        {
            return false;
        }
    }
}
