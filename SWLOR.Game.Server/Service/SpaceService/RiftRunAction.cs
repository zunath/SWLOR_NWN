namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>What a Rift Run's tick asks the engine to do (<see cref="RiftRunRules.Step"/>).</summary>
    public enum RiftRunAction
    {
        None = 0,
        SpawnWave = 1,
        RoomCleared = 2,
        PullToNextRoom = 3,
        Complete = 4,
        Collapse = 5,
        ReturnHome = 6,
        Abandon = 7,
    }
}
