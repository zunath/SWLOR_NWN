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
                var context = BuildLogContext(payload);
                var entries = new[]
                {
                    new NameValueEntry("type", type.ToString()),
                    new NameValueEntry("payload", JsonConvert.SerializeObject(payload)),
                    new NameValueEntry("createdUtc", DateTime.UtcNow.ToString("O"))
                };

                _ = Task.Run(() => EnqueueAsync(type, entries, context));

                return true;
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Failed to schedule background job enqueue. Type='{type}'. Context='{BuildLogContext(payload)}'. {ex}");
                return false;
            }
        }

        private static async Task EnqueueAsync(BackgroundJobType type, NameValueEntry[] entries, string context)
        {
            try
            {
                await DB.StreamAddAsync(
                    StreamName,
                    entries,
                    maxLength: MaxStreamLength,
                    useApproximateMaxLength: true);
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Failed to enqueue background job. Type='{type}'. Context='{context}'. {ex}");
            }
        }

        private static string BuildLogContext<TPayload>(TPayload payload)
        {
            switch (payload)
            {
                case DiscordWebhookPayload discord:
                    return $"authorName='{Truncate(discord.AuthorName)}', title='{Truncate(discord.Title)}', description='{Truncate(discord.Description)}', threadId='{Truncate(discord.ThreadId)}', createThread='{discord.CreateThread}'";
                case GitHubIssuePayload gitHub:
                    return $"repository='{Truncate(gitHub.Repository)}', title='{Truncate(gitHub.Title)}'";
                default:
                    return payload?.GetType().Name ?? "null";
            }
        }

        private static string Truncate(string value)
        {
            const int MaxLogValueLength = 200;

            if (string.IsNullOrEmpty(value) || value.Length <= MaxLogValueLength)
            {
                return value;
            }

            return value.Substring(0, MaxLogValueLength) + "...";
        }
    }
}
