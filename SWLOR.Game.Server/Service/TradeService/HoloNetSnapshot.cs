using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>The HoloNet price board as of one moment; it is only rebuilt once it is stale.</summary>
    public class HoloNetSnapshot
    {
        public DateTime CapturedAt { get; set; }
        public List<HoloNetRow> Rows { get; set; } = new();
    }
}
