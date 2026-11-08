using System.Threading.Tasks;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// Queues jobs for the background worker process on the Redis stream it reads. Static callers reach the
    /// container's instance through <c>GameServices.BackgroundJob</c>.
    /// </summary>
    public interface IBackgroundJobService
    {
        /// <summary>Queues a job that opens a GitHub issue. Resolves to false when the job could not be queued.</summary>
        Task<bool> EnqueueGitHubIssue(string repository, string title, string body);

        /// <summary>Queues a job that posts a Discord webhook message. Resolves to false when the job could not be queued.</summary>
        Task<bool> EnqueueDiscordWebhook(
            string webhookUrl,
            string authorName,
            string description,
            int color,
            string title = "",
            string threadId = "",
            bool createThread = false,
            string threadName = "");
    }
}
