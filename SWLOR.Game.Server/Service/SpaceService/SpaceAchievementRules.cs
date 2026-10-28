using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.AchievementService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Which space achievement a milestone earns (plan section 14), as pure functions. The ids and texts are generated
    /// by tools/SpaceRevampBalance (rewards.py); the handlers live in Space (SpaceAchievements.cs) and Trade.
    /// </summary>
    public static class SpaceAchievementRules
    {
        private static readonly AchievementType[] _tierKills =
        {
            AchievementType.SpaceKillTier1, AchievementType.SpaceKillTier2, AchievementType.SpaceKillTier3,
            AchievementType.SpaceKillTier4, AchievementType.SpaceKillTier5, AchievementType.SpaceKillTier6,
        };

        /// <summary>The trade earnings milestones in credits, lowest first.</summary>
        public static readonly IReadOnlyList<(ulong Credits, AchievementType Achievement)> TradeMilestones = new[]
        {
            (10000UL, AchievementType.SpaceTradeEarnings1),
            (100000UL, AchievementType.SpaceTradeEarnings2),
            (1000000UL, AchievementType.SpaceTradeEarnings3),
        };

        /// <summary>The first-kill achievement of an NPC ship's faction tier (1-6), or Invalid.</summary>
        public static AchievementType TierKill(int tier) =>
            tier >= 1 && tier <= _tierKills.Length ? _tierKills[tier - 1] : AchievementType.Invalid;

        /// <summary>The achievement for jumping to a signature of this type, or Invalid.</summary>
        public static AchievementType Site(SignatureType type) => type switch
        {
            SignatureType.CombatSite => AchievementType.SpaceSiteCombat,
            SignatureType.DataSite => AchievementType.SpaceSiteData,
            SignatureType.CacheSite => AchievementType.SpaceSiteCache,
            SignatureType.GasSite => AchievementType.SpaceSiteGas,
            SignatureType.Derelict => AchievementType.SpaceSiteDerelict,
            SignatureType.Rift => AchievementType.SpaceSiteRift,
            _ => AchievementType.Invalid,
        };

        /// <summary>The trade achievements a running total of credits earned from trade has reached.</summary>
        public static List<AchievementType> TradeEarnings(ulong total) =>
            TradeMilestones.Where(x => total >= x.Credits).Select(x => x.Achievement).ToList();

        /// <summary>Whether a fitted module earns the named module achievement: a named rank-50 module.</summary>
        public static bool IsNamedModule(ShipModuleStep step) => step != null && step.Named;
    }
}
