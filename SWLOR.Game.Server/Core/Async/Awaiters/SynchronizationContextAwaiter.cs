using System.Threading;

namespace SWLOR.Game.Server.Core.Async.Awaiters
{
    public readonly struct SynchronizationContextAwaiter : IAwaiter
    {
        private static readonly SendOrPostCallback PostCallback = state => ((System.Action)state)?.Invoke();

        private readonly SynchronizationContext context;
        private readonly bool alwaysYield;

        public SynchronizationContextAwaiter(SynchronizationContext context, bool alwaysYield = false)
        {
            this.context = context;
            this.alwaysYield = alwaysYield;
        }

        public SynchronizationContextAwaiter GetAwaiter() => this;

        public bool IsCompleted
        {
            get => !alwaysYield && context == SynchronizationContext.Current;
        }

        public void OnCompleted(System.Action continuation)
        {
            context.Post(PostCallback, continuation);
        }

        public void GetResult() { }
    }
}
