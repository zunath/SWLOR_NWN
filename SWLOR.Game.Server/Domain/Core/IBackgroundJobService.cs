using System.Threading.Tasks;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// Queues jobs for the background worker process on the Redis stream it reads. The static facade
    /// <c>Service.BackgroundJob</c> forwards to the container's instance of this interface.
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
