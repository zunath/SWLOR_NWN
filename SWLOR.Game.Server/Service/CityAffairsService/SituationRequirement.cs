using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// A condition on the city or the deciding official. On a situation it decides whether the situation can be drawn;
    /// on a choice it decides whether the choice can be picked.
    /// </summary>
    public sealed class SituationRequirement
    {
        private SituationRequirement(SituationRequirementType type)
        {
            Type = type;
            EdictName = string.Empty;
        }

        public SituationRequirementType Type { get; }

        /// <summary>The city level, facility level, or skill rank asked for.</summary>
        public int Level { get; private init; }

        /// <summary>The planets the city may stand on. Any one of them satisfies the requirement.</summary>
        public PlanetType Planets { get; private init; }

        public PropertyUpgradeType Facility { get; private init; }

        /// <summary>The name of an Edict (an <c>EdictType</c> member) that must be in force.</summary>
        public string EdictName { get; private init; }

        public SkillType Skill { get; private init; }

        public StructureType Structure { get; private init; }

        /// <summary>The name of a traveler (a <c>TravelerType</c> member) the city must host this week.</summary>
        public string TravelerName { get; private init; } = string.Empty;

        public static SituationRequirement CityLevel(int level)
        {
            return new SituationRequirement(SituationRequirementType.CityLevel) { Level = level };
        }

        public static SituationRequirement OnPlanet(PlanetType planets)
        {
            return new SituationRequirement(SituationRequirementType.Planet) { Planets = planets };
        }

        public static SituationRequirement PlanetCity()
        {
            return new SituationRequirement(SituationRequirementType.PlanetCity);
        }

        /// <summary>A facility of the given upgrade type, at the given level or higher.</summary>
        public static SituationRequirement WithFacility(PropertyUpgradeType facility, int level = 1)
        {
            return new SituationRequirement(SituationRequirementType.Facility) { Facility = facility, Level = level };
        }

        public static SituationRequirement ActiveEdict(string edictName)
        {
            return new SituationRequirement(SituationRequirementType.ActiveEdict) { EdictName = edictName ?? string.Empty };
        }

        /// <summary>The city has at least one structure of the type.</summary>
        public static SituationRequirement WithStructure(StructureType structure)
        {
            return new SituationRequirement(SituationRequirementType.Structure) { Structure = structure };
        }

        /// <summary>The traveler is in the city this week. The name is a <c>TravelerType</c> member, such as "BeastTrader".</summary>
        public static SituationRequirement TravelerHost(string travelerName)
        {
            return new SituationRequirement(SituationRequirementType.TravelerHost) { TravelerName = travelerName ?? string.Empty };
        }

        public static SituationRequirement SkillRank(SkillType skill, int rank)
        {
            return new SituationRequirement(SituationRequirementType.SkillRank) { Skill = skill, Level = rank };
        }
    }
}
