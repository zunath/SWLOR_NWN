#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// A single weighted creature choice within a dungeon tier's ambient spawn pool.
    /// </summary>
    public class DungeonCreatureEntry
    {
        public string Resref { get; set; } = string.Empty;
        public int Weight { get; set; } = 1;
    }
}
