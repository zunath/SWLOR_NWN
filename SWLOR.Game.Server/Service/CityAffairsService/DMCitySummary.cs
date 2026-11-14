using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// One city as the DM city window shows it. The window is staff only, so it carries character names.
    /// </summary>
    public sealed class DMCitySummary
    {
        public string CityId { get; init; } = string.Empty;

        public string CityName { get; init; } = string.Empty;

        /// <summary>The planet whose council the city joins. Invalid for a city on a planet the game does not know.</summary>
        public PlanetType Planet { get; init; }

        public string PlanetName { get; init; } = string.Empty;

        public bool IsStation { get; init; }

        public int Level { get; init; }

        public string MayorName { get; init; } = string.Empty;

        /// <summary>The mayor stepped down, so the mayor of record has no powers until the snap election ends.</summary>
        public bool MayorSteppedDown { get; init; }

        /// <summary>The running Acting Mayor, or the interim holder after a step-down. Empty when there is none.</summary>
        public string ActingMayorName { get; init; } = string.Empty;

        public IReadOnlyList<string> OfficerLines { get; init; } = new List<string>();

        public int CitizenCount { get; init; }

        /// <summary>Citizens whose last login is within <see cref="DMCityRules.ActiveWindowDays"/> days.</summary>
        public int ActiveCitizenCount { get; init; }

        public IReadOnlyList<string> EdictNames { get; init; } = new List<string>();

        public IReadOnlyList<string> GazetteLines { get; init; } = new List<string>();

        public CityStoryRequest StoryRequest { get; init; }

        public string StoryPosterName { get; init; } = string.Empty;
    }
}
