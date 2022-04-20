namespace SWLOR.Game.Server.Core.Async.Awaiters
{
    public interface IAwaitable
    {
        IAwaiter GetAwaiter();
    }
}
