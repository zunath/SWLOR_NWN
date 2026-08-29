using System;

namespace SWLOR.Game.Server.Service.AbilityService
{
    [Flags]
    public enum AbilityTargetingFlags
    {
        None = 0,
        HarmsEnemies = 1,
        HarmsAllies = 2,
        HelpsAllies = 4,
        IgnoresSelf = 8,
        OriginOnSelf = 16,
        SuppressWithTarget = 32,
        BackOffsetOrigin = 64
    }
}
