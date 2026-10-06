using System;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Entity
{
    public sealed class SpaceEncounter : EntityBase
    {
        [Indexed] public string AreaResref { get; set; }
        public string Profile { get; set; }
        public string ActivityId { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public bool WreckCreated { get; set; }
        [Indexed] public bool RewardJournalComplete { get; set; }
        [Indexed] public bool Completed { get; set; }
        public DateTime CompletedAt { get; set; }
        public SpaceContributionLedger Contributions { get; set; } = new();
    }
}
