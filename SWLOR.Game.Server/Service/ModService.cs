using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public class ModService : IModService
    {
        private readonly INWScript _;
        private readonly IColorTokenService _color;
        private readonly INWNXDamage _nwnxDamage;
        private readonly ISkillService _skill;

        public ModService(INWScript script,
            IColorTokenService color,
            INWNXDamage nwnxDamage,
            ISkillService skill)
        {
            _ = script;
            _color = color;
            _nwnxDamage = nwnxDamage;
            _skill = skill;
        }

        public CustomItemPropertyType GetModType(NWItem item)
        {
            CustomItemPropertyType ipType = CustomItemPropertyType.Unknown;
            foreach (var ip in item.ItemProperties)
            {
                int type = _.GetItemPropertyType(ip);
                if (type == (int)CustomItemPropertyType.RedMod ||
                    type == (int)CustomItemPropertyType.BlueMod ||
                    type == (int)CustomItemPropertyType.GreenMod ||
                    type == (int)CustomItemPropertyType.YellowMod)
                {
                    ipType = (CustomItemPropertyType)type;
                    break;
                }
            }

            return ipType;
        }

        public ModSlots GetModSlots(NWItem item)
        {
            ModSlots modSlots = new ModSlots();
            foreach (var ip in item.ItemProperties)
            {
                int type = _.GetItemPropertyType(ip);
                switch (type)
                {
                    case (int)CustomItemPropertyType.ModSlotRed:
                        modSlots.RedSlots++;
                        break;
                    case (int)CustomItemPropertyType.ModSlotBlue:
                        modSlots.YellowSlots++;
                        break;
                    case (int)CustomItemPropertyType.ModSlotGreen:
                        modSlots.GreenSlots++;
                        break;
                    case (int)CustomItemPropertyType.ModSlotYellow:
                        modSlots.YellowSlots++;
                        break;
                    case (int)CustomItemPropertyType.ModSlotPrismatic:
                        modSlots.PrismaticSlots++;
                        break;
                }
            }

            for (int red = 1; red <= modSlots.RedSlots; red++)
            {
                int modID = item.GetLocalInt("MOD_SLOT_RED_" + red);
                if (modID > 0)
                    modSlots.FilledRedSlots++;
            }
            for (int blue = 1; blue <= modSlots.BlueSlots; blue++)
            {
                int modID = item.GetLocalInt("MOD_SLOT_BLUE_" + blue);
                if (modID > 0)
                    modSlots.FilledBlueSlots++;
            }
            for (int green = 1; green <= modSlots.GreenSlots; green++)
            {
                int modID = item.GetLocalInt("MOD_SLOT_GREEN_" + green);
                if (modID > 0)
                    modSlots.FilledGreenSlots++;
            }
            for (int yellow = 1; yellow <= modSlots.YellowSlots; yellow++)
            {
                int modID = item.GetLocalInt("MOD_SLOT_YELLOW_" + yellow);
                if (modID > 0)
                    modSlots.FilledYellowSlots++;
            }
            for (int prismatic = 1; prismatic <= modSlots.PrismaticSlots; prismatic++)
            {
                int modID = item.GetLocalInt("MOD_SLOT_PRISMATIC_" + prismatic);
                if (modID > 0)
                    modSlots.FilledPrismaticSlots++;
            }

            return modSlots;
        }

        public bool IsRune(NWItem item)
        {
            return GetModType(item) != CustomItemPropertyType.Unknown;
        }

        public string PrismaticString()
        {
            return _color.Red("p") + _color.Orange("r") + _color.Yellow("i") + _color.Green("s") + _color.Blue("m") +
                                   _color.LightPurple("a") + _color.Purple("t") + _color.White("i") + _color.Black("c");
        }

        public string OnModuleExamine(string existingDescription, NWPlayer examiner, NWObject examinedObject)
        {
            if (examinedObject.ObjectType != NWScript.OBJECT_TYPE_ITEM) return existingDescription;
            NWItem examinedItem = (examinedObject.Object);
            string description = string.Empty;
            ModSlots slot = GetModSlots(examinedItem);
            
            for (int red = 1; red <= slot.FilledRedSlots; red++)
            {
                description += _color.Red("Red Slot #" + red + ": ") + examinedItem.GetLocalString("MOD_SLOT_RED_DESC_" + red) + "\n";
            }
            for (int blue = 1; blue <= slot.FilledBlueSlots; blue++)
            {
                description += _color.Red("Blue Slot #" + blue + ": ") + examinedItem.GetLocalString("MOD_SLOT_BLUE_DESC_" + blue) + "\n";
            }
            for (int green = 1; green <= slot.FilledGreenSlots; green++)
            {
                description += _color.Red("Green Slot #" + green + ": ") + examinedItem.GetLocalString("MOD_SLOT_GREEN_DESC_" + green) + "\n";
            }
            for (int yellow = 1; yellow <= slot.FilledYellowSlots; yellow++)
            {
                description += _color.Red("Yellow Slot #" + yellow + ": ") + examinedItem.GetLocalString("MOD_SLOT_YELLOW_DESC_" + yellow) + "\n";
            }
            for (int prismatic = 1; prismatic <= slot.FilledPrismaticSlots; prismatic++)
            {
                description += PrismaticString() + " Slot #" + prismatic + ": " + examinedItem.GetLocalString("MOD_SLOT_PRISMATIC_DESC_" + prismatic) + "\n";
            }
            
            return existingDescription + "\n" + description;
        }

        public void OnModuleApplyDamage()
        {
            var data = _nwnxDamage.GetDamageEventData();
            NWObject damager = data.Damager;
            NWItem weapon = (_.GetLastWeaponUsed(damager.Object));
            int damageBonus = weapon.DamageBonus;

            if (damager.IsPlayer)
            {
                NWPlayer player = (damager.Object);
                int itemLevel = weapon.RecommendedLevel;
                SkillType skill = _skill.GetSkillTypeForItem(weapon);
                int rank = _skill.GetPCSkill(player, skill).Rank;
                int delta = itemLevel - rank;
                if (delta >= 1) damageBonus--;
                damageBonus = damageBonus - delta / 5;

                if (damageBonus <= 0) damageBonus = 0;
            }

            data.Base += damageBonus;
            _nwnxDamage.SetDamageEventData(data);
        }
    }
}
