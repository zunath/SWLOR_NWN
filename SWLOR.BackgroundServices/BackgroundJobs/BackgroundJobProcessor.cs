using StackExchange.Redis;
using SWLOR.BackgroundServices.Infrastructure;

namespace SWLOR.BackgroundServices.BackgroundJobs
{
    public sealed class BackgroundJobProcessor
    {
        private readonly IReadOnlyDictionary<string, IBackgroundJobHandler> _handlers;
        private readonly BackgroundJobFailureHandler _failureHandler;
        private readonly IAppLogger _logger;

        public BackgroundJobProcessor(
            IReadOnlyDictionary<string, IBackgroundJobHandler> handlers,
            BackgroundJobFailureHandler failureHandler,
            IAppLogger logger)
        {
            _handlers = handlers;
            _failureHandler = failureHandler;
            _logger = logger;
        }

        public async Task ProcessAsync(IDatabase database, StreamEntry entry, CancellationToken cancellationToken)
        {
            if (!BackgroundJob.TryCreate(entry, out var job, out var error))
            {
                await _failureHandler.MoveToDeadLetterAsync(database, entry, error);
                return;
            }

            var backgroundJob = job!;
            if (!_handlers.TryGetValue(backgroundJob.Type, out var handler))
            {
                await _failureHandler.MoveToDeadLetterAsync(
                    database,
                    backgroundJob.Entry,
                    $"Unsupported background job type '{backgroundJob.Type}'.");
                _logger.Error($"Unsupported background job type '{backgroundJob.Type}' for job {backgroundJob.Id}; moved to dead-letter.");
                return;
            }

            try
            {
                await handler.HandleAsync(backgroundJob.Payload, cancellationToken);
                // Redis Streams provide at-least-once delivery here: if the handler succeeds
                // but XACK fails, this job can be delivered again. Handlers must be idempotent
                // or use their own deduplication/idempotency keys.
                await database.StreamAcknowledgeAsync(
                    BackgroundJobQueueNames.StreamName,
                    BackgroundJobQueueNames.ConsumerGroup,
                    backgroundJob.Id);

                _logger.Info($"Processed background job {backgroundJob.Id} ({backgroundJob.Type}).");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                await _failureHandler.HandleFailureAsync(database, backgroundJob, ex);
            }
        }
    }
}
