using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Entity
{
    public sealed class SpaceSite : EntityBase
    {
        [Indexed] public string AreaResref { get; set; }
        public int Slot { get; set; }
        public string Generation { get; set; }
        public string Profile { get; set; }
        public string Blueprint { get; set; } = "spc_asteroid_til";
        public SpaceSiteKind Kind { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public int Hardness { get; set; }
        public int MaximumShips { get; set; }
        public int Stability { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime RespawnsAt { get; set; }
        public DateTime VentsUntil { get; set; }
        public DateTime NextHazardAt { get; set; }
        public DateTime ExclusiveUntil { get; set; }
        public HashSet<string> Participants { get; set; } = new();
        public Dictionary<string, double> Reserves { get; set; } = new();
        public Dictionary<string, double> InitialReserves { get; set; } = new();
        public Dictionary<string, double> SurveyResolution { get; set; } = new();
        public HashSet<string> SurveyedBy { get; set; } = new();
        public Dictionary<string, SpaceWorkClaim> Claims { get; set; } = new();
        public bool DiscoveryDrawn { get; set; }
    }
}
