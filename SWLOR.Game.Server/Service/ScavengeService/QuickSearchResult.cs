using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;

namespace SWLOR.Game.Server.Service.ScavengeService
{
    /// <summary>What a Quick Search came to.</summary>
    /// <param name="Attempts">The attempts made, one or two.</param>
    public sealed record QuickSearchResult(IReadOnlyList<QuickSearchAttempt> Attempts)
    {
        public int Successes => Attempts.Count(attempt => attempt.Success);

        public int TotalXp => Attempts.Sum(attempt => attempt.Xp);

        /// <summary>The time it takes: as long as today's search, one <see cref="SearchRules.QuickSearchSeconds"/> animation for the whole visit however many attempts it made.</summary>
        public double Seconds => SearchRules.QuickSearchSeconds;
    }
}
