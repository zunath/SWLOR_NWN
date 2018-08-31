using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Rune.Contracts;

namespace SWLOR.Game.Server.Rune
{
    public class LevelRune: IRune
    {
        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int level = target.RecommendedLevel;

            if (value > 0 && level >= 120)
                return "You can't raise that item's recommended level any further.";
            else if (value < 0 && level <= 0)
                return "You can't lower that item's recommended level any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int level = target.RecommendedLevel + value;

            if (level > 120) level = 120;
            else if (level < 0) level = 0;

            target.RecommendedLevel = level;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            
            return "Level " + value;
        }
    }
}
