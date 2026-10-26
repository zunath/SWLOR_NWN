namespace SWLOR.Game.Server.Service.PlayerListService
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
