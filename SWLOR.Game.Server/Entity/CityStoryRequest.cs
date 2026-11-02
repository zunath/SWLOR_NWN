using SWLOR.Game.Server.Service.CityAffairsService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// The one open story request a city has for the DMs (Player City Revamp, "Story hooks"): an invitation for a plot or a
    /// hint about ruins near the city. A new request replaces the old one.
    /// </summary>
    public class CityStoryRequest
    {
        public CityStoryRequest()
        {
            Text = string.Empty;
            PosterPlayerId = string.Empty;
            PosterTitle = string.Empty;
            Status = CityStoryRequestStatus.Open;
        }

        /// <summary>What the city wants the DMs to know, up to <see cref="StoryRequestRules.MaxLength"/> characters.</summary>
        public string Text { get; set; }

        /// <summary>The mayor, Acting Mayor, or officer who posted it.</summary>
        public string PosterPlayerId { get; set; }

        /// <summary>The title they posted under, such as Mayor.</summary>
        public string PosterTitle { get; set; }

        public DateTime PostedUtc { get; set; }

        public CityStoryRequestStatus Status { get; set; }

        /// <summary>When a DM last changed the status, or null while it is still open.</summary>
        public DateTime? StatusChangedUtc { get; set; }
    }
}
