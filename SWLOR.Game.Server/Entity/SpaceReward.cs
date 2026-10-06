using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Entity
{
    public sealed class SpaceReward : EntityBase
    {
        [Indexed] public string PlayerId { get; set; }
        [Indexed] public bool Settled { get; set; }
        public int Credits { get; set; }
        public bool CreditsSettled { get; set; }
        public Dictionary<SkillType, int> Experience { get; set; } = new();
    }
}
