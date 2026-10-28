using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>A customs patrol waiting for a pilot's answer: pay the fine and surrender the goods, or run.</summary>
    public class CustomsScan
    {
        public string Id { get; set; }
        public uint Area { get; set; }
        public TradeAuthority Authority { get; set; }
        public string PatrolGroup { get; set; }

        /// <summary>The banned goods the scan found, by units.</summary>
        public Dictionary<CommodityType, int> Goods { get; set; } = new();

        public int Fine { get; set; }
        public DateTime AnswerBy { get; set; }

        /// <summary>The system's tier (1-6); a spoof is sliced at it, at most 5.</summary>
        public int Tier { get; set; }

        /// <summary>The player slicing the scan (the pilot or the Sensors crew member), or OBJECT_INVALID.</summary>
        public uint Slicer { get; set; } = OBJECT_INVALID;

        public bool IsBeingSliced => Slicer != OBJECT_INVALID;
    }
}
