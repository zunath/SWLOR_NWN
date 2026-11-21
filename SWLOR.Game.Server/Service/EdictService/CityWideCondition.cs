using System;

namespace SWLOR.Game.Server.Service.EdictService
{
    /// <summary>
    /// The conditions that belong to the city rather than to any one Edict. While any holds, every Edict is paused and
    /// no new one can be enacted.
    /// </summary>
    [Flags]
    public enum CityWideCondition
    {
        None = 0,

        /// <summary>A bill was still unpaid at the weekly check, until that bill is paid.</summary>
        Disrepair = 1,

        /// <summary>A failed confidence vote, until an election ends with an outright winner.</summary>
        NoConfidence = 2,

        /// <summary>An empty second recall, until an election with a candidate and a vote ends.</summary>
        Neglect = 4
    }
}
