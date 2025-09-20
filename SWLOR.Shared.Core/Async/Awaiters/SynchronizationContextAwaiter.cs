namespace SWLOR.Shared.Core.Async.Awaiters
{
    public readonly struct SynchronizationContextAwaiter : IAwaiter
    {
        private static readonly SendOrPostCallback PostCallback = state => ((System.Action)state)?.Invoke();

        private readonly SynchronizationContext context;

        public SynchronizationContextAwaiter(SynchronizationContext context)
        {
            this.context = context;
        }

        public bool IsCompleted
        {
            get => context == SynchronizationContext.Current;
        }

        public void OnCompleted(System.Action continuation)
        {
            context.Post(PostCallback, continuation);
        }

        public void GetResult() { }
    }
}
