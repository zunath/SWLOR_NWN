using System;
using System.Threading;
using System.Threading.Tasks;
using SWLOR.Shared.Core.Async;

namespace SWLOR.Test.Shared.Core.Async.TestHelpers
{
    /// <summary>
    /// Test-specific version of NwTask that doesn't rely on MainThreadSynchronizationContext
    /// </summary>
    public static class TestNwTask
    {
        /// <summary>
        /// Test version of Delay that uses Task.Delay instead of the synchronization context
        /// </summary>
        public static async Task Delay(TimeSpan delay, CancellationToken? cancellationToken = null)
        {
            try
            {
                if (cancellationToken.HasValue)
                {
                    await Task.Delay(delay, cancellationToken.Value);
                }
                else
                {
                    await Task.Delay(delay);
                }
            }
            catch (TaskCanceledException)
            {
                // Convert TaskCanceledException to OperationCanceledException to match NwTask behavior
                throw new OperationCanceledException();
            }
        }

        /// <summary>
        /// Test version of DelayFrame that uses Task.Delay instead of the synchronization context
        /// </summary>
        public static async Task DelayFrame(int frames, CancellationToken? cancellationToken = null)
        {
            try
            {
                // For testing, just use a small delay instead of frame-based waiting
                var delay = TimeSpan.FromMilliseconds(frames * 16); // Approximate 60 FPS
                if (cancellationToken.HasValue)
                {
                    await Task.Delay(delay, cancellationToken.Value);
                }
                else
                {
                    await Task.Delay(delay);
                }
            }
            catch (TaskCanceledException)
            {
                // Convert TaskCanceledException to OperationCanceledException to match NwTask behavior
                throw new OperationCanceledException();
            }
        }

        /// <summary>
        /// Test version of NextFrame that uses Task.Delay instead of the synchronization context
        /// </summary>
        public static async Task NextFrame()
        {
            await DelayFrame(1);
        }

        /// <summary>
        /// Test version of Run that executes the function directly
        /// </summary>
        public static async Task Run(Func<Task> function)
        {
            await function();
        }

        /// <summary>
        /// Test version of Run that executes the function directly and returns the result
        /// </summary>
        public static async Task<T> Run<T>(Func<Task<T>> function)
        {
            return await function();
        }

        /// <summary>
        /// Test version of WaitUntil that uses a polling approach
        /// Matches production behavior: exits silently when cancellation token is requested
        /// </summary>
        public static async Task WaitUntil(Func<bool> test, CancellationToken? cancellationToken = null)
        {
            while (!test() && cancellationToken is not { IsCancellationRequested: true })
            {
                await Task.Delay(1);
            }
        }

        /// <summary>
        /// Test version of WaitUntilValueChanged that uses a polling approach
        /// Matches production behavior: exits silently when cancellation token is requested
        /// </summary>
        public static async Task WaitUntilValueChanged<T>(Func<T> valueSource, CancellationToken? cancellationToken = null)
        {
            T currentVal = valueSource();
            while (Equals(currentVal, valueSource()) && cancellationToken is not { IsCancellationRequested: true })
            {
                await Task.Delay(1);
            }
        }

        /// <summary>
        /// Test version of WhenAll that uses Task.WhenAll directly
        /// </summary>
        public static async Task WhenAll(params Task[] tasks)
        {
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Test version of WhenAll that uses Task.WhenAll directly
        /// </summary>
        public static async Task WhenAll(IEnumerable<Task> tasks)
        {
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Test version of WhenAll that uses Task.WhenAll directly
        /// </summary>
        public static async Task<TResult[]> WhenAll<TResult>(params Task<TResult>[] tasks)
        {
            return await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Test version of WhenAll that uses Task.WhenAll directly
        /// </summary>
        public static async Task<TResult[]> WhenAll<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            return await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Test version of WhenAny that uses Task.WhenAny directly
        /// </summary>
        public static async Task WhenAny(params Task[] tasks)
        {
            await Task.WhenAny(tasks);
        }

        /// <summary>
        /// Test version of WhenAny that uses Task.WhenAny directly
        /// </summary>
        public static async Task WhenAny(IEnumerable<Task> tasks)
        {
            await Task.WhenAny(tasks);
        }

        /// <summary>
        /// Test version of WhenAny that uses Task.WhenAny directly
        /// </summary>
        public static async Task<Task<TResult>> WhenAny<TResult>(params Task<TResult>[] tasks)
        {
            return await Task.WhenAny(tasks);
        }

        /// <summary>
        /// Test version of WhenAny that uses Task.WhenAny directly
        /// </summary>
        public static async Task<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            return await Task.WhenAny(tasks);
        }
    }
}
