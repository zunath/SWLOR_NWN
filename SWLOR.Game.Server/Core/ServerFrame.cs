namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// The server frame number NWNX most recently passed to the main loop.
    /// </summary>
    public static class ServerFrame
    {
        public static ulong Current { get; internal set; }
    }
}
