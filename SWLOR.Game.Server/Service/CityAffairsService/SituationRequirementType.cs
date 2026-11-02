namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>What a <see cref="SituationRequirement"/> asks of the city or the official.</summary>
    public enum SituationRequirementType
    {
        Invalid = 0,
        CityLevel = 1,
        Planet = 2,

        /// <summary>The city stands on a planet, not on a station.</summary>
        PlanetCity = 3,
        Facility = 4,
        ActiveEdict = 5,
        SkillRank = 6,

        /// <summary>The city has a structure of the type, such as a corral or a garden plot.</summary>
        Structure = 7
    }
}
