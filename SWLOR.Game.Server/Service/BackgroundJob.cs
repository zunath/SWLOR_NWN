using System;
using Newtonsoft.Json;
using StackExchange.Redis;
using SWLOR.Game.Server.Service.BackgroundJobService;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Service
{
    public static class BackgroundJob
    {
        public const string StreamName = "swlor:background-jobs";
        public const int MaxStreamLength = 10000;

        public static bool EnqueueGitHubIssue(string repository, string title, string body)
        {
            var payload = new GitHubIssuePayload
            {
                Repository = repository,
                Title = title,
                Body = body
            };

            return Enqueue(BackgroundJobType.GitHubIssue, payload);
        }

        public static bool EnqueueDiscordWebhook(
            string webhookUrl,
            string authorName,
            string description,
            int color,
            string title = "",
            string threadId = "",
            bool createThread = false,
            string threadName = "")
        {
            var payload = new DiscordWebhookPayload
            {
                WebhookUrl = webhookUrl,
                AuthorName = authorName,
                Title = title,
                Description = description,
                Color = color,
                ThreadId = threadId,
                CreateThread = createThread,
                ThreadName = threadName
            };

            return Enqueue(BackgroundJobType.DiscordWebhook, payload);
        }

        private static bool Enqueue<TPayload>(BackgroundJobType type, TPayload payload)
        {
            try
            {
                DB.StreamAdd(
                    StreamName,
                    new[]
                    {
                        new NameValueEntry("type", type.ToString()),
                        new NameValueEntry("payload", JsonConvert.SerializeObject(payload)),
                        new NameValueEntry("createdUtc", DateTime.UtcNow.ToString("O"))
                    },
                    maxLength: MaxStreamLength,
                    useApproximateMaxLength: true);

                return true;
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Failed to enqueue background job '{type}'. {ex}");
                return false;
            }
        }
    }
}
