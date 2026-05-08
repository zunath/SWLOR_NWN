using Newtonsoft.Json;

namespace SWLOR.Game.Server.Service.BackgroundJobService
{
    public class DiscordWebhookPayload
    {
        [JsonProperty("webhookUrl")]
        public string WebhookUrl { get; set; }

        [JsonProperty("authorName")]
        public string AuthorName { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("color")]
        public int Color { get; set; }

        [JsonProperty("threadId")]
        public string ThreadId { get; set; }

        [JsonProperty("createThread")]
        public bool CreateThread { get; set; }

        [JsonProperty("threadName")]
        public string ThreadName { get; set; }
    }
}
