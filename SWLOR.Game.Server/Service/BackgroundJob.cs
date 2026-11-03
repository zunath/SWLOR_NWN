using System.Threading.Tasks;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Core;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the background job queue. The logic lives in <see cref="IBackgroundJobService"/>; every
    /// member here forwards to the container's instance.
    /// </summary>
    public static class BackgroundJob
    {
        public const string StreamName = Domain.Core.BackgroundJobService.StreamName;
        public const int MaxStreamLength = Domain.Core.BackgroundJobService.MaxStreamLength;

        private static IBackgroundJobService Instance => FacadeAccessor.Resolve<IBackgroundJobService>();

        public static Task<bool> EnqueueGitHubIssue(string repository, string title, string body)
        {
            return Instance.EnqueueGitHubIssue(repository, title, body);
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
            return Instance.EnqueueDiscordWebhook(
                webhookUrl,
                authorName,
                description,
                color,
                title,
                threadId,
                createThread,
                threadName);
        }
    }
}
