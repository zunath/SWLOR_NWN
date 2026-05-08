using StackExchange.Redis;
using SWLOR.BackgroundServices.Configuration;
using SWLOR.BackgroundServices.Infrastructure;

namespace SWLOR.BackgroundServices.BackgroundJobs
{
    public sealed class BackgroundJobFailureHandler
    {
        private readonly BackgroundServiceSettings _settings;
        private readonly IAppLogger _logger;

        public BackgroundJobFailureHandler(BackgroundServiceSettings settings, IAppLogger logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public async Task HandleFailureAsync(IDatabase database, BackgroundJob job, Exception exception)
        {
            var nextAttempt = job.Attempt + 1;
            if (nextAttempt >= _settings.MaxAttempts)
            {
                await MoveToDeadLetterAsync(database, job.Entry, exception.ToString());
                _logger.Error($"Background job {job.Id} failed permanently after {nextAttempt} attempts: {exception.Message}");
                return;
            }

            await database.StreamAddAsync(
                BackgroundJobQueueNames.StreamName,
                new[]
                {
                    new NameValueEntry("type", job.Type),
                    new NameValueEntry("payload", job.Payload),
                    new NameValueEntry("attempt", nextAttempt.ToString()),
                    new NameValueEntry("createdUtc", DateTime.UtcNow.ToString("O")),
                    new NameValueEntry("lastError", Truncate(exception.ToString()))
                });

            await AcknowledgeAsync(database, job.Id);
            _logger.Error($"Background job {job.Id} failed attempt {nextAttempt}; requeued. {exception.Message}");
        }

        public async Task MoveToDeadLetterAsync(IDatabase database, StreamEntry entry, string error)
        {
            await database.StreamAddAsync(
                BackgroundJobQueueNames.DeadLetterStreamName,
                new[]
                {
                    new NameValueEntry("originalId", entry.Id),
                    new NameValueEntry("error", Truncate(error)),
                    new NameValueEntry("failedUtc", DateTime.UtcNow.ToString("O"))
                }.Concat(entry.Values).ToArray());

            await AcknowledgeAsync(database, entry.Id);
        }

        private async Task AcknowledgeAsync(IDatabase database, RedisValue id)
        {
            await database.StreamAcknowledgeAsync(
                BackgroundJobQueueNames.StreamName,
                BackgroundJobQueueNames.ConsumerGroup,
                id);
        }

        private string Truncate(string value)
        {
            return value.Length <= _settings.MaxLogContentLength
                ? value
                : value.Substring(0, _settings.MaxLogContentLength) + "...";
        }
    }
}
