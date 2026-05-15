using StackExchange.Redis;
using SWLOR.BackgroundServices.Configuration;
using SWLOR.BackgroundServices.Infrastructure;

namespace SWLOR.BackgroundServices.BackgroundJobs
{
    public sealed class BackgroundJobFailureHandler
    {
        private const string RequeueAndAcknowledgeScript = @"
redis.call('XADD', KEYS[1], 'MAXLEN', '~', ARGV[1], '*',
    'type', ARGV[4],
    'payload', ARGV[5],
    'attempt', ARGV[6],
    'createdUtc', ARGV[7],
    'lastError', ARGV[8])
return redis.call('XACK', KEYS[1], ARGV[2], ARGV[3])
";

        private const string MoveToDeadLetterAndAcknowledgeScript = @"
redis.call('XADD', KEYS[1], '*',
    'originalId', ARGV[2],
    'error', ARGV[3],
    'failedUtc', ARGV[4],
    unpack(ARGV, 5))
return redis.call('XACK', KEYS[2], ARGV[1], ARGV[2])
";

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

            await RequeueAndAcknowledgeAsync(database, job, nextAttempt, exception.ToString());
            _logger.Error($"Background job {job.Id} failed attempt {nextAttempt}; requeued. {exception.Message}");
        }

        public async Task MoveToDeadLetterAsync(IDatabase database, StreamEntry entry, string error)
        {
            var arguments = new List<RedisValue>
            {
                BackgroundJobQueueNames.ConsumerGroup,
                entry.Id,
                Truncate(error),
                DateTime.UtcNow.ToString("O")
            };

            foreach (var value in entry.Values)
            {
                arguments.Add(value.Name);
                arguments.Add(value.Value);
            }

            await database.ScriptEvaluateAsync(
                MoveToDeadLetterAndAcknowledgeScript,
                new RedisKey[]
                {
                    BackgroundJobQueueNames.DeadLetterStreamName,
                    BackgroundJobQueueNames.StreamName
                },
                arguments.ToArray());
        }

        private async Task RequeueAndAcknowledgeAsync(
            IDatabase database,
            BackgroundJob job,
            int nextAttempt,
            string error)
        {
            await database.ScriptEvaluateAsync(
                RequeueAndAcknowledgeScript,
                new RedisKey[]
                {
                    BackgroundJobQueueNames.StreamName
                },
                new RedisValue[]
                {
                    BackgroundJobQueueNames.MaxStreamLength,
                    BackgroundJobQueueNames.ConsumerGroup,
                    job.Id,
                    job.Type,
                    job.Payload,
                    nextAttempt.ToString(),
                    DateTime.UtcNow.ToString("O"),
                    Truncate(error)
                });
        }

        private string Truncate(string value)
        {
            return value.Length <= _settings.MaxLogContentLength
                ? value
                : value.Substring(0, _settings.MaxLogContentLength) + "...";
        }
    }
}
