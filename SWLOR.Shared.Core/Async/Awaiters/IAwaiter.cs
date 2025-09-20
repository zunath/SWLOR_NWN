using System.Runtime.CompilerServices;

namespace SWLOR.Shared.Core.Async.Awaiters
{
    public interface IAwaiter : INotifyCompletion
    {
        public bool IsCompleted { get; }

        public void GetResult() { }
    }
}
