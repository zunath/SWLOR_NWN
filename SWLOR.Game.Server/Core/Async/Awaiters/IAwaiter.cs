using System.Runtime.CompilerServices;

namespace SWLOR.Game.Server.Core.Async.Awaiters
{
    public interface IAwaiter : INotifyCompletion
    {
        public bool IsCompleted { get; }

        public void GetResult() { }
    }
}
