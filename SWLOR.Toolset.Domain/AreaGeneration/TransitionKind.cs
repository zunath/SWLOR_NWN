#nullable disable
using System;
using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    public enum TransitionKind
    {
        /// <summary>An arrival point: players enter the area here.</summary>
        Entrance = 0,
        /// <summary>An outbound link: an exit placeable/transition spawns here.</summary>
        Exit = 1
    }
}
