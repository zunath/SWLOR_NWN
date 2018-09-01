using System;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

using NWN;

namespace SWLOR.Game.Server.Mod
{
    public class AbilityScoreMod: IMod
    {
        private readonly INWScript _;
        private readonly IBiowareXP2 _biowareXP2;

        public AbilityScoreMod(INWScript script,
            IBiowareXP2 biowareXP2)
        {
            _ = script;
            _biowareXP2 = biowareXP2;
        }

        private Tuple<int, int> GetExistingIPInfo(NWItem item, int abilityType)
        {
            Tuple<int, int> result = new Tuple<int, int>(0, 0);
            foreach (var ip in item.ItemProperties)
            {
                int type = _.GetItemPropertyType(ip);
                if (type == NWScript.ITEM_PROPERTY_ABILITY_BONUS)
                {
                    int currentAbilityType = _.GetItemPropertySubType(ip);
                    if (currentAbilityType == abilityType)
                    {
                        int currentValue = _.GetItemPropertyCostTableValue(ip);
                        result = new Tuple<int, int>(abilityType, currentValue);
                        break;
                    }
                }
            }

            return result;
        }

        private Tuple<int, int, string> ParseData(params string[] data)
        {
            string strType = data[0];
            int amount = Convert.ToInt32(data[1]);
            int type = -1;

            switch (strType)
            {
                case "STR":
                    type = NWScript.ABILITY_STRENGTH;
                    break;
                case "CON":
                    type = NWScript.ABILITY_CONSTITUTION;
                    break;
                case "DEX":
                    type = NWScript.ABILITY_DEXTERITY;
                    break;
                case "WIS":
                    type = NWScript.ABILITY_WISDOM;
                    break;
                case "INT":
                    type = NWScript.ABILITY_INTELLIGENCE;
                    break;
                case "CHA":
                    type = NWScript.ABILITY_CHARISMA;
                    break;
            }

            return new Tuple<int, int, string>(type, amount, strType + " +" + amount);
        }

        

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            var parsed = ParseData(args);
            var info = GetExistingIPInfo(target, parsed.Item1);
            return info.Item2 >= 12 ? "You cannot improve that item's stat any further." : null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            var data = ParseData(args);
            if (data.Item1 < 0) return;

            var existingValues = GetExistingIPInfo(target, data.Item1);
            int newValue = data.Item2 + existingValues.Item2;
            if (newValue > 12) newValue = 12;
            
            ItemProperty ip = _.ItemPropertyAbilityBonus(data.Item1, newValue);
            ip = _.TagItemProperty(ip, "RUNE_IP");

            _biowareXP2.IPSafeAddItemProperty(target, ip, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            var data = ParseData(args);
            if (data.Item1 < 0) return "Invalid";
            return data.Item3;
        }
    }
}
