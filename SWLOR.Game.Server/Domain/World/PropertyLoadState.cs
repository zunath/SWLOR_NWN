namespace SWLOR.Game.Server.Domain.World
{
    public enum PropertyLoadState
    {
        Unloaded = 0,
        Queued = 1,
        Loading = 2,
        Loaded = 3,
        Failed = 4
    }
}
