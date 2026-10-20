using System;

namespace SWLOR.Game.Server.Service.AbilityService
{
    [Flags]
    public enum MeleeTechniqueStage
    {
        None = 0,
        Opening = 1,
        Finish = 2,
        Distraction = 4
    }
}
