using StackExchange.Redis;
using SWLOR.BackgroundServices.Configuration;
using SWLOR.BackgroundServices.Infrastructure;

namespace SWLOR.BackgroundServices.BackgroundJobs
{
    public sealed class BackgroundJobWorker
    {
        private readonly BackgroundServiceSettings _settings;
        private readonly BackgroundJobProcessor _processor;
        private readonly IAppLogger _logger;

        public BackgroundJobWorker(
            BackgroundServiceSettings settings,
            BackgroundJobProcessor processor,
            IAppLogger logger)
        {
            _settings = settings;
            _processor = processor;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            _logger.Info($"Connecting to Redis at {_settings.RedisConnection}...");
            using var redis = await ConnectionMultiplexer.ConnectAsync(_settings.RedisConnection);
            var database = redis.GetDatabase();
            await EnsureConsumerGroup(database);

            _logger.Info($"Background service '{_settings.ConsumerName}' listening on Redis Stream '{BackgroundJobQueueNames.StreamName}'.");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var entries = await ReadPendingOrNew(database);

                    if (entries.Length == 0)
                    {
                        await Task.Delay(_settings.EmptyReadDelay, cancellationToken);
                        continue;
                    }

                    foreach (var entry in entries)
                    {
                        await _processor.ProcessAsync(database, entry, cancellationToken);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error("Worker loop failed.", ex);
                    await Task.Delay(_settings.FailureDelay, cancellationToken);
                }
            }
        }

        private static async Task EnsureConsumerGroup(IDatabase database)
        {
            try
            {
                await database.StreamCreateConsumerGroupAsync(
                    BackgroundJobQueueNames.StreamName,
                    BackgroundJobQueueNames.ConsumerGroup,
                    "0-0",
                    true);
            }
            catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP", StringComparison.OrdinalIgnoreCase))
            {
                // Group already exists.
            }
        }

        private async Task<StreamEntry[]> ReadPendingOrNew(IDatabase database)
        {
            var pending = await database.StreamReadGroupAsync(
                BackgroundJobQueueNames.StreamName,
                BackgroundJobQueueNames.ConsumerGroup,
                _settings.ConsumerName,
                "0-0",
                _settings.BatchSize);

            if (pending.Length > 0)
            {
                return pending;
            }

            return await database.StreamReadGroupAsync(
                BackgroundJobQueueNames.StreamName,
                BackgroundJobQueueNames.ConsumerGroup,
                _settings.ConsumerName,
                ">",
                _settings.BatchSize);
        }
    }
}
