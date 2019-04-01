using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class LevelMod: IModHandler
    {
        public int ModTypeID => 27;
        private const int MinValue = 0;
        private const int MaxValue = 120;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int level = target.RecommendedLevel;

            if (value > MinValue && level >= MaxValue)
                return "You can't raise that item's recommended level any further.";
            else if (value < MinValue && level <= MinValue)
                return "You can't lower that item's recommended level any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int level = target.RecommendedLevel + value;

            if (level > MaxValue) level = MaxValue;
            else if (level < MinValue) level = MinValue;

            target.RecommendedLevel = level;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            
            return "Level " + value;
        }
    }
}
