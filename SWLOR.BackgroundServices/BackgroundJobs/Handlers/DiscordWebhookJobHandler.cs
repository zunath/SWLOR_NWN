using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            var includeThreadName = string.IsNullOrWhiteSpace(job.ThreadId) && !string.IsNullOrWhiteSpace(threadName);

            for (var attempt = 1; attempt <= 5; attempt++)
            {
                using var request = CreateRequest(job, includeThreadName ? threadName : string.Empty);
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                if (includeThreadName && IsThreadCreationUnsupported(responseBody))
                {
                    includeThreadName = false;
                    continue;
                }

                if ((int)response.StatusCode == 429 && attempt < 5)
                {
                    await Task.Delay(GetRetryAfter(responseBody), cancellationToken);
                    continue;
                }

                throw new InvalidOperationException($"Discord webhook failed: {(int)response.StatusCode} {response.StatusCode}. {responseBody}");
            }

            throw new InvalidOperationException("Discord webhook failed after all retry attempts.");
        }

        private static HttpRequestMessage CreateRequest(DiscordWebhookPayload job, string threadName)
        {
            return new HttpRequestMessage(HttpMethod.Post, BuildWebhookUrl(job))
            {
                Content = JsonHttpContent.Create(new
                {
                    thread_name = string.IsNullOrWhiteSpace(threadName)
                        ? null
                        : threadName,
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
            if (!job.CreateThread)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(job.ThreadName))
            {
                return job.ThreadName;
            }

            if (!string.IsNullOrWhiteSpace(job.Title))
            {
                return job.Title;
            }

            return string.Empty;
        }

        private static bool IsThreadCreationUnsupported(string responseBody)
        {
            try
            {
                return JObject.Parse(responseBody)["code"]?.Value<int>() == 220003;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static TimeSpan GetRetryAfter(string responseBody)
        {
            try
            {
                var retryAfter = JObject.Parse(responseBody)["retry_after"]?.Value<double>() ?? 1.0;
                return TimeSpan.FromMilliseconds(retryAfter * 1000 + 100);
            }
            catch (JsonException)
            {
                return TimeSpan.FromSeconds(1);
            }
        }
    }
}
