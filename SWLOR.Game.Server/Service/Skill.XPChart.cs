using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the skill catalog, XP awards and rank rules. The logic lives in <see cref="ISkillService"/>;
    /// the event handlers stay here and every member forwards to the container's instance.
    /// </summary>
    public static partial class Skill
    {
        /// <summary>
        /// When the module loads, cache all XP chart data used for quick access. The chart is a constant table
        /// built when <see cref="SkillXPChart"/> loads, so there is nothing left to build here.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheXPChartData()
        {
            Instance.CacheXPChartData();
        }

        /// <summary>
        /// Gets the amount of XP required to reach the next level.
        /// </summary>
        /// <param name="level">The level to use for the search.</param>
        /// <returns>The amount of XP required to reach the next level.</returns>
        public static int GetRequiredXP(int level)
        {
            return Instance.GetRequiredXP(level);
        }

        /// <summary>
        /// Gets the total amount of XP attained at this level, excluding the XP needed to reach the next level.
        /// </summary>
        /// <param name="level">The level to retrieve.</param>
        /// <returns>The total amount of XP attained at this level</returns>
        public static int GetTotalRequiredXP(int level)
        {
            return Instance.GetTotalRequiredXP(level);
        }

        /// <summary>
        /// Skill rank required to unlock a tier of an Espionage profession.
        /// </summary>
        public static int GetEspionageRequiredRank(PerkType profession, int tier)
        {
            return Instance.GetEspionageRequiredRank(profession, tier);
        }

        /// <summary>
        /// Exclusive training limit: the next tier's unlock, or the Espionage skill cap.
        /// </summary>
        public static int GetEspionagePracticeRankLimit(PerkType profession, int tier)
        {
            return Instance.GetEspionagePracticeRankLimit(profession, tier);
        }

        public static int CalculateEspionageXP(PerkType profession, int tier, int skillRank)
        {
            return Instance.CalculateEspionageXP(profession, tier, skillRank);
        }

        /// <summary>
        /// Keeps a tiered activity useful until its next unlock, with a quarter of same-level XP as the floor.
        /// </summary>
        public static int GetPracticeXP(int activityLevel, int skillRank, int rankLimit)
        {
            return Instance.GetPracticeXP(activityLevel, skillRank, rankLimit);
        }

        /// <summary>
        /// Retrieves XP for the target level minus the player's rank. Trivial activities return zero.
        /// </summary>
        public static int GetDeltaXP(int delta)
        {
            return Instance.GetDeltaXP(delta);
        }
    }
}
