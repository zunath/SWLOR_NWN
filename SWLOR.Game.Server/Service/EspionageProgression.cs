using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Skill gates and practice ranges for the independent Espionage professions.
    /// A tier teaches through the rank which unlocks its successor, never to the skill cap indefinitely.
    /// </summary>
    public static class EspionageProgression
    {
        public const int MaximumRank = 50;
        private static readonly int[] PoisoncraftRanks = { 0, 15, 28, 40, 48 };
        private static readonly int[] TrapcraftRanks = { 0, 18, 30, 45, 50 };
        private static readonly int[] SlicingRanks = { 0, 22, 30, 42, 48 };

        public static int GetRequiredRank(PerkType profession, int tier)
        {
            var ranks = profession switch
            {
                PerkType.Poisoncraft => PoisoncraftRanks,
                PerkType.Trapcraft => TrapcraftRanks,
                PerkType.Slicing => SlicingRanks,
                _ => throw new ArgumentOutOfRangeException(nameof(profession))
            };

            if (tier < 1 || tier > ranks.Length)
                throw new ArgumentOutOfRangeException(nameof(tier));

            return ranks[tier - 1];
        }

        public static int GetPracticeRankLimit(PerkType profession, int tier)
        {
            GetRequiredRank(profession, tier);
            return tier == 5 ? MaximumRank : GetRequiredRank(profession, tier + 1);
        }

        public static int CalculateXP(PerkType profession, int tier, int skillRank)
        {
            var requiredRank = GetRequiredRank(profession, tier);
            if (skillRank < requiredRank)
                return 0;

            return Skill.GetPracticeXP(requiredRank, skillRank, GetPracticeRankLimit(profession, tier));
        }

        public static bool CanUseTrapTier(int trapcraftRank, bool hasMasterSaboteur, int tier)
        {
            return tier is >= 1 and <= 4
                ? trapcraftRank >= tier
                : tier == 5 && hasMasterSaboteur;
        }
    }
}
