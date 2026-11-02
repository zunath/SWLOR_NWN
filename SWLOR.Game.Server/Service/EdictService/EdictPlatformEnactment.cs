using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.EdictService
{
    /// <summary>
    /// What <see cref="EdictChanges.EnactPlatform"/> did to the in-memory city records when an outright winner's platform
    /// replaced the city's Edicts.
    /// </summary>
    /// <param name="Enacted">The Edicts the city now holds from the platform, in Edict order, each with the name of the city it names.</param>
    /// <param name="Dropped">Promised Edicts that could not be enacted, each with the reason.</param>
    /// <param name="ReplacedCount">How many Edicts the city held before the platform replaced them.</param>
    /// <param name="FormedAlliances">Cities whose waiting Alliance took effect together with the platform's.</param>
    /// <param name="EndedAlliances">Cities whose Alliance with this city ended because the platform replaced it.</param>
    /// <param name="PartWeekAdded">The credits the first part-weeks added to the upkeep bill.</param>
    public sealed record EdictPlatformEnactment(
        IReadOnlyList<(EdictType Type, string TargetCityName)> Enacted,
        IReadOnlyList<(EdictType Type, EdictRefusal Refusal)> Dropped,
        int ReplacedCount,
        IReadOnlyList<WorldProperty> FormedAlliances,
        IReadOnlyList<WorldProperty> EndedAlliances,
        int PartWeekAdded)
    {
        /// <summary>The other cities' records that changed, so the caller saves them.</summary>
        public IReadOnlyList<WorldProperty> ChangedCities => FormedAlliances.Concat(EndedAlliances).Distinct().ToList();
    }
}
