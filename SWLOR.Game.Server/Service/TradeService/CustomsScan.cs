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
    }
}
