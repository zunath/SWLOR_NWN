namespace SWLOR.Shared.Core.Async.Awaiters
{
    public readonly struct SynchronizationContextAwaiter : IAwaiter
    {
        private static readonly SendOrPostCallback PostCallback = state => ((Action)state)?.Invoke();

        private readonly SynchronizationContext context;

        public SynchronizationContextAwaiter(SynchronizationContext context)
        {
            this.context = context;
        }

        public bool IsCompleted
        {
            get => context == SynchronizationContext.Current;
        }

        public void OnCompleted(Action continuation)
        {
            context.Post(PostCallback, continuation);
        }

        public void GetResult() { }
    }
}
