namespace SWLOR.Game.Server.Service.FishingService
{
    /// <summary>
    /// Quick Reel: the choice a bite offers besides the fight. The angler lands the fish, or not, at once
    /// by the catch roll fishing had before the Line: <c>40 + (Agriculture rank - fish level) x 10 + 2 x Willpower score</c> percent, kept
    /// between 0 and 95. It takes no fight time, pays today's XP for the bite and nothing more, and spends the bait as today.
    /// </summary>
    public static class FishingQuickReel
    {
        /// <summary>
        /// The Willpower score Quick Reel rolls with, the same for every angler so that fishing no longer reads an attribute: 10, today's
        /// catch roll for a character with no attribute investment.
        /// </summary>
        public const int ReferenceWillpowerScore = 10;

        /// <summary>The chance, in percent, before the rank and Willpower.</summary>
        public const int BaseChance = 40;

        /// <summary>Percent added for each Agriculture rank above the fish's level, or taken off for each below.</summary>
        public const int ChancePerRank = 10;

        /// <summary>Percent added for each point of Willpower score.</summary>
        public const int ChancePerWillpowerPoint = 2;

        /// <summary>The most any roll can give, so a fish is never certain.</summary>
        public const int MaximumChance = 95;

        /// <summary>
        /// The chance, in percent, that Quick Reel lands a fish of the level for an angler of the rank. The Willpower score is the
        /// reference one unless a study asks for another.
        /// </summary>
        public static int CatchChancePercent(int agricultureRank, int fishLevel, int willpowerScore = ReferenceWillpowerScore)
        {
            var chance = BaseChance + (agricultureRank - fishLevel) * ChancePerRank + willpowerScore * ChancePerWillpowerPoint;
            return Math.Clamp(chance, 0, MaximumChance);
        }
    }
}
