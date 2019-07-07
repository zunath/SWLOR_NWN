using System;

using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

using NWN;
using SWLOR.Game.Server.Bioware;

namespace SWLOR.Game.Server.Mod
{
    public class AbilityScoreMod: IModHandler
    {
        private class ParsedData
        {
            public string TypeName { get; set; }
            public int Amount { get; set; }
            public int CurrentValue { get; set; }
            public string Description { get; set; }
        }

        public int ModTypeID => 1;
        private const int MaxValue = 17;
        
        private ParsedData ParseData(NWItem target, params string[] data)
        {
            ParsedData result = new ParsedData();

            // STR, DEX, CON, INT, WIS, or CHA
            result.TypeName = data[0];
            result.Amount = Convert.ToInt32(data[1]);

            switch (result.TypeName)
            {
                case "STR":
                    result.CurrentValue = target.StrengthBonus;
                    break;
                case "CON":
                    result.CurrentValue = target.ConstitutionBonus;
                    break;
                case "DEX":
                    result.CurrentValue = target.DexterityBonus;
                    break;
                case "WIS":
                    result.CurrentValue = target.WisdomBonus;
                    break;
                case "INT":
                    result.CurrentValue = target.IntelligenceBonus;
                    break;
                case "CHA":
                    result.CurrentValue = target.CharismaBonus;
                    break;
            }

            result.Description = result.TypeName + " +" + result.Amount;
            return result;
        }

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            var data = ParseData(target, args);
            return data.CurrentValue >= MaxValue ? "You cannot improve that item's stat any further." : null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            var data = ParseData(target, args);
            
            int newValue = data.CurrentValue + data.Amount;
            if (newValue > MaxValue) newValue = MaxValue;

            switch (data.TypeName)
            {
                case "STR":
                    target.StrengthBonus = newValue;
                    break;
                case "CON":
                    target.ConstitutionBonus = newValue;
                    break;
                case "DEX":
                    target.DexterityBonus = newValue;
                    break;
                case "WIS":
                    target.WisdomBonus = newValue;
                    break;
                case "INT":
                    target.IntelligenceBonus = newValue;
                    break;
                case "CHA":
                    target.CharismaBonus = newValue;
                    break;
            }
            
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            var data = ParseData(target, args);
            return data.Description;
        }
    }
}
