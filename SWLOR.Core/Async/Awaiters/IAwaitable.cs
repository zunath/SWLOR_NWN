namespace SWLOR.Core.Async.Awaiters
{
    public interface IAwaitable
    {
        IAwaiter GetAwaiter();
    }
}
