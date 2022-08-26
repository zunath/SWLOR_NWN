using System;

namespace SWLOR.Game.Server.Service.AIService
{
    [Flags]
    public enum WalkWpFlag
    {
        Uninitialized = 0,
        Initialized = 1,
        GoingBackwards = 2,
    }
}
