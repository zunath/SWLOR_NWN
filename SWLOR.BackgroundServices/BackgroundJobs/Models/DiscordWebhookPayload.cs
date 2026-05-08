using Newtonsoft.Json;

namespace SWLOR.BackgroundServices.BackgroundJobs.Models
{
    public sealed class DiscordWebhookPayload
    {
        [JsonProperty("webhookUrl")]
        public string WebhookUrl { get; set; } = string.Empty;

        [JsonProperty("authorName")]
        public string AuthorName { get; set; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("color")]
        public int Color { get; set; }

        [JsonProperty("threadId")]
        public string ThreadId { get; set; } = string.Empty;

        [JsonProperty("createThread")]
        public bool CreateThread { get; set; }

        [JsonProperty("threadName")]
        public string ThreadName { get; set; } = string.Empty;
    }
}
