using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// Awaiters for running NWN code in an async context.
    /// </summary>
    public static class NWTask
    {
        private static readonly HashSet<ScheduledItem> _scheduledItems = new HashSet<ScheduledItem>();
        private static readonly object _schedulerLock = new object();
        private static bool _isInScriptContext;
        private static Thread _mainThread;

        private static Task RunAndAwait(Func<bool> completionSource)
        {
            if (completionSource())
            {
                return Task.CompletedTask;
            }

            var scheduledItem = new ScheduledItem(completionSource);
            lock (_schedulerLock)
            {
                _scheduledItems.Add(scheduledItem);
            }

            return scheduledItem.TaskCompletionSource.Task;
        }

        internal class TaskRunner
        {
            public TaskRunner()
            {
                _mainThread = Thread.CurrentThread;
                Entrypoints.OnScriptContextBegin += () => _isInScriptContext = true;
                Entrypoints.OnScriptContextEnd += () => _isInScriptContext = false;
            }

            internal void Process()
            {
                _isInScriptContext = true;
                lock (_schedulerLock)
                {
                    _scheduledItems.RemoveWhere(item =>
                    {
                        if (!item.CompletionSource())
                        {
                            return false;
                        }

                        item.TaskCompletionSource.SetResult(true);
                        return true;
                    });
                }

                _isInScriptContext = false;
            }
        }

        private class ScheduledItem
        {
            public readonly Func<bool> CompletionSource;
            public readonly TaskCompletionSource<bool> TaskCompletionSource = new TaskCompletionSource<bool>();

            public ScheduledItem(Func<bool> completionSource)
            {
                CompletionSource = completionSource;
            }
        }
        /// <summary>
        /// Waits until the specified amount of time has passed.
        /// </summary>
        /// <param name="delay">How long to wait.</param>
        public static async Task Delay(TimeSpan delay)
        {
            var stopwatch = Stopwatch.StartNew();
            await RunAndAwait(() => delay < stopwatch.Elapsed);
        }

        /// <summary>
        /// Safely returns to a NWScript context from another thread.
        /// </summary>
        public static async Task SwitchToMainThread()
        {
            // We can execute immediately as we are already in a safe script context.
            if (_isInScriptContext && Thread.CurrentThread == _mainThread)
            {
                return;
            }

            await DelayFrame(1);
        }

        /// <summary>
        /// Waits until the next server frame/loop.
        /// </summary>
        /// <returns></returns>
        public static async Task NextFrame() => await DelayFrame(1);

        /// <summary>
        /// Waits until the specified amount of frames have passed.
        /// </summary>
        /// <param name="frames">The number of frames to wait.</param>
        public static async Task DelayFrame(int frames)
        {
            frames++;
            await RunAndAwait(() =>
            {
                frames--;
                return frames <= 0;
            });
        }

        /// <summary>
        /// Queues the specified work to run on the next server cycle.
        /// </summary>
        /// <param name="function">The task to run.</param>
        public static async Task Run(Func<Task> function)
        {
            await SwitchToMainThread();
            await function();
        }

        /// <inheritdoc cref="Run"/>
        public static async Task<T> Run<T>(Func<Task<T>> function)
        {
            await SwitchToMainThread();
            return await function();
        }

        /// <summary>
        /// Waits until the specified expression returns true.
        /// </summary>
        /// <param name="test">The test expression.</param>
        /// <returns></returns>
        public static async Task WaitUntil(Func<bool> test) => await RunAndAwait(test);

        /// <summary>
        /// Waits until the specified value source changes.
        /// </summary>
        /// <param name="valueSource">The watched value source.</param>
        public static async Task WaitUntilValueChanged<T>(Func<T> valueSource)
        {
            var currentVal = valueSource();
            await RunAndAwait(() => !valueSource().Equals(currentVal));
        }

        /// <summary>
        /// Waits until all the specified tasks have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait on for completion.</param>
        public static async Task WhenAll(params Task[] tasks)
        {
            await Task.WhenAll(tasks);
            await SwitchToMainThread();
        }

        /// <inheritdoc cref="WhenAll(System.Threading.Tasks.Task[])"/>
        public static async Task WhenAll(IEnumerable<Task> tasks)
        {
            await Task.WhenAll(tasks);
            await SwitchToMainThread();
        }

        /// <inheritdoc cref="WhenAll(System.Threading.Tasks.Task[])"/>
        public static async Task<TResult[]> WhenAll<TResult>(params Task<TResult>[] tasks)
        {
            var results = await Task.WhenAll(tasks);
            await SwitchToMainThread();
            return results;
        }

        /// <inheritdoc cref="WhenAll(System.Threading.Tasks.Task[])"/>
        public static async Task<TResult[]> WhenAll<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            var results = await Task.WhenAll(tasks);
            await SwitchToMainThread();
            return results;
        }

        /// <summary>
        /// Waits until any of the specified tasks have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait on for completion.</param>
        public static async Task WhenAny(params Task[] tasks)
        {
            await Task.WhenAny(tasks);
            await SwitchToMainThread();
        }

        /// <inheritdoc cref="WhenAny(System.Threading.Tasks.Task[])"/>
        public static async Task WhenAny(IEnumerable<Task> tasks)
        {
            await Task.WhenAny(tasks);
            await SwitchToMainThread();
        }

        /// <inheritdoc cref="WhenAny(System.Threading.Tasks.Task[])"/>
        public static async Task<Task<TResult>> WhenAny<TResult>(params Task<TResult>[] tasks)
        {
            var results = await Task.WhenAny(tasks);
            await SwitchToMainThread();
            return results;
        }

        /// <inheritdoc cref="WhenAny(System.Threading.Tasks.Task[])"/>
        public static async Task<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            var results = await Task.WhenAny(tasks);
            await SwitchToMainThread();
            return results;
        }
    }
}
