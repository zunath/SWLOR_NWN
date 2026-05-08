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
            var redisOptions = ConfigurationOptions.Parse(_settings.RedisConnection);
            redisOptions.AbortOnConnectFail = false;

            using var redis = await ConnectToRedis(redisOptions, cancellationToken);
            var database = redis.GetDatabase();
            await EnsureConsumerGroup(database, cancellationToken);

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

        private async Task<ConnectionMultiplexer> ConnectToRedis(
            ConfigurationOptions redisOptions,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var redis = await ConnectionMultiplexer.ConnectAsync(redisOptions);
                    _logger.Info("Redis connection established.");
                    return redis;
                }
                catch (RedisConnectionException)
                {
                    _logger.Info($"Redis is not available yet. Retrying in {_settings.FailureDelay.TotalSeconds:0.#} seconds.");
                    await Task.Delay(_settings.FailureDelay, cancellationToken);
                }
            }

            throw new OperationCanceledException(cancellationToken);
        }

        private async Task EnsureConsumerGroup(IDatabase database, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await database.StreamCreateConsumerGroupAsync(
                        BackgroundJobQueueNames.StreamName,
                        BackgroundJobQueueNames.ConsumerGroup,
                        "0-0",
                        true);
                    return;
                }
                catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP", StringComparison.OrdinalIgnoreCase))
                {
                    // Group already exists.
                    return;
                }
                catch (RedisConnectionException)
                {
                    _logger.Info($"Redis is still loading. Retrying background job initialization in {_settings.FailureDelay.TotalSeconds:0.#} seconds.");
                    await Task.Delay(_settings.FailureDelay, cancellationToken);
                }
                catch (RedisTimeoutException)
                {
                    _logger.Info($"Redis is still loading. Retrying background job initialization in {_settings.FailureDelay.TotalSeconds:0.#} seconds.");
                    await Task.Delay(_settings.FailureDelay, cancellationToken);
                }
            }

            throw new OperationCanceledException(cancellationToken);
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
