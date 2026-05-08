using Newtonsoft.Json;

namespace SWLOR.Game.Server.Service.BackgroundJobService
{
    public class GitHubIssuePayload
    {
        [JsonProperty("repository")]
        public string Repository { get; set; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("body")]
        public string Body { get; set; } = string.Empty;
    }
}
