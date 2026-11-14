namespace SWLOR.Game.Server.Domain.Social
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
