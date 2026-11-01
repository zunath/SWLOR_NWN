namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>Where a running capital battle stands on a tick.</summary>
    public enum CapitalBattleStep
    {
        Running = 0,
        Destroyed = 1,
        Withdraw = 2,
    }
}
