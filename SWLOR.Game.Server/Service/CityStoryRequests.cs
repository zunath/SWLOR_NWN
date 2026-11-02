using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// A city's story request for the DMs (Player City Revamp, "Story hooks"). The mayor, an Acting Mayor, or an officer
    /// posts it in the City Affairs window; it goes to the DM feed, and a new one replaces the old. DMs mark it taken or
    /// done in the DM city window. The rules live in <see cref="StoryRequestRules"/>.
    /// </summary>
    public static class CityStoryRequests
    {
        /// <summary>
        /// Posts a request, replacing the city's old one, and sends it to the DM feed. Returns the reason it was refused, or
        /// empty on success.
        /// </summary>
        public static string Post(string cityId, string playerId, string text)
        {
            var city = LoadCity(cityId);
            if (city == null)
                return "This city no longer exists.";

            var role = CityPowers.GetRole(city, playerId);
            var error = StoryRequestRules.CheckPost(role, text);
            if (!string.IsNullOrEmpty(error))
                return error;

            // An officer must still be a citizen of the city, as for deciding situations.
            if (!CityOfficers.CanHandle(city, playerId))
                return "Only the mayor, an Acting Mayor, or an officer can post a story request.";

            var request = new CityStoryRequest
            {
                Text = StoryRequestRules.Normalize(text),
                PosterPlayerId = playerId,
                PosterTitle = CityOfficers.GetTitle(city, playerId),
                PostedUtc = DateTime.UtcNow,
                Status = CityStoryRequestStatus.Open
            };
            city.StoryRequest = request;
            DB.Set(city);

            var poster = DB.Get<Player>(playerId);
            CityGazette.PostStoryRequest(city, poster?.Name, request.PosterTitle, request.Text);
            Log.Write(LogGroup.Property, $"Player '{playerId}' ({request.PosterTitle}) posted a story request for city '{city.CustomName}' ({city.Id}).");
            return string.Empty;
        }

        /// <summary>A DM takes an open request. Returns the reason it was refused, or empty on success.</summary>
        public static string Take(string cityId)
        {
            var city = LoadCity(cityId);
            if (city == null)
                return "This city no longer exists.";

            if (!StoryRequestRules.CanTake(city.StoryRequest))
                return "That request is no longer open.";

            SetStatus(city, CityStoryRequestStatus.Taken);
            return string.Empty;
        }

        /// <summary>A DM marks a request done. Returns the reason it was refused, or empty on success.</summary>
        public static string Finish(string cityId)
        {
            var city = LoadCity(cityId);
            if (city == null)
                return "This city no longer exists.";

            if (!StoryRequestRules.CanFinish(city.StoryRequest))
                return "There is no open or taken request to finish.";

            SetStatus(city, CityStoryRequestStatus.Done);
            return string.Empty;
        }

        private static void SetStatus(WorldProperty city, CityStoryRequestStatus status)
        {
            city.StoryRequest.Status = status;
            city.StoryRequest.StatusChangedUtc = DateTime.UtcNow;
            DB.Set(city);
            Log.Write(LogGroup.Property, $"The story request of city '{city.CustomName}' ({city.Id}) is now {status}.");
        }

        private static WorldProperty LoadCity(string cityId)
        {
            var city = string.IsNullOrWhiteSpace(cityId) ? null : DB.Get<WorldProperty>(cityId);
            return city != null && city.PropertyType == PropertyType.City && !city.IsQueuedForDeletion
                ? city
                : null;
        }
    }
}
