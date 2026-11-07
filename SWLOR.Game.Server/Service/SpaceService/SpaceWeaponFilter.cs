namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Which weapon family a space ability fires: energy weapons, launchers, or whichever the ship has.
    /// </summary>
    public enum SpaceWeaponFilter
    {
        Any = 0,
        Energy = 1,
        Launcher = 2,
    }
}
