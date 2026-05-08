using Newtonsoft.Json;
using SWLOR.BackgroundServices.BackgroundJobs.Models;
using SWLOR.BackgroundServices.Infrastructure;

namespace SWLOR.BackgroundServices.BackgroundJobs.Handlers
{
    public sealed class DiscordWebhookJobHandler : IBackgroundJobHandler
    {
        private readonly HttpClient _httpClient;

        public DiscordWebhookJobHandler(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task HandleAsync(string payload, CancellationToken cancellationToken)
        {
            var job = JsonConvert.DeserializeObject<DiscordWebhookPayload>(payload)
                      ?? throw new InvalidOperationException("Unable to deserialize Discord webhook payload.");
            var threadName = ResolveThreadName(job);

            using var request = new HttpRequestMessage(HttpMethod.Post, BuildWebhookUrl(job))
            {
                Content = JsonHttpContent.Create(new
                {
                    thread_name = string.IsNullOrWhiteSpace(job.ThreadId) && !string.IsNullOrWhiteSpace(threadName)
                        ? threadName
                        : null,
                    embeds = new[]
                    {
                        new
                        {
                            author = new
                            {
                                name = job.AuthorName
                            },
                            title = string.IsNullOrWhiteSpace(job.Title)
                                ? null
                                : job.Title,
                            description = job.Description,
                            color = job.Color
                        }
                    }
                })
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Discord webhook failed: {(int)response.StatusCode} {response.StatusCode}. {responseBody}");
            }
        }

        private static string BuildWebhookUrl(DiscordWebhookPayload job)
        {
            if (string.IsNullOrWhiteSpace(job.ThreadId))
            {
                return job.WebhookUrl;
            }

            return AddQueryString(job.WebhookUrl, "thread_id", job.ThreadId);
        }

        private static string AddQueryString(string url, string name, string value)
        {
            var separator = url.Contains('?')
                ? "&"
                : "?";

            return $"{url}{separator}{name}={Uri.EscapeDataString(value)}";
        }

        private static string ResolveThreadName(DiscordWebhookPayload job)
        {
            if (!string.IsNullOrWhiteSpace(job.ThreadName))
            {
                return job.ThreadName;
            }

            if (job.CreateThread || !string.IsNullOrWhiteSpace(job.Title))
            {
                return job.Title;
            }

            return string.Empty;
        }
    }
}
