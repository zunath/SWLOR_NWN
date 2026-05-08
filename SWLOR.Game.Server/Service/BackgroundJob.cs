using System;
using System.Threading.Tasks;
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

        public static Task<bool> EnqueueGitHubIssue(string repository, string title, string body)
        {
            var payload = new GitHubIssuePayload
            {
                Repository = repository,
                Title = title,
                Body = body
            };

            return Enqueue(BackgroundJobType.GitHubIssue, payload);
        }

        public static Task<bool> EnqueueDiscordWebhook(
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

        private static async Task<bool> Enqueue<TPayload>(BackgroundJobType type, TPayload payload)
        {
            try
            {
                var context = BuildLogContext(payload);
                var entries = new[]
                {
                    new NameValueEntry("type", type.ToString()),
                    new NameValueEntry("payload", JsonConvert.SerializeObject(payload)),
                    new NameValueEntry("createdUtc", DateTime.UtcNow.ToString("O"))
                };

                return await EnqueueAsync(type, entries, context);
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Failed to enqueue background job. Type='{type}'. Context='{BuildLogContext(payload)}'. {ex}");
                return false;
            }
        }

        private static async Task<bool> EnqueueAsync(BackgroundJobType type, NameValueEntry[] entries, string context)
        {
            try
            {
                await DB.StreamAddAsync(
                    StreamName,
                    entries,
                    maxLength: MaxStreamLength,
                    useApproximateMaxLength: true);
                return true;
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Failed to enqueue background job. Type='{type}'. Context='{context}'. {ex}");
                return false;
            }
        }

        private static string BuildLogContext<TPayload>(TPayload payload)
        {
            switch (payload)
            {
                case DiscordWebhookPayload discord:
                    return $"threadId='{discord.ThreadId}', createThread='{discord.CreateThread}'";
                case GitHubIssuePayload gitHub:
                    return $"repository='{gitHub.Repository}'";
                default:
                    return payload?.GetType().Name ?? "null";
            }
        }
    }
}
