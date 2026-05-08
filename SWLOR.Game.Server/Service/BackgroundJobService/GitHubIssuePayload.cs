using Newtonsoft.Json;

namespace SWLOR.Game.Server.Service.BackgroundJobService
{
    public class GitHubIssuePayload
    {
        [JsonProperty("repository")]
        public string Repository { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }
    }
}
