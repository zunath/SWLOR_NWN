using System;

namespace SWLOR.Game.Server.Service.AIService
{
    [Flags]
    public enum AIFlag
    {
        None = 0,
        RandomWalk = 1,
        ReturnHome = 2,

    }
}
