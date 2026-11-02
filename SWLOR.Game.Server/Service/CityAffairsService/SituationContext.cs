using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// What a situation's requirements are checked against: the city, and the deciding official when a choice is checked.
    /// </summary>
    public sealed class SituationContext
    {
        public int CityLevel { get; init; } = 1;

        public bool IsStation { get; init; }

        public PlanetType Planet { get; init; }

        /// <summary>The effective level of each facility the city has. A facility the city lacks has no entry.</summary>
        public IReadOnlyDictionary<PropertyUpgradeType, int> FacilityLevels { get; init; } = new Dictionary<PropertyUpgradeType, int>();

        /// <summary>The types of structure the city has, including the garden plots inside its houses.</summary>
        public IReadOnlyCollection<StructureType> StructureTypes { get; init; } = new List<StructureType>();

        /// <summary>The names of the Edicts in force.</summary>
        public IReadOnlyCollection<string> ActiveEdicts { get; init; } = new List<string>();

        /// <summary>The deciding official's skill ranks. Empty when no official is being checked.</summary>
        public IReadOnlyDictionary<SkillType, int> SkillRanks { get; init; } = new Dictionary<SkillType, int>();
    }
}
