namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Where the viewing player stands in a HoloCom call.
    /// </summary>
    public enum PlayerListCallState
    {
        None,
        Outgoing,
        Incoming,
        Connected,
    }
}
