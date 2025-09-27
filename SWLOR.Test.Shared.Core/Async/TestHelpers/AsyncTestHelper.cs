using SWLOR.Shared.Core.Async;

namespace SWLOR.Test.Shared.Core.Async.TestHelpers
{
    /// <summary>
    /// Helper class for testing async operations that depend on MainThreadSynchronizationContext
    /// </summary>
    public static class AsyncTestHelper
    {
        /// <summary>
        /// Runs an async operation in a test environment with proper synchronization context setup
        /// </summary>
        /// <param name="asyncOperation">The async operation to run</param>
        /// <param name="timeoutMs">Maximum time to wait for completion (default: 5000ms)</param>
        /// <returns>The result of the async operation</returns>
        public static T RunWithTestContext<T>(Func<Task<T>> asyncOperation, int timeoutMs = 5000)
        {
            var context = NwTask.MainThreadSynchronizationContext;
            var originalContext = SynchronizationContext.Current;
            
            try
            {
                // Set the main thread synchronization context as current
                SynchronizationContext.SetSynchronizationContext(context);
                
                // Start the async operation
                var operationTask = asyncOperation();
                
                // Process the synchronization context until the operation completes or times out
                var timeout = TimeSpan.FromMilliseconds(timeoutMs);
                var startTime = DateTime.Now;
                
                while (!operationTask.IsCompleted && DateTime.Now - startTime < timeout)
                {
                    context.Update();
                    Thread.Sleep(1); // Small delay to prevent busy waiting
                }
                
                if (!operationTask.IsCompleted)
                {
                    throw new TimeoutException($"Operation did not complete within {timeoutMs}ms");
                }
                
                return operationTask.GetAwaiter().GetResult();
            }
            finally
            {
                // Restore the original synchronization context
                SynchronizationContext.SetSynchronizationContext(originalContext);
            }
        }
        
        /// <summary>
        /// Runs an async operation in a test environment with proper synchronization context setup
        /// </summary>
        /// <param name="asyncOperation">The async operation to run</param>
        /// <param name="timeoutMs">Maximum time to wait for completion (default: 5000ms)</param>
        public static void RunWithTestContext(Func<Task> asyncOperation, int timeoutMs = 5000)
        {
            RunWithTestContext(async () =>
            {
                await asyncOperation();
                return true;
            }, timeoutMs);
        }
    }
}
