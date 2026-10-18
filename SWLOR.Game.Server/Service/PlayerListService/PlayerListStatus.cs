namespace SWLOR.Game.Server.Service.PlayerListService
{
    /// <summary>
    /// The short party or hostility marker shown beside a player in the list.
    /// </summary>
    public enum PlayerListStatus
    {
        None = 0,
        Leader = 1,
        Party = 2,
        Grouped = 3,
        Hostile = 4,
        DM = 5,
    }
}
