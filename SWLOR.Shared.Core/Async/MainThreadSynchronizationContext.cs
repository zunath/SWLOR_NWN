using SWLOR.Shared.Core.Async.Awaiters;
using SWLOR.Shared.Core.Extension;

namespace SWLOR.Shared.Core.Async
{
    public sealed class MainThreadSynchronizationContext : SynchronizationContext, IAwaitable
    {
        private readonly List<QueuedTask> _queuedTasks = new List<QueuedTask>();
        private readonly List<QueuedTask> _currentWork = new List<QueuedTask>();

        public void Update()
        {
            lock (_queuedTasks)
            {
                _currentWork.AddRange(_queuedTasks);
                _queuedTasks.Clear();
            }

            try
            {
                foreach (QueuedTask task in _currentWork)
                {
                    task.Invoke();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToMessageAndCompleteStacktrace());
            }
            finally
            {
                _currentWork.Clear();
            }
        }

        public override void Post(SendOrPostCallback callback, object state)
        {
            lock (_queuedTasks)
            {
                _queuedTasks.Add(new QueuedTask(callback, state));
            }
        }

        public override void Send(SendOrPostCallback callback, object state)
        {
            lock (_queuedTasks)
            {
                _queuedTasks.Add(new QueuedTask(callback, state));
            }
        }

        public IAwaiter GetAwaiter()
        {
            return new SynchronizationContextAwaiter(this);
        }

        private readonly struct QueuedTask
        {
            private readonly SendOrPostCallback _callback;
            private readonly object _state;

            public QueuedTask(SendOrPostCallback callback, object state)
            {
                this._callback = callback;
                this._state = state;
            }

            public void Invoke()
            {
                try
                {
                    _callback(_state);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToMessageAndCompleteStacktrace());
                }
            }
        }
    }
}
