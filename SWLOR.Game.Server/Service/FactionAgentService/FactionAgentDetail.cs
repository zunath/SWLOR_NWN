using System.Collections.Generic;
using SWLOR.Game.Server.Service.FactionService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.FactionAgentService
{
    /// <summary>A faction agent: who they are, where they stand and what their board offers. Rows come from FactionAgentTuning.</summary>
    public class FactionAgentDetail
    {
        public FactionAgentType Type { get; set; }
        public string Name { get; set; }
        public FactionType Faction { get; set; }

        /// <summary>The dock's landing area resref: the dock id Trade uses for ships docked there.</summary>
        public string DockId { get; set; }

        public string DockName { get; set; }
        public TradeExchangeType Exchange { get; set; }

        /// <summary>The tier of the dock's exchange: standard offers are at this tier.</summary>
        public int DockTier { get; set; }

        public string Greeting { get; set; }

        /// <summary>How often each mission type comes up on this agent's board.</summary>
        public Dictionary<AgentMissionType, int> MissionWeights { get; set; } = new();

        /// <summary>By mission tier, the systems this agent sends pilots to, nearest first.</summary>
        public Dictionary<int, AgentMissionOrbit[]> Orbits { get; set; } = new();
    }
}
