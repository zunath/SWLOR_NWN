using System;

namespace SWLOR.Game.Server.AI
{
    [Flags]
    public enum AIFlags
    {
        None = 0,
        AggroNearby = 1,
        Link = 2,
        RandomWalk = 4,
        ReturnToSpawnPoint = 8

    }
}
