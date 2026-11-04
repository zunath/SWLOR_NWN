using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Reads every city for the DM city window: who runs it, how many citizens logged in during the last week, its active
    /// Edicts, its recent Gazette entries, and its story request. This is a staff surface, so it shows character names.
    /// </summary>
    public static class DMCityDirectory
    {
        /// <summary>Every city and station, with the open story requests first.</summary>
        public static List<DMCitySummary> GetCities()
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.City)
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false);
            var count = (int)DB.SearchCount(query);
            if (count <= 0)
                return new List<DMCitySummary>();

            var now = DateTime.UtcNow;
            var cities = DB.Search(query.AddPaging(count, 0))
                .Select(city => Summarize(city, now))
                .ToList();
            return DMCityRules.Sort(cities);
        }

        private static DMCitySummary Summarize(WorldProperty city, DateTime now)
        {
            var citizens = LoadCitizens(city.Id);
            var councilPlanet = PlanetaryCouncils.GetCouncilPlanet(city);
            var steppedDown = StepDownRules.HasSteppedDown(city.StepDown);
            var actingId = steppedDown
                ? city.StepDown.InterimHolderPlayerId
                : ActingMayorRules.IsActive(city.ActingMayorPlayerId, city.ActingMayorEndsUtc, now)
                    ? city.ActingMayorPlayerId
                    : string.Empty;

            return new DMCitySummary
            {
                CityId = city.Id,
                CityName = city.CustomName,
                Planet = councilPlanet,
                PlanetName = PlanetaryCouncils.GetPlanetName(councilPlanet),
                IsStation = Station.IsStation(city),
                Level = CityOfficers.GetCityLevel(city),
                MayorName = GetCharacterName(city.OwnerPlayerId),
                MayorSteppedDown = steppedDown,
                ActingMayorName = string.IsNullOrWhiteSpace(actingId) ? string.Empty : GetCharacterName(actingId),
                OfficerLines = city.Officers
                    .OrderBy(pair => pair.Value.AppointedUtc)
                    .Select(pair => DMCityRules.BuildPersonLine(GetCharacterName(pair.Key), pair.Value.Title, IsOnline(pair.Key)))
                    .ToList(),
                CitizenCount = citizens.Count,
                ActiveCitizenCount = DMCityRules.CountActiveCitizens(citizens.Select(citizen => citizen.LastLoginDate), now),
                EdictNames = EdictChanges.GetInEffectTypes(city, now).Select(EdictRules.GetName).ToList(),
                GazetteLines = CityGazette.GetEntries(city.Id, true)
                    .Take(DMCityRules.RecentGazetteEntries)
                    .Select(DMCityRules.BuildGazetteLine)
                    .ToList(),
                StoryRequest = city.StoryRequest,
                StoryPosterName = city.StoryRequest == null ? string.Empty : GetCharacterName(city.StoryRequest.PosterPlayerId)
            };
        }

        private static List<Player> LoadCitizens(string cityId)
        {
            var query = new DBQuery<Player>()
                .AddFieldSearch(nameof(Player.CitizenPropertyId), cityId, false)
                .AddFieldSearch(nameof(Player.IsDeleted), false);
            var count = (int)DB.SearchCount(query);
            return count <= 0
                ? new List<Player>()
                : DB.Search(query.AddPaging(count, 0)).ToList();
        }

        // Staff see the character's own name.
        private static string GetCharacterName(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return "(nobody)";

            return DB.Get<Player>(playerId)?.Name ?? "(unknown)";
        }

        private static bool IsOnline(string playerId)
        {
            return GetIsObjectValid(DMPlayerDirectory.GetOnlinePlayer(playerId));
        }
    }
}
