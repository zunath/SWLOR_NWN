namespace SWLOR.Game.Server.Service.PerformanceService
{
    public enum PerformanceEndReason
    {
        Stopped = 0,
        Combat = 1,
        Moved = 2,
        AreaExit = 3,
        Died = 4,
        LoggedOut = 5,
        InstrumentRemoved = 6,
        Idle = 7,
        Invalid = 8
    }
}
