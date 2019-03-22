namespace SWLOR.Game.Server.Threading.Contracts
{
    /// <summary>
    /// Used for running background threads while the server is alive.
    /// Do not inject anything that is not thread-safe, such as I_.
    /// You do not have access to the NWN context in any background thread. If you try, the server WILL CRASH.
    /// </summary>
    public interface IBackgroundThreadManager
    {
        void Start();
    }
}
