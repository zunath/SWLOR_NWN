using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BackgroundJobService;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// Background jobs: the logic that used to be static in <c>Service.BackgroundJob</c>, moved here unchanged. The
    /// facade repeats the two constants and forwards every call to the container's instance.
    /// </summary>
    public class BackgroundJobService : IBackgroundJobService
    {
        public const string StreamName = "swlor:background-jobs";
        public const int MaxStreamLength = 10000;

        private readonly IDBService _db;

        public BackgroundJobService(IDBService db)
        {
            _db = db;
        }

        public Task<bool> EnqueueGitHubIssue(string repository, string title, string body)
        {
            var payload = new GitHubIssuePayload
            {
                Repository = repository,
                Title = title,
                Body = body
            };

            return Enqueue(BackgroundJobType.GitHubIssue, payload);
        }

        public Task<bool> EnqueueDiscordWebhook(
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

        private async Task<bool> Enqueue<TPayload>(BackgroundJobType type, TPayload payload)
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
                WriteLog(LogGroup.Error, $"Failed to enqueue background job. Type='{type}'. Context='{BuildLogContext(payload)}'. {ex}");
                return false;
            }
        }

        private async Task<bool> EnqueueAsync(BackgroundJobType type, NameValueEntry[] entries, string context)
        {
            try
            {
                await _db.StreamAddAsync(
                    StreamName,
                    entries,
                    maxLength: MaxStreamLength,
                    useApproximateMaxLength: true);
                return true;
            }
            catch (Exception ex)
            {
                WriteLog(LogGroup.Error, $"Failed to enqueue background job. Type='{type}'. Context='{context}'. {ex}");
                return false;
            }
        }

        private string BuildLogContext<TPayload>(TPayload payload)
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

        /// <summary>
        /// Writes to the server log. One line so a test can see what was logged; the body is the original
        /// <c>Log.Write</c> call (<c>Log</c> stays static in Phase 2).
        /// </summary>
        protected virtual void WriteLog(LogGroup group, string details, bool printToConsole = false)
        {
            Log.Write(group, details, printToConsole);
        }
    }
}
