namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Identifies which path requested a character appearance restore after space mode.
    /// </summary>
    public enum AppearanceRestoreTrigger
    {
        SpaceExit,
        SpaceDeath,
        Logout,
        AreaEnter
    }
}
