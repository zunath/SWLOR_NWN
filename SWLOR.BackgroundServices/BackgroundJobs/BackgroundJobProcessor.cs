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
            try
            {
                if (!_handlers.TryGetValue(backgroundJob.Type, out var handler))
                {
                    throw new InvalidOperationException($"Unsupported background job type '{backgroundJob.Type}'.");
                }

                await handler.HandleAsync(backgroundJob.Payload, cancellationToken);
                await database.StreamAcknowledgeAsync(
                    BackgroundJobQueueNames.StreamName,
                    BackgroundJobQueueNames.ConsumerGroup,
                    backgroundJob.Id);

                _logger.Info($"Processed background job {backgroundJob.Id} ({backgroundJob.Type}).");
            }
            catch (Exception ex)
            {
                await _failureHandler.HandleFailureAsync(database, backgroundJob, ex);
            }
        }
    }
}
