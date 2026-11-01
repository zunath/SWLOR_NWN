using System.Globalization;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>Player-facing names and rider descriptions of routines and sets. Pure.</summary>
    public static class PerformanceRiderText
    {
        /// <param name="perkLevel">The performer's level of the routine's perk; Light Feet shows it as a numeral.</param>
        public static string GetName(PerformanceRoutineType routine, int perkLevel = 0)
        {
            return routine switch
            {
                PerformanceRoutineType.LightFeet => WithNumeral("Light Feet", perkLevel),
                PerformanceRoutineType.PowerStep => "Power Step",
                PerformanceRoutineType.Quickstep => "Quickstep",
                PerformanceRoutineType.VigorReel => "Vigor Reel",
                _ => "Cantina Shuffle"
            };
        }

        /// <param name="perkLevel">The performer's level of the set's perk; Steady Hands shows it as a numeral.</param>
        public static string GetName(PerformanceSetType set, int perkLevel = 0)
        {
            return set switch
            {
                PerformanceSetType.SteadyHands => WithNumeral("Steady Hands", perkLevel),
                PerformanceSetType.KeenEar => "Keen Ear",
                PerformanceSetType.AnglersLullaby => "Angler's Lullaby",
                PerformanceSetType.StillWaters => "Still Waters",
                PerformanceSetType.SilverTongue => "Silver Tongue",
                _ => "Cantina Standards"
            };
        }

        /// <summary>What an unlocked routine adds to the Inspired of the performer's audience.</summary>
        public static string GetRider(PerformanceRoutineType routine, int lightFeetSpeedPercent, int attributeBonus)
        {
            var attribute = 1 + Math.Max(0, attributeBonus);
            return routine switch
            {
                PerformanceRoutineType.LightFeet => $"+{Math.Max(0, lightFeetSpeedPercent)}% move speed",
                PerformanceRoutineType.PowerStep => $"+{attribute} MGT",
                PerformanceRoutineType.Quickstep => $"+{attribute} AGI",
                PerformanceRoutineType.VigorReel => $"+{attribute} VIT",
                _ => "XP and debt only"
            };
        }

        /// <summary>What an unlocked set adds to the Inspired of the performer's audience.</summary>
        public static string GetRider(PerformanceSetType set, int steadyHandsBonus, int anglersLullabyBonus, int attributeBonus)
        {
            var attribute = 1 + Math.Max(0, attributeBonus);
            var craft = Math.Max(0, steadyHandsBonus);
            return set switch
            {
                PerformanceSetType.SteadyHands => $"+{craft} Control, +{craft} Craftsmanship",
                PerformanceSetType.KeenEar => $"+{attribute} PER",
                PerformanceSetType.AnglersLullaby => $"+{Math.Max(0, anglersLullabyBonus)}% fishing",
                PerformanceSetType.StillWaters => $"+{attribute} WIL",
                PerformanceSetType.SilverTongue => $"+{attribute} SOC",
                _ => "XP and debt only"
            };
        }

        public static string GetLockedText(int requiredRank)
        {
            return requiredRank > 0
                ? string.Create(CultureInfo.InvariantCulture, $"Performance {requiredRank}")
                : "Locked";
        }

        public static string GetNumeral(int level)
        {
            return level switch
            {
                1 => "I",
                2 => "II",
                3 => "III",
                4 => "IV",
                _ => string.Empty
            };
        }

        private static string WithNumeral(string name, int perkLevel)
        {
            var numeral = GetNumeral(perkLevel);
            return numeral.Length == 0 ? name : $"{name} {numeral}";
        }
    }
}
